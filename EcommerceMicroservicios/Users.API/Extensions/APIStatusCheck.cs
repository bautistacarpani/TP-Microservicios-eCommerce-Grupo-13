using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;


namespace Users.API.Extensions
{
    public class APIStatusCheck : IHealthCheck
    {
   
        // Guardamos el momento exacto en el que la API se encendió
        private static readonly DateTime StartTime = DateTime.UtcNow;

        public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            var uptime = DateTime.UtcNow - StartTime;

            // Armamos el diccionario con la información que pide la cátedra
            var data = new Dictionary<string, object>
        {
            { "Uptime", $"{uptime.Days}d {uptime.Hours}h {uptime.Minutes}m {uptime.Seconds}s" },
            { "DotNetVersion", RuntimeInformation.FrameworkDescription },
            { "StartTimeUtc", StartTime.ToString("o") },
            { "TimestampUtc", DateTime.UtcNow.ToString("o") }
        };

            // Retornamos Healthy incluyendo los datos de diagnóstico
            return Task.FromResult(HealthCheckResult.Healthy("API operativa y respondiendo correctamente.", data));
        }
    }


}
