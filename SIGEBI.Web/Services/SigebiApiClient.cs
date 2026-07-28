using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.WebUtilities;
using SIGEBI.Application.Dtos.Auth;
using SIGEBI.Application.Dtos.Catalogo;
using SIGEBI.Application.Dtos.Notificaciones;
using SIGEBI.Application.Dtos.SolicitudesPrestamo;
using SIGEBI.Application.Dtos.Usuarios;

namespace SIGEBI.Web.Services;

public sealed class SigebiApiClient(
    HttpClient httpClient,
    IConfiguration configuration) : ISigebiApiClient
{
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
            ["pagina"] = page.ToString(),
            ["tamanoPagina"] = pageSize.ToString()
        };
        return GetAsync<IReadOnlyCollection<LibroDto>>(
            QueryHelpers.AddQueryString("api/Libros/buscar", query),
            cancellationToken);
    }

    public Task<IReadOnlyCollection<LibroDto>> GetBooksAsync(
        int page = 1,
        int pageSize = 200,
        CancellationToken cancellationToken = default) =>
        GetAsync<IReadOnlyCollection<LibroDto>>(
            $"api/Libros?pagina={page}&tamanoPagina={pageSize}",
            cancellationToken);

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
        using var response = await httpClient.SendAsync(request, cancellationToken);
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
        using var response = await httpClient.SendAsync(request, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        return await response.Content.ReadFromJsonAsync<T>(JsonOptions, cancellationToken)
            ?? throw new SigebiApiException("La API devolvió una respuesta vacía.", (int)response.StatusCode);
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
        var message = ExtractMessage(raw) ??
            (response.StatusCode == HttpStatusCode.Unauthorized
                ? "La sesión no es válida o las credenciales son incorrectas."
                : "La API no pudo completar la operación.");
        throw new SigebiApiException(message, (int)response.StatusCode);
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
                    return value.GetString();
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
