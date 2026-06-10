using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Notifications.API.Services;

namespace Notifications.API.Services;

// Cliente HTTP para consultar datos de usuarios desde Users API
public class UsersClient
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<UsersClient> _logger;

    public UsersClient(IHttpClientFactory httpClientFactory, ILogger<UsersClient> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    // Obtiene el email del usuario consultando Users API.
    // Devuelve null si el usuario no existe o hay un error de comunicación.
    public async Task<string?> ObtenerEmailAsync(string usuarioId, string? correlationId)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("UsersClient");

            if (!string.IsNullOrEmpty(correlationId))
                client.DefaultRequestHeaders.TryAddWithoutValidation("X-Correlation-Id", correlationId);

            var response = await client.GetAsync($"api/users/{usuarioId}/exists");

            // Leemos el body una sola vez
            var body = await response.Content.ReadAsStringAsync();
            _logger.LogInformation("Users API respondió {StatusCode}: {Body}", response.StatusCode, body);

            if (!response.IsSuccessStatusCode)
                return null;

            // Parseamos desde el string ya leído
            var data = System.Text.Json.JsonSerializer.Deserialize<UserExistsResponse>(body,
                new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            return data?.Email;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener email del usuario {UsuarioId}", usuarioId);
            return null;
        }
    }
}

// DTO que mapea la respuesta de GET /api/users/{id}/exists
public class UserExistsResponse
{
    [JsonPropertyName("email")]
    public string? Email { get; set; }
}