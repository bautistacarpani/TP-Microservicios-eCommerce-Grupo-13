using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Runtime.InteropServices;

namespace Cart.API;

public class ApiStatusCheck : IHealthCheck
{
    private static readonly DateTime StartTime = DateTime.UtcNow;

    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var uptime = DateTime.UtcNow - StartTime;

        var data = new Dictionary<string, object>
        {
            { "Uptime", $"{uptime.Days}d {uptime.Hours}h {uptime.Minutes}m {uptime.Seconds}s" },
            { "DotNetVersion", RuntimeInformation.FrameworkDescription },
            { "StartTimeUtc", StartTime.ToString("o") },
            { "TimestampUtc", DateTime.UtcNow.ToString("o") }
        };

        return Task.FromResult(HealthCheckResult.Healthy("API operativa y respondiendo correctamente.", data));
    }
}