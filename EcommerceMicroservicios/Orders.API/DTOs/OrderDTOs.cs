namespace Orders.API.DTOs;

// ─── Requests ───────────────────────────────────────────

public record CreateOrderRequest(
    Guid UsuarioId,
    List<CreateOrderItemRequest> Items
);

public record CreateOrderItemRequest(
    Guid ProductoId,
    int Cantidad
);

public record UpdateStatusRequest(
    string Estado
);

// ─── Responses ──────────────────────────────────────────

public record OrderResponse(
    Guid Id,
    Guid UsuarioId,
    List<OrderItemResponse> Items,
    decimal Total,
    string Estado,
    DateTime FechaCreacion
);

public record OrderItemResponse(
    Guid ProductoId,
    int Cantidad,
    decimal PrecioUnitario
);

public record UpdateStatusResponse(
    Guid Id,
    string Estado,
    DateTime FechaActualizacion
);