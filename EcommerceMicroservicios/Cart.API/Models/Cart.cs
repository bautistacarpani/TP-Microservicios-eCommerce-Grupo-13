namespace Cart.API.Models;

public record Cart
{
    public Guid UserId { get; init; }
    public List<CartItem> Items { get; init; } = new();
    public DateTime UpdatedAt { get; init; }
}

public record CartItem
{
    public Guid ProductId { get; init; }
    public int Quantity { get; init; }
}

public record AddToCartRequest(
    Guid ProductId,
    int Quantity);

public record UpdateCartItemRequest(
    int Quantity);