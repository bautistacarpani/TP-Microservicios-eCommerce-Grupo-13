using Dapper;
using Microsoft.Data.Sqlite;
using Products.API.Models;

namespace Products.API.Services;

public class ProductRepository
{
    private readonly string _connectionString;
    private readonly ILogger<ProductRepository> _logger;

    public ProductRepository(IConfiguration config, ILogger<ProductRepository> logger)
    {
        _connectionString = config.GetConnectionString("DefaultConnection")
            ?? "Data Source=products.db";
        _logger = logger;
    }

    private SqliteConnection CreateConnection() => new(_connectionString);

    public async Task<IEnumerable<Product>> GetAllAsync(string? category = null, string? name = null)
    {
        _logger.LogInformation("Consultando productos. Filtros: categoria={Category}, nombre={Name}", category, name);
        using var conn = CreateConnection();
        var sql = "SELECT id, name, description, price, stock, category, created_at AS CreatedAt, updated_at AS UpdatedAt FROM products WHERE 1=1";
        var parameters = new DynamicParameters();

        if (!string.IsNullOrEmpty(category))
        {
            sql += " AND LOWER(category) = LOWER(@Category)";
            parameters.Add("Category", category);
        }

        if (!string.IsNullOrEmpty(name))
        {
            sql += " AND LOWER(name) LIKE LOWER(@Name)";
            parameters.Add("Name", $"%{name}%");
        }

        sql += " ORDER BY id DESC";
        var result = await conn.QueryAsync<Product>(sql, parameters);
        _logger.LogInformation("Se encontraron {Count} productos", result.Count());
        return result;
    }

    public async Task<Product?> GetByIdAsync(long id)
    {
        _logger.LogInformation("Buscando producto con ID {Id}", id);
        using var conn = CreateConnection();
        var product = await conn.QueryFirstOrDefaultAsync<Product>(
            "SELECT id, name, description, price, stock, category, created_at AS CreatedAt, updated_at AS UpdatedAt FROM products WHERE id = @Id", new { Id = id });

        if (product is null)
            _logger.LogWarning("Producto con ID {Id} no encontrado", id);

        return product;
    }

    public async Task<Product> CreateAsync(CreateProductRequest req)
    {
        _logger.LogInformation("Creando producto {Name} en categoría {Category}", req.Name, req.Category);
        using var conn = CreateConnection();
        var id = await conn.ExecuteScalarAsync<long>("""
            INSERT INTO products (name, description, price, stock, category)
            VALUES (@Name, @Description, @Price, @Stock, @Category);
            SELECT last_insert_rowid();
        """, req);
        _logger.LogInformation("Producto creado con ID {Id}", id);
        return (await GetByIdAsync(id))!;
    }

    public async Task<Product?> UpdateAsync(long id, UpdateProductRequest req)
    {
        _logger.LogInformation("Actualizando producto con ID {Id}", id);
        using var conn = CreateConnection();
        var rows = await conn.ExecuteAsync("""
            UPDATE products 
            SET name = @Name, description = @Description, price = @Price, 
                stock = @Stock, category = @Category, updated_at = datetime('now')
            WHERE id = @Id
        """, new { req.Name, req.Description, req.Price, req.Stock, req.Category, Id = id });

        if (rows == 0)
        {
            _logger.LogWarning("No se encontró el producto con ID {Id} para actualizar", id);
            return null;
        }

        _logger.LogInformation("Producto con ID {Id} actualizado correctamente", id);
        return await GetByIdAsync(id);
    }

    public async Task<bool> DeleteAsync(long id)
    {
        _logger.LogInformation("Eliminando producto con ID {Id}", id);
        using var conn = CreateConnection();
        var rows = await conn.ExecuteAsync(
            "DELETE FROM products WHERE id = @Id", new { Id = id });

        if (rows > 0)
            _logger.LogInformation("Producto con ID {Id} eliminado correctamente", id);
        else
            _logger.LogWarning("No se encontró el producto con ID {Id} para eliminar", id);

        return rows > 0;
    }

    public async Task<bool> ExistsAsync(string name, string category, long? excludeId = null)
    {
        using var conn = CreateConnection();
        var sql = "SELECT COUNT(*) FROM products WHERE LOWER(name) = LOWER(@Name) AND LOWER(category) = LOWER(@Category)";
        if (excludeId.HasValue) sql += " AND id != @ExcludeId";
        var count = await conn.ExecuteScalarAsync<int>(sql, new { Name = name, Category = category, ExcludeId = excludeId });
        return count > 0;
    }
}