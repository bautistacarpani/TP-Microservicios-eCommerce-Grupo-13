using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Notifications.API.Handler
{
    public class BaseExceptionHandler : IExceptionHandler
    {
        public async ValueTask<bool> TryHandleAsync(
            HttpContext context,
            Exception exception,
            CancellationToken cancellationToken)
        {
            context.Response.StatusCode = 500;

            await context.Response.WriteAsJsonAsync(new ProblemDetails
            {
                Type = "https://httpstatuses.com/500",
                Title = "Internal Server Error",
                Status = 500,
                Detail = "Ocurrió un error interno.",
                Instance = context.Request.Path,
                Extensions =
                {
                    ["errorCode"] = "NTF-500",
                    ["errorMessage"] = "Ocurrió un error interno."
                }
            }, cancellationToken);

            return true;
        }
    }
}