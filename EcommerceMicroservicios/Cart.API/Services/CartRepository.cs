using Dapper;
using Microsoft.Data.Sqlite;
using Cart.API.Models;

namespace Cart.API.Services;
// ══════════════════════════════════════════════════════════════════════
// CART REPOSITORY
// Capa de acceso a datos para el carrito de compras.
// Usa Dapper como micro-ORM y SQLite como base de datos.
// El carrito se divide en dos tablas:
//   - carts: un registro por usuario (user_id como PK)
//   - cart_items: los productos del carrito (FK a carts)
// Los IDs (UserId y ProductId) son Guid guardados como TEXT en SQLite.
// ══════════════════════════════════════════════════════════════════════

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

    // Crea y devuelve una nueva conexión a SQLite
    private SqliteConnection CreateConnection() => new(_connectionString);

    // ──────────────────────────────────────────────────────────────
    // CONSULTAS
    // ──────────────────────────────────────────────────────────────

    /// <summary>Obtiene el carrito completo de un usuario incluyendo sus items.</summary>
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

      /// <summary>Verifica si un producto específico está en el carrito del usuario.</summary>
    public async Task<bool> ItemExistsAsync(Guid userId, Guid productId)
    {
        using var conn = CreateConnection();
        var count = await conn.ExecuteScalarAsync<int>("""
            SELECT COUNT(*) FROM cart_items 
            WHERE cart_user_id = @UserId AND product_id = @ProductId
        """, new { UserId = userId.ToString(), ProductId = productId.ToString() });
        return count > 0;
    }

    // ──────────────────────────────────────────────────────────────
    // COMANDOS
    // ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Agrega un producto al carrito o suma la cantidad si ya existe.
    /// Crea el carrito automáticamente si el usuario no tenía uno.
    /// </summary>

    public async Task<Cart.API.Models.Cart> UpsertItemAsync(Guid userId, Guid productId, int quantity)
    {
        _logger.LogInformation("Agregando producto {ProductId} al carrito del usuario {UserId}", productId, userId);
        using var conn = CreateConnection();

         // Crear carrito si no existe (INSERT OR IGNORE no falla si ya existe)
        await conn.ExecuteAsync("""
            INSERT OR IGNORE INTO carts (user_id, updated_at)
            VALUES (@UserId, datetime('now'));
        """, new { UserId = userId.ToString() });

        // Agregar el item o sumar cantidad si ya estaba en el carrito
        await conn.ExecuteAsync("""
            INSERT INTO cart_items (cart_user_id, product_id, quantity)
            VALUES (@UserId, @ProductId, @Quantity)
            ON CONFLICT(cart_user_id, product_id) 
            DO UPDATE SET quantity = quantity + @Quantity;
        """, new { UserId = userId.ToString(), ProductId = productId.ToString(), Quantity = quantity });

        // Actualizar fecha de última modificación
        await conn.ExecuteAsync(
            "UPDATE carts SET updated_at = datetime('now') WHERE user_id = @UserId",
            new { UserId = userId.ToString() });

        _logger.LogInformation("Producto {ProductId} agregado correctamente al carrito del usuario {UserId}", productId, userId);
        return (await GetByUserIdAsync(userId))!;
    }

     /// <summary>Actualiza la cantidad de un producto ya existente en el carrito.</summary>
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

    /// <summary>Quita un producto específico del carrito.</summary>
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

     /// <summary>Vacía el carrito completo del usuario eliminando todos sus items.</summary>seguimo
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
   
}