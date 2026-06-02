namespace Products.API.Models;

/// <summary>Representa un producto del catálogo.</summary>
public record Product
{
    /// <summary>ID único del producto.</summary>
    public Guid Id { get; init; }

    /// <summary>Nombre del producto.</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>Descripción opcional del producto.</summary>
    public string? Description { get; init; }

    /// <summary>Precio del producto.</summary>
    public double Price { get; init; }

    /// <summary>Stock disponible.</summary>
    public long Stock { get; init; }

    /// <summary>Categoría del producto.</summary>
    public string Category { get; init; } = string.Empty;

    /// <summary>Fecha de creación en formato ISO 8601.</summary>
    public string CreatedAt { get; init; } = string.Empty;

    /// <summary>Fecha de última actualización.</summary>
    public string? UpdatedAt { get; init; }
}

/// <summary>Request para crear un nuevo producto.</summary>
/// <param name="Name" example="Notebook Dell XPS 15">Nombre del producto.</param>
/// <param name="Description" example="Laptop 15 pulgadas, 32GB RAM">Descripción opcional.</param>
/// <param name="Price" example="1500.00">Precio del producto.</param>
/// <param name="Stock" example="10">Stock disponible.</param>
/// <param name="Category" example="Electrónica">Categoría del producto.</param>
public record CreateProductRequest(
    string Name,
    string? Description,
    decimal Price,
    int Stock,
    string Category);

/// <summary>Request para actualizar un producto existente.</summary>
/// <param name="Name" example="Notebook Dell XPS 15">Nombre del producto.</param>
/// <param name="Description" example="Laptop 15 pulgadas, 64GB RAM">Descripción opcional.</param>
/// <param name="Price" example="1750.00">Precio del producto.</param>
/// <param name="Stock" example="8">Stock disponible.</param>
/// <param name="Category" example="Electrónica">Categoría del producto.</param>
public record UpdateProductRequest(
    string Name,
    string? Description,
    decimal Price,
    int Stock,
    string Category);