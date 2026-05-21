using Dapper;
using Microsoft.Data.Sqlite;
using Cart.API.Models;

namespace Cart.API.Services;

public class CartRepository
{
    private readonly string _connectionString;
    private readonly ILogger<CartRepository> _logger;

    public CartRepository(IConfiguration config, ILogger<CartRepository> logger)
    {
        _connectionString = config.GetConnectionString("DefaultConnection")
            ?? "Data Source=cart.db";
        _logger = logger;
    }

    private SqliteConnection CreateConnection() => new(_connectionString);

    public async Task<Cart.API.Models.Cart?> GetByUserIdAsync(Guid userId)
    {
        _logger.LogInformation("Buscando carrito del usuario {UserId}", userId);
        using var conn = CreateConnection();

        var row = await conn.QueryFirstOrDefaultAsync(
            "SELECT user_id, updated_at FROM carts WHERE user_id = @UserId",
            new { UserId = userId.ToString() });

        if (row is null)
        {
            _logger.LogWarning("Carrito no encontrado para el usuario {UserId}", userId);
            return null;
        }

        var items = await conn.QueryAsync(
            "SELECT product_id, quantity FROM cart_items WHERE cart_user_id = @UserId",
            new { UserId = userId.ToString() });

        return new Cart.API.Models.Cart
        {
            UserId = Guid.Parse((string)row.user_id),
            UpdatedAt = DateTime.Parse((string)row.updated_at),
            Items = items.Select(i => new CartItem
            {
                ProductId = Guid.Parse((string)i.product_id),
                Quantity = (int)i.quantity
            }).ToList()
        };
    }

    public async Task<Cart.API.Models.Cart> UpsertItemAsync(Guid userId, Guid productId, int quantity)
    {
        _logger.LogInformation("Agregando producto {ProductId} al carrito del usuario {UserId}", productId, userId);
        using var conn = CreateConnection();

        await conn.ExecuteAsync("""
            INSERT OR IGNORE INTO carts (user_id, updated_at)
            VALUES (@UserId, datetime('now'));
        """, new { UserId = userId.ToString() });

        await conn.ExecuteAsync("""
            INSERT INTO cart_items (cart_user_id, product_id, quantity)
            VALUES (@UserId, @ProductId, @Quantity)
            ON CONFLICT(cart_user_id, product_id) 
            DO UPDATE SET quantity = quantity + @Quantity;
        """, new { UserId = userId.ToString(), ProductId = productId.ToString(), Quantity = quantity });

        await conn.ExecuteAsync(
            "UPDATE carts SET updated_at = datetime('now') WHERE user_id = @UserId",
            new { UserId = userId.ToString() });

        _logger.LogInformation("Producto {ProductId} agregado correctamente al carrito del usuario {UserId}", productId, userId);
        return (await GetByUserIdAsync(userId))!;
    }

    public async Task<Cart.API.Models.Cart?> UpdateItemQuantityAsync(Guid userId, Guid productId, int quantity)
    {
        _logger.LogInformation("Actualizando cantidad del producto {ProductId} en carrito del usuario {UserId}", productId, userId);
        using var conn = CreateConnection();

        var rows = await conn.ExecuteAsync("""
            UPDATE cart_items SET quantity = @Quantity
            WHERE cart_user_id = @UserId AND product_id = @ProductId
        """, new { UserId = userId.ToString(), ProductId = productId.ToString(), Quantity = quantity });

        if (rows == 0)
        {
            _logger.LogWarning("Producto {ProductId} no encontrado en el carrito del usuario {UserId}", productId, userId);
            return null;
        }

        await conn.ExecuteAsync(
            "UPDATE carts SET updated_at = datetime('now') WHERE user_id = @UserId",
            new { UserId = userId.ToString() });

        _logger.LogInformation("Cantidad actualizada correctamente para producto {ProductId}", productId);
        return await GetByUserIdAsync(userId);
    }

    public async Task<bool> RemoveItemAsync(Guid userId, Guid productId)
    {
        _logger.LogInformation("Quitando producto {ProductId} del carrito del usuario {UserId}", productId, userId);
        using var conn = CreateConnection();

        var rows = await conn.ExecuteAsync("""
            DELETE FROM cart_items 
            WHERE cart_user_id = @UserId AND product_id = @ProductId
        """, new { UserId = userId.ToString(), ProductId = productId.ToString() });

        if (rows > 0)
        {
            await conn.ExecuteAsync(
                "UPDATE carts SET updated_at = datetime('now') WHERE user_id = @UserId",
                new { UserId = userId.ToString() });
            _logger.LogInformation("Producto {ProductId} eliminado del carrito del usuario {UserId}", productId, userId);
        }

        return rows > 0;
    }

    public async Task<bool> ClearCartAsync(Guid userId)
    {
        _logger.LogInformation("Vaciando carrito del usuario {UserId}", userId);
        using var conn = CreateConnection();

        await conn.ExecuteAsync(
            "DELETE FROM cart_items WHERE cart_user_id = @UserId",
            new { UserId = userId.ToString() });

        var rows = await conn.ExecuteAsync(
            "DELETE FROM carts WHERE user_id = @UserId",
            new { UserId = userId.ToString() });

        _logger.LogInformation("Carrito del usuario {UserId} vaciado correctamente", userId);
        return rows > 0;
    }

    public async Task<bool> ItemExistsAsync(Guid userId, Guid productId)
    {
        using var conn = CreateConnection();
        var count = await conn.ExecuteScalarAsync<int>("""
            SELECT COUNT(*) FROM cart_items 
            WHERE cart_user_id = @UserId AND product_id = @ProductId
        """, new { UserId = userId.ToString(), ProductId = productId.ToString() });
        return count > 0;
    }
}