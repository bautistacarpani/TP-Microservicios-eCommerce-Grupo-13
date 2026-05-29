namespace Orders.API.Middleware;

public class CorrelationIdMiddleware
{
    private const string CorrelationIdHeader = "X-Correlation-Id";
    private readonly RequestDelegate _next;

    public CorrelationIdMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Si nos llega un ID desde otro servicio lo reutilizamos,
        // así mantenemos el mismo ID en toda la cadena de llamadas
        var correlationId = context.Request.Headers[CorrelationIdHeader]
                                .FirstOrDefault()
                            ?? Guid.NewGuid().ToString();

        // Lo guardamos en Items para que los ExceptionHandlers
        // puedan incluirlo en las respuestas de error
        context.Items[CorrelationIdHeader] = correlationId;

        // Lo mandamos en el header de respuesta para que puedan
        // trackearlo desde Swagger o Postman
        context.Response.Headers[CorrelationIdHeader] = correlationId;

        // Lo agregamos al contexto de Serilog para que aparezca
        // automáticamente en todos los logs de esta request
        using (Serilog.Context.LogContext.PushProperty("CorrelationId", correlationId))
        {
            await _next(context);
        }
    }
}