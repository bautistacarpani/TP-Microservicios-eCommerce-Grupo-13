using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Data.Sqlite;


namespace Users.API.Data
{
    public class DatabaseInitializer
    {
        private readonly IConfiguration _config;
        private readonly ILogger<DatabaseInitializer> _logger;

        public DatabaseInitializer(IConfiguration config, ILogger<DatabaseInitializer> logger)
        {
            _config = config;
            _logger = logger;
        }

        public void Initialize()
        {
            var connectionString = _config.GetConnectionString("DefaultConnection")
                                   ?? "Data Source=users.db";

            using var connection = new SqliteConnection(connectionString);
            connection.Open();

            // Tabla adaptada a tu modelo de Users
            connection.Execute("""
                CREATE TABLE IF NOT EXISTS Users (
                    Id                 TEXT PRIMARY KEY,
                    Nombre             TEXT NOT NULL,
                    Apellido           TEXT NOT NULL,
                    Email              TEXT NOT NULL UNIQUE,
                    PasswordHash       TEXT NOT NULL,
                    FechaRegistro      TEXT NOT NULL,
                    Activo             INTEGER NOT NULL DEFAULT 1,
                    IntentosFallidos   INTEGER NOT NULL DEFAULT 0,
                    BloqueadoPorFraude INTEGER NOT NULL DEFAULT 0
                );
            """);

            _logger.LogInformation("SQLite inicializado correctamente → {db}", connectionString);
        }
    }

}
