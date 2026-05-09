using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Serilog;
using Serilog.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Notifications.API.Extensions
{
    public static class MiddlewareExtensions
    {
        /// <summary>
        /// Middleware centralizado de logging HTTP.
        /// Intercepta todas las requests y asigna
        /// un nivel de severidad al evento de log.
        /// </summary>
        public static void UseAppRequestLogging(this WebApplication app)
        {
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
