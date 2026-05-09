using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Serilog.Filters;

namespace Notifications.API.Extensions
{
    public static class LoggingExtensions
    {
        /// <summary>
        /// Configuración centralizada de Serilog.
        /// Define:
        /// - niveles mínimos
        /// - filtros
        /// - salida a consola
        /// - archivo de auditoría HTTP
        /// </summary>
        public static void AddAppLogging(this WebApplicationBuilder builder)
        {
            Log.Logger = new LoggerConfiguration()

                // =========================
                // Nivel mínimo global
                // =========================
                .MinimumLevel.Information()

                // Reduce ruido de logs internos de Microsoft
                .MinimumLevel.Override(
                    "Microsoft",
                    LogEventLevel.Warning)

                // Permite logs HTTP del hosting
                .MinimumLevel.Override(
                    "Microsoft.AspNetCore.Hosting.Diagnostics",
                    LogEventLevel.Information)

                // Agrega contexto automático
                .Enrich.FromLogContext()

                // =====================================
                // CONSOLA → solo errores o superiores
                // =====================================
                .WriteTo.Logger(lc => lc

                    .Filter.ByIncludingOnly(le =>
                        le.Level >= LogEventLevel.Error)

                    .WriteTo.Console(
                        outputTemplate:
                        "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
                )

                // ============================================
                // ARCHIVO → requests HTTP auditables
                // ============================================
                .WriteTo.Logger(lc => lc

                    .Filter.ByIncludingOnly(le =>
                    {
                        // Solo eventos generados por el middleware HTTP
                        var esMiddlewareHttp =
                            Matching.FromSource(
                                "Serilog.AspNetCore.RequestLoggingMiddleware")(le);

                        if (!esMiddlewareHttp)
                            return false;

                        // Excluir /health y /swagger
                        if (le.Properties.TryGetValue("RequestPath", out var p) &&
                            p is ScalarValue s &&
                            s.Value is string path)
                        {
                            return !path.Contains("/health") &&
                                   !path.Contains("/swagger");
                        }

                        return true;
                    })

                    .WriteTo.File(

                        // Archivo de auditoría
                        path: "logs/audit.log",

                        // Formato de salida
                        outputTemplate:
                        "{Timestamp:yyyy-MM-dd HH:mm:ss} | {RequestMethod} | {RequestPath} | {StatusCode}{NewLine}",

                        // Rotación diaria
                        rollingInterval: RollingInterval.Day
                    )
                )

                .CreateLogger();

            // Reemplaza el logging por defecto de ASP.NET
            builder.Host.UseSerilog();
        }
    }
}
