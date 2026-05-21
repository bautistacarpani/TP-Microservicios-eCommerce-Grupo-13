namespace Cart.API.Models;

/// <summary>Representa el carrito de compras de un usuario.</summary>
public record Cart
{
    /// <summary>ID del usuario dueño del carrito.</summary>
    public Guid UserId { get; init; }

    /// <summary>Lista de productos en el carrito.</summary>
    public List<CartItem> Items { get; init; } = new();

    /// <summary>Fecha de última actualización.</summary>
    public DateTime UpdatedAt { get; init; }
}

/// <summary>Representa un item dentro del carrito.</summary>
public record CartItem
{
    /// <summary>ID del producto.</summary>
    public Guid ProductId { get; init; }

    /// <summary>Cantidad del producto en el carrito.</summary>
    public int Quantity { get; init; }
}

/// <summary>Request para agregar un producto al carrito.</summary>
public record AddToCartRequest(
    /// <summary>ID del producto a agregar.</summary>
    Guid ProductId,
    /// <summary>Cantidad a agregar. Debe ser mayor a 0.</summary>
    int Quantity);

/// <summary>Request para actualizar la cantidad de un producto en el carrito.</summary>
public record UpdateCartItemRequest(
    /// <summary>Nueva cantidad. Debe ser mayor a 0.</summary>
    int Quantity);