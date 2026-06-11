using Microsoft.AspNetCore.Mvc;
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
        })
        .WithTags("Orders")
        .WithName("GetOrders")
        .WithSummary("Listar órdenes")
        .WithDescription("Devuelve todas las órdenes. Opcionalmente filtra por usuarioId.")
        .Produces<IEnumerable<OrderResponse>>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

        // ─── Obtener orden por ID ─────────────────────────────
        app.MapGet("/api/orders/{id}", async (Guid id) =>
        {
            var order = await repository.GetByIdAsync(id);

            if (order is null)
                throw new NotFoundException("ORD-001", "Orden no encontrada.");

            return Results.Ok(MapToResponse(order));
        })
        .WithTags("Orders")
        .WithName("GetOrderById")
        .WithSummary("Obtener orden por ID")
        .WithDescription("Devuelve el detalle de una orden específica. Lanza ORD-001 si no existe.")
        .Produces<OrderResponse>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
        .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

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
                    producto.Precio  // precio real de Products API
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
        })
        .WithTags("Orders")
        .WithName("CreateOrder")
        .WithSummary("Crear nueva orden")
        .WithDescription("Crea una orden validando usuario (ORD-003), productos (ORD-004) y stock (ORD-005) contra las APIs externas.")
        .Produces<OrderResponse>(StatusCodes.Status201Created)
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
        .Produces<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)
        .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

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
        })
        .WithTags("Orders")
        .WithName("UpdateOrderStatus")
        .WithSummary("Actualizar estado de la orden")
        .WithDescription("Cambia el estado siguiendo las transiciones válidas: Pendiente→Confirmada→Enviada→Entregada. Lanza ORD-006 si la transición no es válida.")
        .Produces<UpdateStatusResponse>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
        .Produces<ProblemDetails>(StatusCodes.Status409Conflict)
        .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

        // ─── Verificar órdenes activas por producto (uso interno de Products API) ─────
        app.MapGet("/api/orders/producto/{productoId}/tiene-ordenes-activas",
            async (Guid productoId) =>
            {
                var tieneOrdenes = await repository.TieneOrdenesActivasAsync(productoId);
                return Results.Ok(new { tieneOrdenesActivas = tieneOrdenes });
            })
        .WithTags("Orders")
        .WithName("CheckActiveOrders")
        .WithSummary("Verificar órdenes activas por producto")
        .WithDescription("Endpoint interno usado por Products API para verificar si un producto tiene órdenes en estado Pendiente o Confirmada antes de eliminarlo.")
        .Produces(StatusCodes.Status200OK);
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