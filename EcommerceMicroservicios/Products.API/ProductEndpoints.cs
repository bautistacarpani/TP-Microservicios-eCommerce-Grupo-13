using Microsoft.AspNetCore.Mvc;
using Products.API.Exceptions;
using Products.API.Models;
using Products.API.Services;

// ══════════════════════════════════════════════════════════════════════
// PRODUCT ENDPOINTS
// Agrupa todos los endpoints REST de la API de productos.
// Cada endpoint valida los datos, ejecuta la lógica de negocio
// a través del ProductRepository y devuelve la respuesta correspondiente.
// ══════════════════════════════════════════════════════════════════════
public static class ProductEndpoints
{
    public static void MapProductEndpoints(this WebApplication app)
    {
        // ──────────────────────────────────────────────────────────────
        // GET /api/products
        // Lista todos los productos con filtros opcionales por categoría y nombre.
        // ──────────────────────────────────────────────────────────────
        app.MapGet("/api/products", async (ProductRepository repo, string? categoria, string? nombre) =>
        {
            var products = await repo.GetAllAsync(categoria, nombre);
            return Results.Ok(products);
        })
        .WithTags("Products")
        .WithSummary("Listar productos")
        .WithDescription("Devuelve todos los productos. Opcionalmente filtrar por categoría y/o nombre.")
        .Produces<IEnumerable<Product>>(200)
        .Produces<ProblemDetails>(500);

        // ──────────────────────────────────────────────────────────────
        // GET /api/products/{id}
        // Obtiene un producto específico por su ID (Guid).
        // Error PRD-001 si no existe.
        // ──────────────────────────────────────────────────────────────
        app.MapGet("/api/products/{id}", async (ProductRepository repo, Guid id) =>
        {
            var product = await repo.GetByIdAsync(id);
            if (product is null)
                throw new NotFoundException(ErrorCodes.ProductoNoEncontrado, "Producto no encontrado.");
            return Results.Ok(product);
        })
        .WithTags("Products")
        .WithSummary("Obtener producto por ID")
        .WithDescription("Devuelve el detalle de un producto específico.")
        .Produces<Product>(200)
        .Produces<ProblemDetails>(404)
        .Produces<ProblemDetails>(500);

        // ──────────────────────────────────────────────────────────────
        // POST /api/products
        // Crea un nuevo producto en el catálogo.
        // Error PRD-002 si los datos son inválidos.
        // Error PRD-003 si ya existe un producto con ese nombre en la misma categoría.
        // ──────────────────────────────────────────────────────────────
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
        .WithTags("Products")
        .WithSummary("Crear producto")
        .WithDescription("Crea un nuevo producto en el catálogo.")
        .Produces<Product>(201)
        .Produces<ProblemDetails>(400)
        .Produces<ProblemDetails>(409)
        .Produces<ProblemDetails>(500);

         // ──────────────────────────────────────────────────────────────
        // PUT /api/products/{id}
        // Actualiza un producto existente.
        // Error PRD-001 si no existe.
        // Error PRD-002 si los datos son inválidos.
        // Error PRD-003 si el nuevo nombre ya existe en la misma categoría.
        // ──────────────────────────────────────────────────────────────
        app.MapPut("/api/products/{id}", async (ProductRepository repo, Guid id, UpdateProductRequest req) =>
        {
            if (string.IsNullOrEmpty(req.Name) || req.Price <= 0 || req.Stock < 0 || string.IsNullOrEmpty(req.Category))
                throw new ValidationException(ErrorCodes.DatosInvalidos, "Los datos del producto son inválidos.");

            var existing = await repo.GetByIdAsync(id);
            if (existing is null)
                throw new NotFoundException(ErrorCodes.ProductoNoEncontrado, "Producto no encontrado.");

            if (await repo.ExistsAsync(req.Name, req.Category, id))
                return Results.Problem(
                    title: "Conflict",
                    detail: $"Ya existe un producto con ese nombre en la categoría '{req.Category}'.",
                    statusCode: 409,
                    extensions: new Dictionary<string, object?> { ["errorCode"] = "PRD-003", ["errorMessage"] = $"Ya existe un producto con ese nombre en la categoría '{req.Category}'." }
                );

            var updated = await repo.UpdateAsync(id, req);
            return Results.Ok(updated);
        })
        .WithTags("Products")
        .WithSummary("Actualizar producto")
        .WithDescription("Actualiza los datos de un producto existente.")
        .Produces<Product>(200)
        .Produces<ProblemDetails>(400)
        .Produces<ProblemDetails>(404)
        .Produces<ProblemDetails>(409)
        .Produces<ProblemDetails>(500);
        
        // ──────────────────────────────────────────────────────────────
        // DELETE /api/products/{id}
        // Elimina un producto del catálogo.
        // Error PRD-001 si no existe.
        // Error PRD-004 si el producto tiene órdenes activas en Orders.API.
        // Nota: la verificación de órdenes activas requiere que Orders.API esté corriendo.
        // ──────────────────────────────────────────────────────────────
        app.MapDelete("/api/products/{id}", async (
            ProductRepository repo,
            Guid id,
            IHttpClientFactory httpClientFactory,
            HttpContext context) =>

        {
            var existing = await repo.GetByIdAsync(id);
            if (existing is null)
                throw new NotFoundException(ErrorCodes.ProductoNoEncontrado, "Producto no encontrado.");

            var correlationId = context.Request.Headers["X-Correlation-Id"]
                .FirstOrDefault() ?? Guid.NewGuid().ToString();
            var httpClient = httpClientFactory.CreateClient("OrdersClient");
            httpClient.DefaultRequestHeaders.Add("X-Correlation-Id", correlationId);
            var response = await httpClient.GetAsync($"/api/orders/producto/{id}/tiene-ordenes-activas");

            if (response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadFromJsonAsync<TieneOrdenesResponse>();
                if (body?.TieneOrdenesActivas == true)
                    throw new BusinessRuleException(ErrorCodes.ProductoConOrdenesActivas,
                        "El producto tiene órdenes activas y no puede eliminarse.");
            }

            await repo.DeleteAsync(id);
            return Results.NoContent();
        })


        .WithTags("Products")
        .WithSummary("Eliminar producto")
        .WithDescription("Elimina un producto del catálogo.")
        .Produces(204)
        .Produces<ProblemDetails>(404)
        .Produces<ProblemDetails>(409)
        .Produces<ProblemDetails>(500);
            }
        }

        // ══════════════════════════════════════════════════════════════════════
        // DTO INTERNO
        // Mapea la respuesta de Orders.API al consultar órdenes activas.
        // ══════════════════════════════════════════════════════════════════════
        public record TieneOrdenesResponse(bool TieneOrdenesActivas);