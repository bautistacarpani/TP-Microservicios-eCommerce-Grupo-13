using Serilog;
using Serilog.Events;
using Microsoft.AspNetCore.Builder;
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
            /// <summary>
            /// Middleware centralizado de logging HTTP.
            /// Intercepta todas las requests y define el nivel de log
            /// según el tipo de request o si ocurrió una excepción.
            /// </summary> 
            app.UseSerilogRequestLogging(options =>
            {
                options.GetLevel = (httpContext, _, exception) =>

                // Si ocurrió una excepción → Error
                exception != null
                ? LogEventLevel.Error

                // Requests a /health → Verbose
                : httpContext.Request.Path.StartsWithSegments("/health")
                ? LogEventLevel.Verbose

                // Todas las demás → Information
                : LogEventLevel.Information;
            });

        }
    }
}
