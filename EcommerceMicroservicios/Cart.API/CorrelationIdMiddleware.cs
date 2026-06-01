namespace Cart.API;
// ══════════════════════════════════════════════════════════════════════
// CORRELATION ID MIDDLEWARE
// Se ejecuta en cada request antes de llegar al endpoint.
// Lee el X-Correlation-Id del header entrante, o genera uno nuevo si no viene.
// Lo propaga en:
//   - context.Items → para usarlo en el código del endpoint
//   - Response headers → para que el cliente lo vea en la respuesta
//   - LogContext → para que aparezca en todos los logs de ese request
// ══════════════════════════════════════════════════════════════════════
public class CorrelationIdMiddleware
{
    private readonly RequestDelegate _next;
    private const string CorrelationIdHeader = "X-Correlation-Id";

    public CorrelationIdMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = context.Request.Headers[CorrelationIdHeader].FirstOrDefault()
            ?? Guid.NewGuid().ToString();

        context.Items[CorrelationIdHeader] = correlationId;
        context.Response.Headers[CorrelationIdHeader] = correlationId;

        using (Serilog.Context.LogContext.PushProperty("CorrelationId", correlationId))
        {
            await _next(context);
        }
    }
}