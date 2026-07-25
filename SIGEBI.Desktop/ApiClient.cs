using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace SIGEBI.Desktop;

public sealed class ApiClient : IDisposable
{
    private readonly HttpClient _httpClient = new();
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public void ConfigurarBaseUrl(string baseUrl)
    {
        if (!Uri.TryCreate(baseUrl.TrimEnd('/') + "/", UriKind.Absolute, out var uri))
            throw new ArgumentException("La URL de la API no es válida.");
        _httpClient.BaseAddress = uri;
    }

    public async Task<string> IniciarSesionAsync(string email, string password)
    {
        using var response = await _httpClient.PostAsJsonAsync(
            "api/Auth/login",
            new { email, password },
            JsonOptions);
        await AsegurarExitoAsync(response);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var token = document.RootElement.GetProperty("token").GetString()
            ?? throw new InvalidOperationException("La API no devolvió un token.");
        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);
        return token;
    }

    public Task<JsonElement> GetAsync(string endpoint) =>
        EnviarAsync(HttpMethod.Get, endpoint, null);

    public Task<JsonElement> PostAsync(string endpoint, object payload) =>
        EnviarAsync(HttpMethod.Post, endpoint, payload);

    public Task<JsonElement> PutAsync(string endpoint, object payload) =>
        EnviarAsync(HttpMethod.Put, endpoint, payload);

    public Task<JsonElement> DeleteAsync(string endpoint) =>
        EnviarAsync(HttpMethod.Delete, endpoint, null);

    private async Task<JsonElement> EnviarAsync(
        HttpMethod method,
        string endpoint,
        object? payload)
    {
        using var request = new HttpRequestMessage(method, endpoint);
        if (payload is not null)
            request.Content = JsonContent.Create(payload, options: JsonOptions);

        using var response = await _httpClient.SendAsync(request);
        await AsegurarExitoAsync(response);
        if (response.Content.Headers.ContentLength == 0 ||
            response.StatusCode == System.Net.HttpStatusCode.NoContent)
            return JsonDocument.Parse("{}").RootElement.Clone();

        var content = await response.Content.ReadAsStringAsync();
        return string.IsNullOrWhiteSpace(content)
            ? JsonDocument.Parse("{}").RootElement.Clone()
            : JsonDocument.Parse(content).RootElement.Clone();
    }

    private static async Task AsegurarExitoAsync(HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode)
            return;

        var detail = await response.Content.ReadAsStringAsync();
        try
        {
            using var problem = JsonDocument.Parse(detail);
            if (problem.RootElement.TryGetProperty("detail", out var property))
                detail = property.GetString() ?? detail;
            else if (problem.RootElement.TryGetProperty("title", out property))
                detail = property.GetString() ?? detail;
        }
        catch (JsonException)
        {
        }

        throw new HttpRequestException(
            $"La API respondió {(int)response.StatusCode} ({response.ReasonPhrase}). {detail}");
    }

    public void Dispose() => _httpClient.Dispose();
}
