using Orders.API.Exceptions;
using System.Text.Json.Serialization;

namespace Orders.API.Services;

// Cliente HTTP centralizado para comunicación con Products API y Users API.
// Todas las llamadas salientes pasan por acá, incluyendo la propagación del Correlation ID.
public class ExternalServicesClient
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ExternalServicesClient> _logger;

    public ExternalServicesClient(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<ExternalServicesClient> logger)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _logger = logger;
    }

    // Verifica que el usuario existe en Users API.
    // Si no existe lanzamos ORD-003 para que el ExceptionHandler lo maneje.
    public async Task VerificarUsuarioAsync(Guid usuarioId, string? correlationId)
    {
        var client = CrearClienteConCorrelation("UsersClient", correlationId);
        var baseUrl = _configuration["Services:UsersApi"];

        _logger.LogInformation("Verificando existencia de usuario {UsuarioId}", usuarioId);

        var response = await client.GetAsync($"{baseUrl}/api/users/{usuarioId}/exists");

        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            throw new NotFoundException("ORD-003", "Usuario no encontrado al crear la orden.");

        response.EnsureSuccessStatusCode();
    }

    // Obtiene el producto desde Products API.
    // Si no existe lanzamos ORD-004. Si el stock es insuficiente, ORD-005.
    public async Task<ProductoResponse> ObtenerYValidarProductoAsync(
        Guid productoId, int cantidadSolicitada, string? correlationId)
    {
        var client = CrearClienteConCorrelation("ProductsClient", correlationId);
        var baseUrl = _configuration["Services:ProductsApi"];

        _logger.LogInformation("Consultando producto {ProductoId} a Products API", productoId);

        var response = await client.GetAsync($"{baseUrl}/api/products/{productoId}");

        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            throw new NotFoundException("ORD-004", "Producto no encontrado al crear la orden.");

        response.EnsureSuccessStatusCode();

        var producto = await response.Content.ReadFromJsonAsync<ProductoResponse>()
            ?? throw new NotFoundException("ORD-004", "Producto no encontrado al crear la orden.");

        // Validamos stock antes de permitir la orden
        if (producto.Stock < cantidadSolicitada)
            throw new BusinessRuleException("ORD-005",
                $"Stock insuficiente para '{producto.Nombre}'. " +
                $"Disponible: {producto.Stock}, solicitado: {cantidadSolicitada}.");

        return producto;
    }

    // Crea el HttpClient e inyecta el Correlation ID en el header saliente,
    // así Products y Users pueden trackearlo en sus propios logs.
    private HttpClient CrearClienteConCorrelation(string clientName, string? correlationId)
    {
        var client = _httpClientFactory.CreateClient(clientName);

        if (!string.IsNullOrEmpty(correlationId))
            client.DefaultRequestHeaders.TryAddWithoutValidation("X-Correlation-Id", correlationId);

        return client;
    }
}

// DTO que mapea la respuesta de GET /api/products/{id}
public class ProductoResponse
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    [JsonPropertyName("name")]
    public string Nombre { get; set; } = string.Empty;

    [JsonPropertyName("price")]
    public decimal Precio { get; set; }

    [JsonPropertyName("stock")]
    public int Stock { get; set; }
}