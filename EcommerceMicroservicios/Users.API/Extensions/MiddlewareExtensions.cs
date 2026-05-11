using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Serilog;
using Serilog.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Users.API.Extensions
{
    public static class MiddlewareExtensions
    {
        public static void UseAppRequestLogging(this WebApplication app)
        {
            app.UseSerilogRequestLogging(options =>
            {
                options.GetLevel = (httpContext, elapsed, exception) =>
                {
                    // Si ocurrió una excepción → Error
                    if (exception != null) return LogEventLevel.Error;
                    var path = httpContext.Request.Path.Value ?? "";
                    if (path.Contains("/health") || path.Contains("/swagger")) return LogEventLevel.Verbose;
                    return LogEventLevel.Information;

                };

                options.MessageTemplate = "HTTP {RequestMethod} {RequestPath} respondió {StatusCode} en {Elapsed:0.0000} ms";
            
            });

            // EXPLICACIÓN: Exponemos el estado de salud en la ruta /health.
            // Usamos el Formateador de la cátedra para que sea compatible con Dashboards.
            app.MapHealthChecks("/health", new HealthCheckOptions
            {
                ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
            });


        }

    }
}