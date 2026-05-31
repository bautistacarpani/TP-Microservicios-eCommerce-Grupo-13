using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Data.Sqlite;
using System.Data;
using Notifications.API.Models;

namespace Notifications.API.Repositories
{
    public class NotificationsRepository
    {
        private readonly IConfiguration _configuration;

        public NotificationsRepository(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        private IDbConnection CreateConnection()
            => new SqliteConnection(_configuration.GetConnectionString("DefaultConnection"));

        public async Task CreateAsync(Notification notification)
        {
            using var conn = CreateConnection();

            // Usamos comillas triples para que el SQL sea más legible
            const string sql = """
                INSERT INTO Notifications (Id, UsuarioId, Mensaje, Tipo, Estado, FechaEnvio)
                VALUES (@Id, @UsuarioId, @Mensaje, @Tipo, @Estado, @FechaEnvio)
            """;

            await conn.ExecuteAsync(sql, notification);
        }

        public async Task<IEnumerable<Notification>> GetByUserIdAsync(string userId)
        {
            using var conn = CreateConnection();
            const string sql = "SELECT * FROM Notifications WHERE UsuarioId = @userId";

            return await conn.QueryAsync<Notification>(sql, new { userId });
        }

        // 🔥 NUEVO MÉTODO: Para actualizar el estado de lectura en SQLite
        public async Task UpdateStatusAsync(string id, bool leida)
        {
            using var conn = CreateConnection();
            const string sql = "UPDATE Notifications SET Leida = @Leida WHERE Id = @Id";
            // En SQLite, los booleanos pueden manejarse como enteros (1 = true, 0 = false)
            await conn.ExecuteAsync(sql, new { Id = id, Leida = leida ? 1 : 0 });
        }
        // Actualiza el estado de envío de la notificación (Enviada / Fallida)
        public async Task UpdateEstadoAsync(string id, string estado)
        {
            using var conn = CreateConnection();
            const string sql = "UPDATE Notifications SET Estado = @Estado WHERE Id = @Id";
            await conn.ExecuteAsync(sql, new { Id = id, Estado = estado });
        }

    }

}


