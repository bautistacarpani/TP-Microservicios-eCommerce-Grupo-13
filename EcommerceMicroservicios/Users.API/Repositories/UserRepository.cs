using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Data.Sqlite;
using System.Data;
using Users.API.Models;

namespace Users.API.Repositories
{
    public class UserRepository(IConfiguration configuration)
    {
        // Método privado para crear la conexión, tal como pide la guía
        private IDbConnection CreateConnection()
            => new SqliteConnection(configuration.GetConnectionString("DefaultConnection"));

        public async Task<User?> GetByEmailAsync(string email)
        {
            using var conn = CreateConnection();
            return await conn.QueryFirstOrDefaultAsync<User>(
                "SELECT * FROM Users WHERE Email = @Email", new { Email = email });
        }

        public async Task CreateAsync(User user)
        {
            using var conn = CreateConnection();
            const string sql = @"
                INSERT INTO Users (Id, Nombre, Apellido, Email, PasswordHash, FechaRegistro, Activo, IntentosFallidos, BloqueadoPorFraude)
                VALUES (@Id, @Nombre, @Apellido, @Email, @PasswordHash, @FechaRegistro, @Activo, @IntentosFallidos, @BloqueadoPorFraude)";

            await conn.ExecuteAsync(sql, user);
        }

        public async Task UpdateLoginAttemptsAsync(string id, int attempts, bool active)
        {
            using var conn = CreateConnection();
            await conn.ExecuteAsync(
                "UPDATE Users SET IntentosFallidos = @attempts, Activo = @active WHERE Id = @Id",
                new { Id = id, attempts, active = active ? 1 : 0 });
        }

        // Añadir este método dentro de la clase UserRepository
        public async Task LockAccountAsync(Guid id)
        {
            using var db = CreateConnection();
            const string sql = "UPDATE Users SET Activo = 0 WHERE Id = @Id";
            await db.ExecuteAsync(sql, new { Id = id.ToString() });
        }

        internal async Task ResetAttemptsAsync(Guid id)
        {
            throw new NotImplementedException();
        }
    }
}
