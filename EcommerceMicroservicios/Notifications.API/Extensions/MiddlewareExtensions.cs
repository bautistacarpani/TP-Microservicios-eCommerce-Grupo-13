using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Identity;
using Serilog;
using Serilog.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;


namespace Notifications.API.Extensions
{
    public static class MiddlewareExtensions
    {
        public static void UseAppRequestLogging(this WebApplication app)
        {
            app.UseSerilogRequestLogging(options =>
            {
                options.GetLevel = (httpContext, _, exception) =>
                {
                    // 1. Si hubo un error técnico no controlado -> Error
                    if (exception != null) return LogEventLevel.Error;

                    // 2. Filtramos rutas de sistema para evitar "ensuciar" el log de auditoría
                    var path = httpContext.Request.Path.Value ?? "";
                    if (path.Contains("/health") || path.Contains("/swagger"))
                    {
                        return LogEventLevel.Verbose; // Nivel mínimo: no aparece en consola/archivo normal
                    }

                    // 3. Todo lo demás es una operación válida del negocio -> Information
                    return LogEventLevel.Information;
                    options.MessageTemplate = "HTTP {RequestMethod} {RequestPath} respondió {StatusCode} en {Elapsed:0.0000} ms";

                };

                // EXPLICACIÓN: Exponemos el estado de salud en la ruta /health.
                // Usamos el Formateador de la cátedra para que sea compatible con Dashboards.
                app.MapHealthChecks("/health", new HealthCheckOptions
                {
                    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
                });

            });
        }


        public static void UseAppHealthChecks(this WebApplication app)
        {
            // 1. Endpoint JSON con estado detallado (Exige el punto 4.5)
            app.MapHealthChecks("/health", new HealthCheckOptions
            {
                ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
            });

            // 2. Dashboard web visual (Exige el punto 4.5)
            app.MapHealthChecksUI(setup => setup.UIPath = "/health-ui");
        }

    }
}