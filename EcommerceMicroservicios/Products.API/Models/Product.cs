namespace Products.API.Models;

public record Product
{
    public long Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public double Price { get; init; }
    public long Stock { get; init; }
    public string Category { get; init; } = string.Empty;
    public string CreatedAt { get; init; } = string.Empty;
    public string? UpdatedAt { get; init; }
}

public record CreateProductRequest(
    string Name,
    string? Description,
    decimal Price,
    int Stock,
    string Category);

public record UpdateProductRequest(
    string Name,
    string? Description,
    decimal Price,
    int Stock,
    string Category);