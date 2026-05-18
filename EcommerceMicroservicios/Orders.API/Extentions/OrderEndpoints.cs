using Orders.API.Models;
using Orders.API.Exceptions;
using Orders.API.DTOs;

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
                throw new BusinessRuleException("ORD-002", "La orden debe tener al menos un item.");

            var orderItems = request.Items.Select(i => new OrderItem(
                i.ProductoId,
                i.Cantidad,
                100  // mock — precio hasta conectar Products API
      )).ToList();

            var order = new Order
            {
                Id = Guid.NewGuid(),
                UsuarioId = request.UsuarioId,
                Items = orderItems,
                FechaCreacion = DateTime.UtcNow
            };

            // calcular total
            order.Total = order.Items.Sum(i => i.PrecioUnitario * i.Cantidad);

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
                throw new NotFoundException("ORD-001", "Orden no encontrada.");

            return Results.Ok(order);
        });

        // ─────────────────────────────
        // Cambiar estado
        // ─────────────────────────────
        app.MapPut("/api/orders/{id}/status", (Guid id, string estado) =>
        {
            var order = orders.FirstOrDefault(o => o.Id == id);

            if (order is null)
                throw new NotFoundException("ORD-001", "Orden no encontrada.");

            var estadosValidos = new[] { "Pendiente", "Confirmada", "Enviada", "Entregada", "Cancelada" };

            if (!estadosValidos.Contains(estado))
                throw new BusinessRuleException("ORD-006", $"El estado '{estado}' no es válido.");
           
            // Transiciones válidas según el enunciado
            var transiciones = new Dictionary<string, List<string>>
            {
                { "Pendiente",  ["Confirmada", "Cancelada"] },
                { "Confirmada", ["Enviada",    "Cancelada"] },
                { "Enviada",    ["Entregada"]               },
                { "Entregada",  []                          },
                { "Cancelada",  []                          },
            };

            if (!transiciones[order.Estado].Contains(estado))
                throw new BusinessRuleException("ORD-006",
                    $"Una orden en estado '{order.Estado}' no puede cambiar a '{estado}'.");

            order.Estado = estado;

            return Results.Ok(order);
        });
    }
}
