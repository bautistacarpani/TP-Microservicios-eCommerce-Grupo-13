using Orders.API.Models;
using Orders.API.Exceptions;

namespace Orders.API.Extensions;

public static class OrderEndpoints
{
    private static readonly List<Order> orders = [];

    public static void MapOrderEndpoints(this WebApplication app)
    {
        // ─────────────────────────────
        // Crear orden
        // ─────────────────────────────
        app.MapPost("/api/orders", (CreateOrderRequest request) =>
        {
            if (!request.Items.Any())
                throw new BusinessRuleException("ORD-001", "La orden debe tener al menos un item.");

            var orderItems = request.Items.Select(i => new OrderItem
            {
                ProductoId = i.ProductoId,
                Cantidad = i.Cantidad,
                Nombre = $"Producto {i.ProductoId}", // mock
                Precio = 100 // mock
            }).ToList();

            var order = new Order
            {
                Id = Guid.NewGuid(),
                UsuarioId = request.UsuarioId,
                Items = orderItems,
                FechaCreacion = DateTime.UtcNow
            };

            // calcular total
            order.Total = order.Items.Sum(i => i.Precio * i.Cantidad);

            orders.Add(order);

            return Results.Created($"/api/orders/{order.Id}", order);
        });

        // ─────────────────────────────
        // Obtener orden por ID
        // ─────────────────────────────
        app.MapGet("/api/orders/{id}", (Guid id) =>
        {
            var order = orders.FirstOrDefault(o => o.Id == id);

            if (order is null)
                throw new NotFoundException("ORD-404", "Orden no encontrada.");

            return Results.Ok(order);
        });

        // ─────────────────────────────
        // Cambiar estado
        // ─────────────────────────────
        app.MapPut("/api/orders/{id}/status", (Guid id, string estado) =>
        {
            var order = orders.FirstOrDefault(o => o.Id == id);

            if (order is null)
                throw new NotFoundException("ORD-404", "Orden no encontrada.");

            var estadosValidos = new[] { "Pendiente", "Pagada", "Cancelada" };

            if (!estadosValidos.Contains(estado))
                throw new BusinessRuleException("ORD-002", "Estado inválido.");

            order.Estado = estado;

            return Results.Ok(order);
        });
    }
}
