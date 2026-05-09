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

        public async Task<IEnumerable<Notification>> GetByUserIdAsync(Guid userId)
        {
            using var conn = CreateConnection();
            const string sql = "SELECT * FROM Notifications WHERE UsuarioId = @userId";

            return await conn.QueryAsync<Notification>(sql, new { userId });
        }
    }
}


