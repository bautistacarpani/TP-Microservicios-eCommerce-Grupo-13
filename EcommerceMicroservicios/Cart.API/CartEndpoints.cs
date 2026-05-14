using Cart.API.Exceptions;
using Cart.API.Models;
using Cart.API.Services;

public static class CartEndpoints
{
    public static void MapCartEndpoints(this WebApplication app)
    {
        // GET /api/cart/{userId}
        app.MapGet("/api/cart/{userId}", async (CartRepository repo, Guid userId) =>
        {
            var cart = await repo.GetByUserIdAsync(userId);

            if (cart is null)
                throw new NotFoundException(ErrorCodes.CarritoNoEncontrado, "Carrito no encontrado.");

            return Results.Ok(cart);
        })
        .WithTags("Cart");

        // POST /api/cart/{userId}/items
        app.MapPost("/api/cart/{userId}/items", async (CartRepository repo, Guid userId, AddToCartRequest req) =>
        {
            if (req.Quantity <= 0)
                throw new ValidationException(ErrorCodes.CantidadInvalida, "Cantidad inválida.");

            var cart = await repo.UpsertItemAsync(userId, req.ProductId, req.Quantity);
            return Results.Ok(cart);
        })
        .WithTags("Cart");

        // PUT /api/cart/{userId}/items/{productId}
        app.MapPut("/api/cart/{userId}/items/{productId}", async (CartRepository repo, Guid userId, Guid productId, UpdateCartItemRequest req) =>
        {
            if (req.Quantity <= 0)
                throw new ValidationException(ErrorCodes.CantidadInvalida, "Cantidad inválida.");

            var exists = await repo.ItemExistsAsync(userId, productId);
            if (!exists)
                throw new NotFoundException(ErrorCodes.ProductoNoEncontrado, "Producto no encontrado en el carrito.");

            var cart = await repo.UpdateItemQuantityAsync(userId, productId, req.Quantity);
            return Results.Ok(cart);
        })
        .WithTags("Cart");

        // DELETE /api/cart/{userId}/items/{productId}
        app.MapDelete("/api/cart/{userId}/items/{productId}", async (CartRepository repo, Guid userId, Guid productId) =>
        {
            var exists = await repo.ItemExistsAsync(userId, productId);
            if (!exists)
                throw new NotFoundException(ErrorCodes.ProductoNoEncontrado, "Producto no encontrado en el carrito.");

            await repo.RemoveItemAsync(userId, productId);
            return Results.NoContent();
        })
        .WithTags("Cart");

        // DELETE /api/cart/{userId}
        app.MapDelete("/api/cart/{userId}", async (CartRepository repo, Guid userId) =>
        {
            var cart = await repo.GetByUserIdAsync(userId);
            if (cart is null)
                throw new NotFoundException(ErrorCodes.CarritoNoEncontrado, "Carrito no encontrado.");

            await repo.ClearCartAsync(userId);
            return Results.NoContent();
        })
        .WithTags("Cart");
    }
}