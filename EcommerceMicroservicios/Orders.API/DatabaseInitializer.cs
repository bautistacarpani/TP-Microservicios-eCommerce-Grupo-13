using Dapper;
using Microsoft.Data.Sqlite;

namespace Orders.API;

public class DatabaseInitializer
{
    private readonly string _connectionString;

    public DatabaseInitializer(IConfiguration config)
    {
        _connectionString = config.GetConnectionString("DefaultConnection")
            ?? "Data Source=orders.db";
    }

    public void Initialize()
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        connection.Execute("""
            CREATE TABLE IF NOT EXISTS orders (
                id                  TEXT PRIMARY KEY,
                usuario_id          TEXT NOT NULL,
                total               REAL NOT NULL DEFAULT 0,
                estado              TEXT NOT NULL DEFAULT 'Pendiente',
                fecha_creacion      TEXT NOT NULL DEFAULT (datetime('now')),
                fecha_actualizacion TEXT NULL
            );

            CREATE TABLE IF NOT EXISTS order_items (
                order_id        TEXT NOT NULL,
                producto_id     TEXT NOT NULL,
                cantidad        INTEGER NOT NULL,
                precio_unitario REAL NOT NULL,
                PRIMARY KEY (order_id, producto_id),
                FOREIGN KEY (order_id) REFERENCES orders(id)
            );
        """);
    }
}