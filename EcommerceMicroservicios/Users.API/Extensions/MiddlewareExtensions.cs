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
}