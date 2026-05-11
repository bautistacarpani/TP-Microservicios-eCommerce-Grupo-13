using Products.API.Exceptions;
using Products.API.Models;

public static class ProductEndpoints
{
    public static void MapProductEndpoints(this WebApplication app)
    {
        var products = new List<Product>();
        var idCounter = 1L;

        // GET all
        app.MapGet("/api/products", (string? categoria, string? nombre) =>
        {
            var result = products.AsEnumerable();

            if (!string.IsNullOrEmpty(categoria))
                result = result.Where(p => p.Category.Equals(categoria, StringComparison.OrdinalIgnoreCase));

            if (!string.IsNullOrEmpty(nombre))
                result = result.Where(p => p.Name.Contains(nombre, StringComparison.OrdinalIgnoreCase));

            return Results.Ok(result.ToList());
        })
        .WithTags("Products");

        // GET by id
        app.MapGet("/api/products/{id}", (long id) =>
        {
            var product = products.FirstOrDefault(p => p.Id == id);

            if (product is null)
                throw new NotFoundException(ErrorCodes.ProductoNoEncontrado, "Producto no encontrado.");

            return Results.Ok(product);
        })
        .WithTags("Products");

        // POST
        app.MapPost("/api/products", (CreateProductRequest req) =>
        {
            if (string.IsNullOrEmpty(req.Name) || req.Price <= 0 || req.Stock < 0 || string.IsNullOrEmpty(req.Category))
                throw new ValidationException(ErrorCodes.DatosInvalidos, "Los datos del producto son inválidos.");

            var existe = products.Any(p =>
                p.Name.Equals(req.Name, StringComparison.OrdinalIgnoreCase) &&
                p.Category.Equals(req.Category, StringComparison.OrdinalIgnoreCase));

            if (existe)
                throw new BusinessRuleException(ErrorCodes.NombreDuplicado,
                    $"Ya existe un producto con ese nombre en la categoría '{req.Category}'.");

            var product = new Product
            {
                Id          = idCounter++,
                Name        = req.Name,
                Description = req.Description,
                Price       = (double)req.Price,
                Stock       = req.Stock,
                Category    = req.Category,
                CreatedAt   = DateTime.UtcNow.ToString("o")
            };

            products.Add(product);
            return Results.Created($"/api/products/{product.Id}", product);
        })
        .WithTags("Products");

        // PUT
        app.MapPut("/api/products/{id}", (long id, UpdateProductRequest req) =>
        {
            var existing = products.FirstOrDefault(p => p.Id == id);

            if (existing is null)
                throw new NotFoundException(ErrorCodes.ProductoNoEncontrado, "Producto no encontrado.");

            if (string.IsNullOrEmpty(req.Name) || req.Price <= 0 || req.Stock < 0 || string.IsNullOrEmpty(req.Category))
                throw new ValidationException(ErrorCodes.DatosInvalidos, "Los datos del producto son inválidos.");

            var updated = existing with
            {
                Name        = req.Name,
                Description = req.Description,
                Price       = (double)req.Price,
                Stock       = req.Stock,
                Category    = req.Category,
                UpdatedAt   = DateTime.UtcNow.ToString("o")
            };

            products.Remove(existing);
            products.Add(updated);
            return Results.Ok(updated);
        })
        .WithTags("Products");

        // DELETE
        app.MapDelete("/api/products/{id}", (long id) =>
        {
            var product = products.FirstOrDefault(p => p.Id == id);

            if (product is null)
                throw new NotFoundException(ErrorCodes.ProductoNoEncontrado, "Producto no encontrado.");

            products.Remove(product);
            return Results.NoContent();
        })
        .WithTags("Products");
    }
}