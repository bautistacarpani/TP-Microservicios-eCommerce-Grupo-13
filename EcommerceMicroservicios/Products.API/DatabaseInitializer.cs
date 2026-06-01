using Dapper;
using Microsoft.Data.Sqlite;

namespace Products.API;
// ══════════════════════════════════════════════════════════════════════
// DATABASE INITIALIZER
// Se ejecuta una sola vez al arrancar el servicio.
// Crea las tablas en SQLite si no existen (CREATE TABLE IF NOT EXISTS).
// Registrado como Singleton en Program.cs y llamado en el startup.
// ══════════════════════════════════════════════════════════════════════
public class DatabaseInitializer
{
    private readonly string _connectionString;

    public DatabaseInitializer(IConfiguration config)
    {
        _connectionString = config.GetConnectionString("DefaultConnection") 
            ?? "Data Source=products.db";
    }

    public void Initialize()
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        connection.Execute("""
    CREATE TABLE IF NOT EXISTS products (
        id          TEXT    PRIMARY KEY,
        name        TEXT    NOT NULL,
        description TEXT,
        price       REAL    NOT NULL DEFAULT 0,
        stock       INTEGER NOT NULL DEFAULT 0,
        category    TEXT    NOT NULL,
        created_at  TEXT    NOT NULL DEFAULT (datetime('now')),
        updated_at  TEXT
    );
""");
    }
}