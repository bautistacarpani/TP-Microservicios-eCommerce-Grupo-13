using Cart.API.Exceptions;
using Cart.API.Models;

public static class CartEndpoints
{
    public static void MapCartEndpoints(this WebApplication app)
    {
        var carts = new Dictionary<Guid, Cart.API.Models.Cart>();

        // GET /api/cart/{userId}
        app.MapGet("/api/cart/{userId}", (Guid userId) =>
        {
            if (!carts.TryGetValue(userId, out var cart))
                throw new NotFoundException("CRT-001", "Carrito no encontrado.");

            return Results.Ok(cart);
        })
        .WithTags("Cart");

        // POST /api/cart/{userId}/items
        app.MapPost("/api/cart/{userId}/items", (Guid userId, AddToCartRequest req) =>
        {
            if (req.Quantity <= 0)
                throw new ValidationException("CRT-004", "Cantidad inválida.");

            if (!carts.TryGetValue(userId, out var cart))
            {
                cart = new Cart.API.Models.Cart
                {
                    UserId = userId,
                    Items = new List<CartItem>(),
                    UpdatedAt = DateTime.UtcNow
                };
            }

            var existing = cart.Items.FirstOrDefault(i => i.ProductId == req.ProductId);
            var updatedItems = cart.Items.ToList();

            if (existing is not null)
            {
                updatedItems.Remove(existing);
                updatedItems.Add(existing with { Quantity = existing.Quantity + req.Quantity });
            }
            else
            {
                updatedItems.Add(new CartItem { ProductId = req.ProductId, Quantity = req.Quantity });
            }

            var updated = cart with { Items = updatedItems, UpdatedAt = DateTime.UtcNow };
            carts[userId] = updated;

            return Results.Ok(updated);
        })
        .WithTags("Cart");

        // PUT /api/cart/{userId}/items/{productId}
        app.MapPut("/api/cart/{userId}/items/{productId}", (Guid userId, Guid productId, UpdateCartItemRequest req) =>
        {
            if (req.Quantity <= 0)
                throw new ValidationException("CRT-004", "Cantidad inválida.");

            if (!carts.TryGetValue(userId, out var cart))
                throw new NotFoundException("CRT-001", "Carrito no encontrado.");

            var existing = cart.Items.FirstOrDefault(i => i.ProductId == productId);
            if (existing is null)
                throw new NotFoundException("CRT-002", "Producto no encontrado en el carrito.");

            var updatedItems = cart.Items.ToList();
            updatedItems.Remove(existing);
            updatedItems.Add(existing with { Quantity = req.Quantity });

            var updated = cart with { Items = updatedItems, UpdatedAt = DateTime.UtcNow };
            carts[userId] = updated;

            return Results.Ok(updated);
        })
        .WithTags("Cart");

        // DELETE /api/cart/{userId}/items/{productId}
        app.MapDelete("/api/cart/{userId}/items/{productId}", (Guid userId, Guid productId) =>
        {
            if (!carts.TryGetValue(userId, out var cart))
                throw new NotFoundException("CRT-001", "Carrito no encontrado.");

            var item = cart.Items.FirstOrDefault(i => i.ProductId == productId);
            if (item is null)
                throw new NotFoundException("CRT-002", "Producto no encontrado en el carrito.");

            var updatedItems = cart.Items.ToList();
            updatedItems.Remove(item);

            carts[userId] = cart with { Items = updatedItems, UpdatedAt = DateTime.UtcNow };

            return Results.NoContent();
        })
        .WithTags("Cart");

        // DELETE /api/cart/{userId}
        app.MapDelete("/api/cart/{userId}", (Guid userId) =>
        {
            if (!carts.ContainsKey(userId))
                throw new NotFoundException("CRT-001", "Carrito no encontrado.");

            carts.Remove(userId);
            return Results.NoContent();
        })
        .WithTags("Cart");
    }
}