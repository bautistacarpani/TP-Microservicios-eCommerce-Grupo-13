using Orders.API.DTOs;
using Orders.API.Exceptions;
using Orders.API.Models;
using Orders.API.Services;

namespace Orders.API.Extensions;

public static class OrderEndpoints
{
    public static void MapOrderEndpoints(this WebApplication app)
    {
        var repository = app.Services.GetRequiredService<OrderRepository>();

        // ─── Listar órdenes ───────────────────────────────────
        app.MapGet("/api/orders", async (Guid? usuarioId) =>
        {
            var orders = await repository.GetAllAsync(usuarioId);
            return Results.Ok(orders.Select(MapToResponse));
        }).WithTags("Orders");

        // ─── Obtener orden por ID ─────────────────────────────
        app.MapGet("/api/orders/{id}", async (Guid id) =>
        {
            var order = await repository.GetByIdAsync(id);

            if (order is null)
                throw new NotFoundException("ORD-001", "Orden no encontrada.");

            return Results.Ok(MapToResponse(order));
        }).WithTags("Orders");

        // ─── Crear orden ──────────────────────────────────────
        app.MapPost("/api/orders", async (
            CreateOrderRequest request,
            ExternalServicesClient externalServices,
            HttpContext context) =>
        {
            if (request.Items == null || !request.Items.Any())
                throw new BusinessRuleException("ORD-002",
                    "La orden debe tener al menos un item.");

            if (request.Items.Any(i => i.Cantidad <= 0))
                throw new BusinessRuleException("ORD-002",
                    "La cantidad de cada item debe ser mayor a cero.");

            // Leemos el ID para propagarlo a Products y Users en las llamadas salientes
            var correlationId = context.Items["X-Correlation-Id"]?.ToString();

            // Verificamos que el usuario existe en Users API → ORD-003
            await externalServices.VerificarUsuarioAsync(request.UsuarioId, correlationId);

            // Para cada producto verificamos existencia, stock y capturamos el precio real
            var items = new List<OrderItem>();
            foreach (var itemRequest in request.Items)
            {
                // Lanza ORD-004 si el producto no existe, ORD-005 si no hay stock suficiente
                var producto = await externalServices.ObtenerYValidarProductoAsync(
                    itemRequest.ProductoId,
                    itemRequest.Cantidad,
                    correlationId);

                items.Add(new OrderItem(
                    itemRequest.ProductoId,
                    itemRequest.Cantidad,
                    producto.Precio  // precio real de Products API, reemplaza el TODO
                ));
            }

            var order = new Order
            {
                Id = Guid.NewGuid(),
                UsuarioId = request.UsuarioId,
                Items = items,
                FechaCreacion = DateTime.UtcNow
            };

            order.Total = order.Items.Sum(i => i.PrecioUnitario * i.Cantidad);

            await repository.CreateAsync(order);

            return Results.Created($"/api/orders/{order.Id}", MapToResponse(order));
        }).WithTags("Orders");

        // ─── Cambiar estado ───────────────────────────────────
        app.MapPut("/api/orders/{id}/status", async (Guid id, UpdateStatusRequest request) =>
        {
            var order = await repository.GetByIdAsync(id);

            if (order is null)
                throw new NotFoundException("ORD-001", "Orden no encontrada.");

            var estadosValidos = new[]
            {
                "Pendiente", "Confirmada", "Enviada", "Entregada", "Cancelada"
            };

            if (!estadosValidos.Contains(request.Estado))
                throw new ConflictException("ORD-006",
                    $"El estado '{request.Estado}' no es válido.");

            var transiciones = new Dictionary<string, List<string>>
            {
                { "Pendiente",  ["Confirmada", "Cancelada"] },
                { "Confirmada", ["Enviada",    "Cancelada"] },
                { "Enviada",    ["Entregada"]               },
                { "Entregada",  []                          },
                { "Cancelada",  []                          },
            };

            if (!transiciones[order.Estado].Contains(request.Estado))
                throw new ConflictException("ORD-006",
                    $"Una orden en estado '{order.Estado}' no puede " +
                    $"cambiar a '{request.Estado}'.");

            var updated = await repository.UpdateStatusAsync(id, request.Estado);

            return Results.Ok(new UpdateStatusResponse(
                updated!.Id,
                updated.Estado,
                updated.FechaActualizacion!.Value
            ));
        }).WithTags("Orders");

        // ─── Verificar órdenes activas por producto (uso interno de Products API) ─────
        app.MapGet("/api/orders/producto/{productoId}/tiene-ordenes-activas",
            async (Guid productoId) =>
            {
                var tieneOrdenes = await repository.TieneOrdenesActivasAsync(productoId);
                return Results.Ok(new { tieneOrdenesActivas = tieneOrdenes });
            })
            .WithTags("Orders");
    }

    private static OrderResponse MapToResponse(Order order) => new(
        order.Id,
        order.UsuarioId,
        order.Items.Select(i => new OrderItemResponse(
            i.ProductoId,
            i.Cantidad,
            i.PrecioUnitario
        )).ToList(),
        order.Total,
        order.Estado,
        order.FechaCreacion
    );
}