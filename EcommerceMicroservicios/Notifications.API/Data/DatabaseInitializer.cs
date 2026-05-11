using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Data.Sqlite;


namespace Notifications.API.Data
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
                                   ?? "Data Source=notifications.db";

            using var connection = new SqliteConnection(connectionString);
            connection.Open();

            // Tabla adaptada a tu modelo de Notifications
            connection.Execute("""
                CREATE TABLE IF NOT EXISTS Notifications (
                    Id          TEXT PRIMARY KEY,
                    UsuarioId   TEXT NOT NULL,
                    Mensaje     TEXT NOT NULL,
                    Tipo        TEXT NOT NULL,
                    Estado      TEXT NOT NULL,
                    FechaEnvio  TEXT NOT NULL
                );
            """);

            _logger.LogInformation("SQLite inicializado correctamente → {db}", connectionString);
        }
    }

}
