using Products.API.Exceptions;
using Products.API.Models;
using Products.API.Services;

public static class ProductEndpoints
{
    public static void MapProductEndpoints(this WebApplication app)
    {
        // GET all
        app.MapGet("/api/products", async (ProductRepository repo, string? categoria, string? nombre) =>
        {
            var products = await repo.GetAllAsync(categoria, nombre);
            return Results.Ok(products);
        })
        .WithTags("Products");

        // GET by id
        app.MapGet("/api/products/{id}", async (ProductRepository repo, long id) =>
        {
            var product = await repo.GetByIdAsync(id);

            if (product is null)
                throw new NotFoundException(ErrorCodes.ProductoNoEncontrado, "Producto no encontrado.");

            return Results.Ok(product);
        })
        .WithTags("Products");

        // POST
        app.MapPost("/api/products", async (ProductRepository repo, CreateProductRequest req) =>
        {
            if (string.IsNullOrEmpty(req.Name) || req.Price <= 0 || req.Stock < 0 || string.IsNullOrEmpty(req.Category))
                throw new ValidationException(ErrorCodes.DatosInvalidos, "Los datos del producto son inválidos.");

            if (await repo.ExistsAsync(req.Name, req.Category))
    return Results.Problem(
        title: "Conflict",
        detail: $"Ya existe un producto con ese nombre en la categoría '{req.Category}'.",
        statusCode: 409,
        extensions: new Dictionary<string, object?> { ["errorCode"] = "PRD-003", ["errorMessage"] = $"Ya existe un producto con ese nombre en la categoría '{req.Category}'." }
    );
            var product = await repo.CreateAsync(req);
            return Results.Created($"/api/products/{product.Id}", product);
        })
        .WithTags("Products");

        // PUT
        app.MapPut("/api/products/{id}", async (ProductRepository repo, long id, UpdateProductRequest req) =>
        {
            if (string.IsNullOrEmpty(req.Name) || req.Price <= 0 || req.Stock < 0 || string.IsNullOrEmpty(req.Category))
                throw new ValidationException(ErrorCodes.DatosInvalidos, "Los datos del producto son inválidos.");

            var existing = await repo.GetByIdAsync(id);
            if (existing is null)
                throw new NotFoundException(ErrorCodes.ProductoNoEncontrado, "Producto no encontrado.");

            if (await repo.ExistsAsync(req.Name, req.Category, id))
                throw new BusinessRuleException(ErrorCodes.NombreDuplicado,
                    $"Ya existe un producto con ese nombre en la categoría '{req.Category}'.");

            var updated = await repo.UpdateAsync(id, req);
            return Results.Ok(updated);
        })
        .WithTags("Products");

        // DELETE
        app.MapDelete("/api/products/{id}", async (ProductRepository repo, long id) =>
        {
            var existing = await repo.GetByIdAsync(id);
            if (existing is null)
                throw new NotFoundException(ErrorCodes.ProductoNoEncontrado, "Producto no encontrado.");

            await repo.DeleteAsync(id);
            return Results.NoContent();
        })
        .WithTags("Products");
    }
}