namespace Orders.API.Models;

// ── Entidad principal ─────────────────────────────────────
public record Order
{
    public Guid Id { get; init; }                      // Identificador de la orden
    public Guid UsuarioId { get; init; }               // Usuario que hizo la compra
    public List<OrderItem> Items { get; init; } = [];  // Productos de la orden
    public double Total { get; set; }                  // Total calculado
    public string Estado { get; set; } = "Pendiente";  // Pendiente | Pagada | Cancelada
    public DateTime FechaCreacion { get; init; }       // Timestamp
}

// ── Subentidad ────────────────────────────────────────────
public record OrderItem
{
    public Guid ProductoId { get; init; }   // ID del producto
    public string Nombre { get; init; } = "";
    public double Precio { get; init; }
    public int Cantidad { get; init; }
}

// DTOs
public record CreateOrderRequest(Guid UsuarioId,
    List<CreateOrderItemRequest> Items);
public record CreateOrderItemRequest(
    Guid ProductoId,
    int Cantidad
);
public record OrderResponse(
    Guid Id,
    Guid UsuarioId,
    List<OrderItem> Items,
    double Total,
    string Estado,
    DateTime FechaCreacion
);

