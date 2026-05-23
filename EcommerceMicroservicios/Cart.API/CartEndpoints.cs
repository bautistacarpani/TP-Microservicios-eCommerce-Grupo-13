using Microsoft.AspNetCore.Mvc;
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
        .WithTags("Cart")
        .WithSummary("Obtener carrito del usuario")
        .WithDescription("Devuelve el carrito activo del usuario con todos sus items.")
        .Produces<Cart.API.Models.Cart>(200)
        .Produces<ProblemDetails>(404)
        .Produces<ProblemDetails>(500);

        // POST /api/cart/{userId}/items
app.MapPost("/api/cart/{userId}/items", async (
    CartRepository repo,
    Guid userId,
    AddToCartRequest req,
    IHttpClientFactory httpClientFactory,
    HttpContext context) =>
{
    if (req.Quantity <= 0)
        throw new ValidationException(ErrorCodes.CantidadInvalida, "Cantidad inválida.");

    // Obtener Correlation ID
    var correlationId = context.Request.Headers["X-Correlation-Id"]
        .FirstOrDefault() ?? Guid.NewGuid().ToString();

    // Validar que el producto existe en Products.API
    var httpClient = httpClientFactory.CreateClient("ProductsClient");
    httpClient.DefaultRequestHeaders.Remove("X-Correlation-Id");
    httpClient.DefaultRequestHeaders.Add("X-Correlation-Id", correlationId);

    var response = await httpClient.GetAsync($"/api/products/{req.ProductId}");

    if (!response.IsSuccessStatusCode)
        throw new NotFoundException(ErrorCodes.ProductoNoEncontrado,
            $"El producto con ID '{req.ProductId}' no existe en el catálogo.");

    var cart = await repo.UpsertItemAsync(userId, req.ProductId, req.Quantity);
    return Results.Ok(cart);
})
.WithTags("Cart")
.WithSummary("Agregar producto al carrito")
.WithDescription("Agrega un producto al carrito. Valida que el producto exista en Products.API.")
.Produces<Cart.API.Models.Cart>(200)
.Produces<ProblemDetails>(400)
.Produces<ProblemDetails>(404)
.Produces<ProblemDetails>(500);

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
        .WithTags("Cart")
        .WithSummary("Actualizar cantidad de un producto")
        .WithDescription("Actualiza la cantidad de un producto que ya está en el carrito.")
        .Produces<Cart.API.Models.Cart>(200)
        .Produces<ProblemDetails>(400)
        .Produces<ProblemDetails>(404)
        .Produces<ProblemDetails>(500);

        // DELETE /api/cart/{userId}/items/{productId}
        app.MapDelete("/api/cart/{userId}/items/{productId}", async (CartRepository repo, Guid userId, Guid productId) =>
        {
            var exists = await repo.ItemExistsAsync(userId, productId);
            if (!exists)
                throw new NotFoundException(ErrorCodes.ProductoNoEncontrado, "Producto no encontrado en el carrito.");
            await repo.RemoveItemAsync(userId, productId);
            return Results.NoContent();
        })
        .WithTags("Cart")
        .WithSummary("Quitar un producto del carrito")
        .WithDescription("Elimina un producto específico del carrito.")
        .Produces(204)
        .Produces<ProblemDetails>(404)
        .Produces<ProblemDetails>(500);

        // DELETE /api/cart/{userId}
        app.MapDelete("/api/cart/{userId}", async (CartRepository repo, Guid userId) =>
        {
            var cart = await repo.GetByUserIdAsync(userId);
            if (cart is null)
                throw new NotFoundException(ErrorCodes.CarritoNoEncontrado, "Carrito no encontrado.");
            await repo.ClearCartAsync(userId);
            return Results.NoContent();
        })
        .WithTags("Cart")
        .WithSummary("Vaciar carrito completo")
        .WithDescription("Elimina todos los productos del carrito del usuario.")
        .Produces(204)
        .Produces<ProblemDetails>(404)
        .Produces<ProblemDetails>(500);
    }
}