using Dapper; // Requerido para el ExecuteScalarAsync
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using System;
using System.Threading;
using System.Threading.Tasks;


namespace Users.API.Extensions
{
    public class SqliteHealthCheck : IHealthCheck
    {
        private readonly IConfiguration _config;
        public SqliteHealthCheck(IConfiguration config) => _config = config;

        public async Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var connectionString = _config.GetConnectionString("DefaultConnection")
                    ?? "Data Source=app.db";
                using var conn = new SqliteConnection(connectionString);
                await conn.OpenAsync(cancellationToken);

                // Dapper ejecuta un SELECT 1 para ver si la base responde
                await conn.ExecuteScalarAsync<int>("SELECT 1");

                return HealthCheckResult.Healthy("SELECT 1 ejecutado OK");
            }
            catch (Exception ex)
            {
                return HealthCheckResult.Unhealthy(
                    description: "No se pudo conectar a SQLite",
                    exception: ex);
            }
        }

    }

}
