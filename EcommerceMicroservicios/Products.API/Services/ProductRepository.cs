using Dapper;
using Microsoft.Data.Sqlite;
using Products.API.Models;

namespace Products.API.Services;

public class ProductRepository
{
    private readonly string _connectionString;

    public ProductRepository(IConfiguration config)
    {
        _connectionString = config.GetConnectionString("DefaultConnection")
            ?? "Data Source=products.db";
    }

    private SqliteConnection CreateConnection() => new(_connectionString);

    public async Task<IEnumerable<Product>> GetAllAsync(string? category = null, string? name = null)
    {
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
        return await conn.QueryAsync<Product>(sql, parameters);
    }

    public async Task<Product?> GetByIdAsync(long id)
    {
        using var conn = CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<Product>(
    "SELECT id, name, description, price, stock, category, created_at AS CreatedAt, updated_at AS UpdatedAt FROM products WHERE id = @Id", new { Id = id });
    }

    public async Task<Product> CreateAsync(CreateProductRequest req)
    {
        using var conn = CreateConnection();
        var id = await conn.ExecuteScalarAsync<long>("""
            INSERT INTO products (name, description, price, stock, category)
            VALUES (@Name, @Description, @Price, @Stock, @Category);
            SELECT last_insert_rowid();
        """, req);
        return (await GetByIdAsync(id))!;
    }

    public async Task<Product?> UpdateAsync(long id, UpdateProductRequest req)
    {
        using var conn = CreateConnection();
        var rows = await conn.ExecuteAsync("""
            UPDATE products 
            SET name = @Name, description = @Description, price = @Price, 
                stock = @Stock, category = @Category, updated_at = datetime('now')
            WHERE id = @Id
        """, new { req.Name, req.Description, req.Price, req.Stock, req.Category, Id = id });

        return rows == 0 ? null : await GetByIdAsync(id);
    }

    public async Task<bool> DeleteAsync(long id)
    {
        using var conn = CreateConnection();
        var rows = await conn.ExecuteAsync(
            "DELETE FROM products WHERE id = @Id", new { Id = id });
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