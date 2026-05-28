using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IdentityModel.Client;
using Microsoft.AspNetCore.Http;
using Serilog.Context;
using Notifications.API.Extensions;

namespace Notifications.API
{
    public class CorrelationIdMiddleware
    {
        private readonly RequestDelegate _next;
        private const string CorrelationIdHeaderName = "X-Correlation-Id";

        public CorrelationIdMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // 1. Buscamos el ID en los headers (por si viene de Users.API)
            if (!context.Request.Headers.TryGetValue(CorrelationIdHeaderName, out var correlationId))
            {
                // Si no viene (ej: lo llamamos directo por Swagger), generamos uno nuevo
                correlationId = Guid.NewGuid().ToString();
                context.Request.Headers.Add(CorrelationIdHeaderName, correlationId);
            }

            // 2. 🔥 MAGIA: Metemos el ID en el contexto global de Serilog
            using (LogContext.PushProperty("CorrelationId", correlationId.ToString()))
            {
                // 3. Dejamos que el request siga su camino hacia el endpoint
                await _next(context);
            }
        }


    }
}
