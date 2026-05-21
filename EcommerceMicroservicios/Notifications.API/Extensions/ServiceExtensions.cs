using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;


namespace Notifications.API.Extensions
{
    public static class ServiceExtensions
    {
        public static IServiceCollection AddAppHealthChecks(this IServiceCollection services, IConfiguration configuration)
        {
            // 1. Registro de los checks que pide el punto 4.3 y 4.4
            services.AddHealthChecks()
                .AddCheck<SqliteHealthCheck>("sqlite-db", tags: new[] { "database" })
                .AddCheck<APIStatusCheck>("api-status", tags: new[] { "api" });

            // 2. Configuración del panel visual (UI) en memoria
            services.AddHealthChecksUI(setup =>
            {
                setup.SetEvaluationTimeInSeconds(600); // Evalúa cada 10 minutos para no saturar
                setup.AddHealthCheckEndpoint("E-Commerce API", "/health");
            }).AddInMemoryStorage();

            return services;
        }

    }
}
