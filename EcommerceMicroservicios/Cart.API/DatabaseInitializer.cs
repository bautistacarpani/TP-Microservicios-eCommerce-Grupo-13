using Dapper;
using Microsoft.Data.Sqlite;

namespace Cart.API;

public class DatabaseInitializer
{
    private readonly string _connectionString;

    public DatabaseInitializer(IConfiguration config)
    {
        _connectionString = config.GetConnectionString("DefaultConnection")
            ?? "Data Source=cart.db";
    }

    public void Initialize()
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        connection.Execute("""
            CREATE TABLE IF NOT EXISTS carts (
                user_id     TEXT PRIMARY KEY,
                updated_at  TEXT NOT NULL DEFAULT (datetime('now'))
            );

            CREATE TABLE IF NOT EXISTS cart_items (
                cart_user_id  TEXT NOT NULL,
                product_id    TEXT NOT NULL,
                quantity      INTEGER NOT NULL DEFAULT 1,
                PRIMARY KEY (cart_user_id, product_id),
                FOREIGN KEY (cart_user_id) REFERENCES carts(user_id)
            );
        """);
    }
}