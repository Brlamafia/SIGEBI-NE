using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SIGEBI.Desktop;

public sealed class ApiClient : IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly bool _ownsClient;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public ApiClient() : this(new HttpClient(), ownsClient: true)
    {
    }

    public ApiClient(HttpClient httpClient, bool ownsClient = false)
    {
        _httpClient = httpClient;
        _ownsClient = ownsClient;
        _httpClient.Timeout = TimeSpan.FromSeconds(30);
    }

    public DesktopSession? Session { get; private set; }

    public void ConfigurarBaseUrl(string baseUrl)
    {
        if (!Uri.TryCreate(baseUrl.TrimEnd('/') + "/", UriKind.Absolute, out var uri) ||
            uri.Scheme is not ("http" or "https"))
            throw new ArgumentException("La URL de la API no es válida.");
        _httpClient.BaseAddress = uri;
    }

    public async Task<DesktopSession> IniciarSesionAsync(
        string email,
        string password,
        CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.PostAsJsonAsync(
            "api/Auth/login",
            new { email, password },
            JsonOptions,
            cancellationToken);
        await AsegurarExitoAsync(response, cancellationToken);
        var session = await response.Content.ReadFromJsonAsync<DesktopSession>(
            JsonOptions,
            cancellationToken)
            ?? throw new InvalidOperationException("La API no devolvió la sesión.");
        if (string.IsNullOrWhiteSpace(session.Token))
            throw new InvalidOperationException("La API no devolvió un token.");

        Session = session;
        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", session.Token);
        return session;
    }

    public void CerrarSesion()
    {
        Session = null;
        _httpClient.DefaultRequestHeaders.Authorization = null;
    }

    public Task<JsonElement> GetAsync(
        string endpoint,
        CancellationToken cancellationToken = default) =>
        EnviarAsync(HttpMethod.Get, endpoint, null, cancellationToken);

    public Task<JsonElement> PostAsync(
        string endpoint,
        object payload,
        CancellationToken cancellationToken = default) =>
        EnviarAsync(HttpMethod.Post, endpoint, payload, cancellationToken);

    public Task<JsonElement> PutAsync(
        string endpoint,
        object payload,
        CancellationToken cancellationToken = default) =>
        EnviarAsync(HttpMethod.Put, endpoint, payload, cancellationToken);

    public Task<JsonElement> DeleteAsync(
        string endpoint,
        object? payload = null,
        CancellationToken cancellationToken = default) =>
        EnviarAsync(HttpMethod.Delete, endpoint, payload, cancellationToken);

    private async Task<JsonElement> EnviarAsync(
        HttpMethod method,
        string endpoint,
        object? payload,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(method, endpoint);
        if (payload is not null)
            request.Content = JsonContent.Create(payload, options: JsonOptions);

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        await AsegurarExitoAsync(response, cancellationToken);
        if (response.Content.Headers.ContentLength == 0 ||
            response.StatusCode == HttpStatusCode.NoContent)
            return JsonDocument.Parse("{}").RootElement.Clone();

        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        return string.IsNullOrWhiteSpace(content)
            ? JsonDocument.Parse("{}").RootElement.Clone()
            : JsonDocument.Parse(content).RootElement.Clone();
    }

    private static async Task AsegurarExitoAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
            return;

        var detail = await response.Content.ReadAsStringAsync(cancellationToken);
        try
        {
            using var problem = JsonDocument.Parse(detail);
            if (problem.RootElement.TryGetProperty("detail", out var property))
                detail = property.GetString() ?? detail;
            else if (problem.RootElement.TryGetProperty("title", out property))
                detail = property.GetString() ?? detail;
            else if (problem.RootElement.ValueKind == JsonValueKind.String)
                detail = problem.RootElement.GetString() ?? detail;
        }
        catch (JsonException)
        {
        }

        if (response.StatusCode == HttpStatusCode.Unauthorized)
            throw new DesktopSessionExpiredException(
                string.IsNullOrWhiteSpace(detail)
                    ? "La sesión expiró o no es válida."
                    : detail);

        throw new HttpRequestException(
            $"La API respondió {(int)response.StatusCode} ({response.ReasonPhrase}). {detail}");
    }

    public void Dispose()
    {
        if (_ownsClient)
            _httpClient.Dispose();
    }
}

public sealed class DesktopSession
{
    public string Token { get; init; } = string.Empty;
    public DesktopUser Usuario { get; init; } = new();
    public IReadOnlyCollection<string> Roles { get; init; } = [];
    public IReadOnlyCollection<string> Permisos { get; init; } = [];

    public bool TieneRol(string role) =>
        Roles.Contains(role, StringComparer.OrdinalIgnoreCase);

    public bool TienePermiso(string permission) =>
        Permisos.Contains(permission, StringComparer.OrdinalIgnoreCase);

    public bool PuedeUsarDesktop =>
        TieneRol("Administrador") ||
        TieneRol("Bibliotecario") ||
        TieneRol("Auditor");
}

public sealed class DesktopUser
{
    public int Id { get; init; }
    public string Nombre { get; init; } = string.Empty;
    public string Apellido { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
}

public sealed class DesktopSessionExpiredException(string message)
    : Exception(message);
