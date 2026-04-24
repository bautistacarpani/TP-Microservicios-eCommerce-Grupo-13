using Products.API.Models;

public static class ProductEndpoints
{
    public static void MapProductEndpoints(this WebApplication app)
    {
        var products = new List<Product>();
        var idCounter = 1L;

        // GET all
        app.MapGet("/api/products", () =>
        {
            return Results.Ok(products);
        })
        .WithTags("Products");

        // GET by id
        app.MapGet("/api/products/{id}", (long id) =>
        {
            var product = products.FirstOrDefault(p => p.Id == id);
            return product is not null ? Results.Ok(product) : Results.NotFound();
        })
        .WithTags("Products");

        // POST
        app.MapPost("/api/products", (CreateProductRequest req) =>
        {
            var product = new Product
            {
                Id = idCounter++,
                Name = req.Name,
                Description = req.Description,
                Price = (double)req.Price,
                Stock = req.Stock,
                Category = req.Category,
                CreatedAt = DateTime.UtcNow.ToString("o")
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
                return Results.NotFound();

            var updated = existing with
            {
                Name = req.Name,
                Description = req.Description,
                Price = (double)req.Price,
                Stock = req.Stock,
                Category = req.Category,
                UpdatedAt = DateTime.UtcNow.ToString("o")
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
                return Results.NotFound();

            products.Remove(product);
            return Results.NoContent();
        })
        .WithTags("Products");
    }
}