using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Notifications.API.Exceptions;

namespace Notifications.API.Handler
{
    public class BusinessRuleExceptionHandler : IExceptionHandler
    {
        public async ValueTask<bool> TryHandleAsync(
            HttpContext context,
            Exception exception,
            CancellationToken cancellationToken)
        {
            if (exception is not BusinessRuleException ex)
                return false;

            // Ajustado a 400 según el catálogo del TP para NTF-002
            context.Response.StatusCode = StatusCodes.Status400BadRequest;

            await context.Response.WriteAsJsonAsync(new ProblemDetails
            {
                Type = "https://httpstatuses.com/400",
                Title = "Bad Request",
                Status = 400,
                Detail = ex.Message,
                Instance = context.Request.Path,
                Extensions =
                {
                    ["errorCode"] = ex.ErrorCode,
                    ["errorMessage"] = ex.Message
                }
            }, cancellationToken);

            return true;
        }
    }
}

