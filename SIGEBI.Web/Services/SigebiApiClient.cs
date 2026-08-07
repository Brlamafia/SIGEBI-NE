using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Caching.Memory;
using SIGEBI.Application.Dtos.Auth;
using SIGEBI.Application.Dtos.Catalogo;
using SIGEBI.Application.Dtos.Notificaciones;
using SIGEBI.Application.Dtos.SolicitudesPrestamo;
using SIGEBI.Application.Dtos.Usuarios;

namespace SIGEBI.Web.Services;

public sealed class SigebiApiClient(
    HttpClient httpClient,
    IConfiguration configuration,
    IMemoryCache? cache = null) : ISigebiApiClient
{
    private readonly IMemoryCache _cache = cache ?? new MemoryCache(new MemoryCacheOptions());
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    public Task<ApiSession> LoginAsync(string email, string password, CancellationToken cancellationToken = default) =>
        SendAsync<ApiSession>(HttpMethod.Post, "api/Auth/login", new { email, password }, cancellationToken);

    public Task RegisterAsync(SaveUsuarioDto request, CancellationToken cancellationToken = default) =>
        SendAsync(HttpMethod.Post, "api/Auth/register", request, cancellationToken);

    public Task<ApiSession> ExternalLoginAsync(string email, CancellationToken cancellationToken = default) =>
        SendExternalAsync<ApiSession>("api/Auth/external-login", new { email }, cancellationToken);

    public Task<ApiSession> ExternalRegisterAsync(SaveUsuarioDto request, CancellationToken cancellationToken = default) =>
        SendExternalAsync<ApiSession>("api/Auth/external-register", request, cancellationToken);

    public async Task<string?> RequestPasswordResetAsync(
        string email,
        string resetUrlBase,
        CancellationToken cancellationToken = default)
    {
        var response = await SendAsync<PasswordResetResponse>(
            HttpMethod.Post,
            "api/Auth/forgot-password",
            new { email, resetUrlBase },
            cancellationToken);
        return response.DevelopmentResetUrl;
    }

    public Task ResetPasswordAsync(string token, string password, CancellationToken cancellationToken = default) =>
        SendAsync(HttpMethod.Post, "api/Auth/reset-password", new { token, password }, cancellationToken);

    public Task<UsuarioDto> GetMeAsync(CancellationToken cancellationToken = default) =>
        GetAsync<UsuarioDto>("api/Usuarios/me", cancellationToken);

    public Task<MySummary> GetMySummaryAsync(CancellationToken cancellationToken = default) =>
        GetAsync<MySummary>("api/Usuarios/me/resumen", cancellationToken);

    public Task ChangeMyPasswordAsync(
        string currentPassword,
        string newPassword,
        CancellationToken cancellationToken = default) =>
        SendAsync(
            HttpMethod.Put,
            "api/Usuarios/me/password",
            new CambiarPasswordDto
            {
                PasswordActual = currentPassword,
                PasswordNueva = newPassword
            },
            cancellationToken);

    public Task<IReadOnlyCollection<LibroDto>> SearchBooksAsync(
        string? term,
        string? genre,
        string? publisher,
        bool? available,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = new Dictionary<string, string?>
        {
            ["termino"] = term,
            ["genero"] = genre,
            ["editorial"] = publisher,
            ["disponible"] = available?.ToString().ToLowerInvariant(),
            ["skip"] = ((page - 1) * pageSize).ToString(),
            ["take"] = Math.Min(pageSize + 1, 200).ToString()
        };
        return GetAsync<IReadOnlyCollection<LibroDto>>(
            QueryHelpers.AddQueryString("api/Libros/buscar", query),
            cancellationToken);
    }

    public async Task<IReadOnlyCollection<LibroDto>> GetBooksAsync(
        int page = 1,
        int pageSize = 200,
        CancellationToken cancellationToken = default)
    {
        var cacheKey = $"web:catalogo:{page}:{pageSize}";
        var result = await _cache.GetOrCreateAsync(cacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);
            entry.SlidingExpiration = TimeSpan.FromMinutes(2);
            return await GetAsync<IReadOnlyCollection<LibroDto>>(
                $"api/Libros?pagina={page}&tamanoPagina={pageSize}",
                cancellationToken);
        });
        return result ?? Array.Empty<LibroDto>();
    }

    public Task<LibroDto> GetBookByIdAsync(
        int bookId,
        CancellationToken cancellationToken = default) =>
        GetAsync<LibroDto>($"api/Libros/{bookId}", cancellationToken);

    public Task<IReadOnlyCollection<SolicitudPrestamoDto>> GetMyRequestsAsync(
        CancellationToken cancellationToken = default) =>
        GetAsync<IReadOnlyCollection<SolicitudPrestamoDto>>(
            "api/SolicitudesPrestamo/mias",
            cancellationToken);

    public Task CreateRequestAsync(int bookId, CancellationToken cancellationToken = default) =>
        SendAsync(
            HttpMethod.Post,
            "api/SolicitudesPrestamo",
            new SaveSolicitudPrestamoDto { LibroId = bookId },
            cancellationToken);

    public Task CancelRequestAsync(int requestId, CancellationToken cancellationToken = default) =>
        SendAsync(HttpMethod.Delete, $"api/SolicitudesPrestamo/{requestId}", null, cancellationToken);

    public Task<IReadOnlyCollection<NotificacionDto>> GetMyNotificationsAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken = default) =>
        GetAsync<IReadOnlyCollection<NotificacionDto>>(
            $"api/Notificaciones/mias?pagina={page}&tamanoPagina={pageSize}",
            cancellationToken);

    public Task MarkNotificationReadAsync(int notificationId, CancellationToken cancellationToken = default) =>
        SendAsync(HttpMethod.Put, $"api/Notificaciones/{notificationId}/leer", null, cancellationToken);

    public Task MarkAllNotificationsReadAsync(CancellationToken cancellationToken = default) =>
        SendAsync(HttpMethod.Put, "api/Notificaciones/leer-todas", null, cancellationToken);

    private async Task<T> SendExternalAsync<T>(
        string uri,
        object body,
        CancellationToken cancellationToken)
    {
        var secret = configuration["Authentication:WebClientSecret"];
        if (string.IsNullOrWhiteSpace(secret))
            throw new InvalidOperationException(
                "Debe configurar Authentication:WebClientSecret para usar Google.");

        using var request = new HttpRequestMessage(HttpMethod.Post, uri)
        {
            Content = JsonContent.Create(body, options: JsonOptions)
        };
        request.Headers.Add("X-SIGEBI-Web-Key", secret);
        return await SendRequestAsync<T>(request, cancellationToken);
    }

    private Task<T> GetAsync<T>(string uri, CancellationToken cancellationToken) =>
        SendAsync<T>(HttpMethod.Get, uri, null, cancellationToken);

    private async Task SendAsync(
        HttpMethod method,
        string uri,
        object? body,
        CancellationToken cancellationToken)
    {
        using var request = CreateRequest(method, uri, body);
        using var response = await SendHttpAsync(request, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
    }

    private async Task<T> SendAsync<T>(
        HttpMethod method,
        string uri,
        object? body,
        CancellationToken cancellationToken)
    {
        using var request = CreateRequest(method, uri, body);
        return await SendRequestAsync<T>(request, cancellationToken);
    }

    private async Task<T> SendRequestAsync<T>(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        using var response = await SendHttpAsync(request, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        try
        {
            return await response.Content.ReadFromJsonAsync<T>(JsonOptions, cancellationToken)
                ?? throw new SigebiApiException(
                    "La API devolvió una respuesta vacía.",
                    StatusCodes.Status502BadGateway);
        }
        catch (JsonException exception)
        {
            throw new SigebiApiException(
                "La API devolvió una respuesta que no se pudo interpretar.",
                StatusCodes.Status502BadGateway,
                exception);
        }
    }

    private async Task<HttpResponseMessage> SendHttpAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        try
        {
            return await httpClient.SendAsync(request, cancellationToken);
        }
        catch (OperationCanceledException exception)
            when (!cancellationToken.IsCancellationRequested)
        {
            throw new SigebiApiException(
                "La API tardó demasiado en responder. Inténtalo nuevamente.",
                StatusCodes.Status504GatewayTimeout,
                exception);
        }
        catch (HttpRequestException exception)
        {
            throw new SigebiApiException(
                "No fue posible comunicarse con la API de SIGEBI. Verifica que el servicio esté disponible.",
                StatusCodes.Status503ServiceUnavailable,
                exception);
        }
    }

    private static HttpRequestMessage CreateRequest(HttpMethod method, string uri, object? body)
    {
        var request = new HttpRequestMessage(method, uri);
        if (body is not null)
            request.Content = JsonContent.Create(body, options: JsonOptions);
        return request;
    }

    private static async Task EnsureSuccessAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
            return;

        var raw = await response.Content.ReadAsStringAsync(cancellationToken);
        var detail = ExtractMessage(raw);
        var message = response.StatusCode switch
        {
            HttpStatusCode.BadRequest => detail ?? "Revisa los datos enviados.",
            HttpStatusCode.Unauthorized =>
                "La sesión no es válida o las credenciales son incorrectas.",
            HttpStatusCode.Forbidden =>
                "Tu cuenta no tiene permiso para realizar esta acción.",
            HttpStatusCode.NotFound => detail ?? "La información solicitada no existe.",
            HttpStatusCode.Conflict => detail ?? "La operación entra en conflicto con el estado actual.",
            >= HttpStatusCode.InternalServerError =>
                "La API encontró un problema al procesar la solicitud. Inténtalo nuevamente.",
            _ => detail ?? "La API no pudo completar la operación."
        };
        throw new SigebiApiException(
            message,
            (int)response.StatusCode,
            responseDetail: detail);
    }

    private static string? ExtractMessage(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return null;
        try
        {
            using var document = JsonDocument.Parse(raw);
            var root = document.RootElement;
            foreach (var property in new[] { "detail", "title", "message" })
            {
                if (root.ValueKind == JsonValueKind.Object &&
                    root.TryGetProperty(property, out var value) &&
                    value.ValueKind == JsonValueKind.String)
                {
                    var simpleMessage = value.GetString();
                    if (property != "title" ||
                        !root.TryGetProperty("errors", out _))
                        return simpleMessage;
                }
            }
            if (root.ValueKind == JsonValueKind.Object &&
                root.TryGetProperty("errors", out var errors) &&
                errors.ValueKind == JsonValueKind.Object)
            {
                var validationMessages = errors
                    .EnumerateObject()
                    .SelectMany(error => error.Value.ValueKind == JsonValueKind.Array
                        ? error.Value.EnumerateArray()
                            .Where(item => item.ValueKind == JsonValueKind.String)
                            .Select(item => item.GetString())
                        : [])
                    .Where(message => !string.IsNullOrWhiteSpace(message))
                    .Distinct()
                    .ToArray();
                if (validationMessages.Length > 0)
                    return string.Join(" ", validationMessages);
            }
            return root.ValueKind == JsonValueKind.String ? root.GetString() : raw;
        }
        catch (JsonException)
        {
            return raw.Trim('"');
        }
    }

    private sealed class PasswordResetResponse
    {
        public string? DevelopmentResetUrl { get; init; }
    }
}
