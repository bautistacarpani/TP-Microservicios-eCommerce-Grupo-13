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

        sql += " ORDER BY created_at DESC";
        var rows = await conn.QueryAsync(sql, parameters);
        var result = rows.Select(r => new Product
        {
            Id = Guid.Parse((string)r.id),
            Name = (string)r.name,
            Description = r.description != null ? (string)r.description : null,
            Price = (double)r.price,
            Stock = (long)r.stock,
            Category = (string)r.category,
            CreatedAt = (string)r.CreatedAt,
            UpdatedAt = r.UpdatedAt != null ? (string)r.UpdatedAt : null
        }).ToList();

        _logger.LogInformation("Se encontraron {Count} productos", result.Count);
        return result;
    }

    public async Task<Product?> GetByIdAsync(Guid id)
    {
        _logger.LogInformation("Buscando producto con ID {Id}", id);
        using var conn = CreateConnection();
        var row = await conn.QueryFirstOrDefaultAsync(
            "SELECT id, name, description, price, stock, category, created_at AS CreatedAt, updated_at AS UpdatedAt FROM products WHERE id = @Id",
            new { Id = id.ToString() });

        if (row is null)
        {
            _logger.LogWarning("Producto con ID {Id} no encontrado", id);
            return null;
        }

        return new Product
        {
            Id = Guid.Parse((string)row.id),
            Name = (string)row.name,
            Description = row.description != null ? (string)row.description : null,
            Price = (double)row.price,
            Stock = (long)row.stock,
            Category = (string)row.category,
            CreatedAt = (string)row.CreatedAt,
            UpdatedAt = row.UpdatedAt != null ? (string)row.UpdatedAt : null
        };
    }

    public async Task<Product> CreateAsync(CreateProductRequest req)
    {
        _logger.LogInformation("Creando producto {Name} en categoría {Category}", req.Name, req.Category);
        using var conn = CreateConnection();
        var id = Guid.NewGuid();
        await conn.ExecuteAsync("""
            INSERT INTO products (id, name, description, price, stock, category)
            VALUES (@Id, @Name, @Description, @Price, @Stock, @Category);
        """, new { Id = id.ToString(), req.Name, req.Description, req.Price, req.Stock, req.Category });

        _logger.LogInformation("Producto creado con ID {Id}", id);
        return (await GetByIdAsync(id))!;
    }

    public async Task<Product?> UpdateAsync(Guid id, UpdateProductRequest req)
    {
        _logger.LogInformation("Actualizando producto con ID {Id}", id);
        using var conn = CreateConnection();
        var rows = await conn.ExecuteAsync("""
            UPDATE products 
            SET name = @Name, description = @Description, price = @Price, 
                stock = @Stock, category = @Category, updated_at = datetime('now')
            WHERE id = @Id
        """, new { req.Name, req.Description, req.Price, req.Stock, req.Category, Id = id.ToString() });

        if (rows == 0)
        {
            _logger.LogWarning("No se encontró el producto con ID {Id} para actualizar", id);
            return null;
        }

        _logger.LogInformation("Producto con ID {Id} actualizado correctamente", id);
        return await GetByIdAsync(id);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        _logger.LogInformation("Eliminando producto con ID {Id}", id);
        using var conn = CreateConnection();
        var rows = await conn.ExecuteAsync(
            "DELETE FROM products WHERE id = @Id", new { Id = id.ToString() });

        if (rows > 0)
            _logger.LogInformation("Producto con ID {Id} eliminado correctamente", id);
        else
            _logger.LogWarning("No se encontró el producto con ID {Id} para eliminar", id);

        return rows > 0;
    }

    public async Task<bool> ExistsAsync(string name, string category, Guid? excludeId = null)
    {
        using var conn = CreateConnection();
        var sql = "SELECT COUNT(*) FROM products WHERE LOWER(name) = LOWER(@Name) AND LOWER(category) = LOWER(@Category)";
        if (excludeId.HasValue) sql += " AND id != @ExcludeId";
        var count = await conn.ExecuteScalarAsync<int>(sql, new { Name = name, Category = category, ExcludeId = excludeId?.ToString() });
        return count > 0;
    }
}