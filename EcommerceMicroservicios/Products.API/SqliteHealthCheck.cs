using Dapper;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Products.API;
// ══════════════════════════════════════════════════════════════════════
// SQLITE HEALTH CHECK
// Verifica que la base de datos SQLite esté disponible y respondiendo.
// Ejecuta SELECT 1 para confirmar que la conexión es válida.
// Se expone en /health/ready con el tag "database".
// ══════════════════════════════════════════════════════════════════════
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
                ?? "Data Source=products.db";
            using var conn = new SqliteConnection(connectionString);
            await conn.OpenAsync(cancellationToken);
            await conn.ExecuteScalarAsync<int>("SELECT 1");
            return HealthCheckResult.Healthy("Base de datos OK");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy(
                description: "No se pudo conectar a SQLite",
                exception: ex);
        }
    }
}