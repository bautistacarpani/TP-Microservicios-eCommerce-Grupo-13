using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Serilog;
using Serilog.Events;
using Serilog.Filters;
using Serilog.Core;
using Microsoft.AspNetCore.Builder;

namespace Users.API.Extensions
{
    public static class LogginExtensions
    {
        public static void AddAppLogging(this WebApplicationBuilder builder)
        {   
            Log.Logger = new LoggerConfiguration()
        // Nivel mínimo global
        .MinimumLevel.Information()

         // Reduce ruido de logs internos de Microsoft
         .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)

        // Permite logs HTTP del hosting
        .MinimumLevel.Override(
        "Microsoft.AspNetCore.Hosting.Diagnostics",
        LogEventLevel.Information)

        // Agrega contexto automático a los logs
        .Enrich.FromLogContext()

        // =========================
        // CONSOLA → solo errores
        // =========================
        .WriteTo.Logger(lc => lc

        .Filter.ByIncludingOnly(le =>
        le.Level >= LogEventLevel.Error)

        .WriteTo.Console(
        outputTemplate:
        "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
        )

        // =========================================
        // ARCHIVO → solo requests HTTP auditables
        // =========================================
        .WriteTo.Logger(lc => lc

        .Filter.ByIncludingOnly(le =>
        {
            // Solo eventos generados por el middleware HTTP de Serilog
            var esSerilogMiddleware =
            Matching.FromSource(
            "Serilog.AspNetCore.RequestLoggingMiddleware")(le);

            if (!esSerilogMiddleware)
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
            path: "Logs/audit.log",

            // Formato del log
            outputTemplate:
            "{Timestamp:yyyy-MM-dd HH:mm:ss} | {RequestMethod} | {RequestPath} | {StatusCode}{NewLine}",

            // Rotación diaria
            rollingInterval: RollingInterval.Day
        )
        )

            .CreateLogger();


            // Reemplaza el logging por defecto con Serilog
            builder.Host.UseSerilog();
        }
    }
}
