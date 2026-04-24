using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Notifications.API.ExceptionHandler
{
    public abstract class BaseExceptionHandler : IExceptionHandler
    {
        protected abstract bool CanHandle(Exception exception);
        protected abstract int StatusCode { get; }
        protected abstract string GetErrorCode(Exception exception);

        public async ValueTask<bool> TryHandleAsync(
            HttpContext context,
            Exception exception,
            CancellationToken cancellationToken)
        {
            if (!CanHandle(exception))
                return false;

            context.Response.StatusCode = StatusCode;

            await context.Response.WriteAsJsonAsync(new ProblemDetails
            {
                Type = $"https://httpstatuses.com/{StatusCode}",
                Title = GetTitle(StatusCode),
                Status = StatusCode,
                Detail = exception.Message,
                Instance = context.Request.Path,
                Extensions =
            {
                ["errorCode"] = GetErrorCode(exception),
                ["errorMessage"] = exception.Message
            }
            }, cancellationToken);

            return true;
        }

        private static string GetTitle(int statusCode) => statusCode switch
        {
            404 => "Not Found",
            409 => "Conflict",
            422 => "Unprocessable Entity",
            _ => "Error"
        };
    }
}

