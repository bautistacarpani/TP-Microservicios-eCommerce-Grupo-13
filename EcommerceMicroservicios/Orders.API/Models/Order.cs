namespace Orders.API.Models;

// ── Entidad principal ─────────────────────────────────────
public class Order
{
    public Guid Id { get; set; }
    public Guid UsuarioId { get; set; }
    public List<OrderItem> Items { get; set; } = [];
    public decimal Total { get; set; }
    public string Estado { get; set; } = "Pendiente";
    public DateTime FechaCreacion { get; set; }
    public DateTime? FechaActualizacion { get; set; }
}

// ── Subentidad ────────────────────────────────────────────
public record OrderItem(
    Guid ProductoId,
    int Cantidad,
    decimal PrecioUnitario
);


