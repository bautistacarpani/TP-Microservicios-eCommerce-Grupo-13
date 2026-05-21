namespace Products.API.Models;

/// <summary>Representa un producto del catálogo.</summary>
public record Product
{
    /// <summary>ID único del producto.</summary>
    public long Id { get; init; }

    /// <summary>Nombre del producto.</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>Descripción opcional del producto.</summary>
    public string? Description { get; init; }

    /// <summary>Precio del producto.</summary>
    public double Price { get; init; }

    /// <summary>Stock disponible.</summary>
    public long Stock { get; init; }

    /// <summary>Categoría del producto (ej: Electrónica, Ropa, Hogar).</summary>
    public string Category { get; init; } = string.Empty;

    /// <summary>Fecha de creación en formato ISO 8601.</summary>
    public string CreatedAt { get; init; } = string.Empty;

    /// <summary>Fecha de última actualización en formato ISO 8601.</summary>
    public string? UpdatedAt { get; init; }
}

/// <summary>Request para crear un nuevo producto.</summary>
public record CreateProductRequest(
    /// <summary>Nombre del producto.</summary>
    string Name,
    /// <summary>Descripción opcional.</summary>
    string? Description,
    /// <summary>Precio del producto. Debe ser mayor a 0.</summary>
    decimal Price,
    /// <summary>Stock inicial. No puede ser negativo.</summary>
    int Stock,
    /// <summary>Categoría del producto.</summary>
    string Category);

/// <summary>Request para actualizar un producto existente.</summary>
public record UpdateProductRequest(
    /// <summary>Nuevo nombre del producto.</summary>
    string Name,
    /// <summary>Nueva descripción opcional.</summary>
    string? Description,
    /// <summary>Nuevo precio. Debe ser mayor a 0.</summary>
    decimal Price,
    /// <summary>Nuevo stock. No puede ser negativo.</summary>
    int Stock,
    /// <summary>Nueva categoría.</summary>
    string Category);