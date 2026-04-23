using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Users.API.Exceptions;

namespace Users.API.ExceptionHandlers
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

            var statusCode = ex.ErrorCode switch
            {
                "USR-001" => StatusCodes.Status409Conflict,
                "USR-003" => StatusCodes.Status401Unauthorized,
                "USR-004" => StatusCodes.Status403Forbidden,
                "USR-005" => StatusCodes.Status403Forbidden,
                _ => StatusCodes.Status422UnprocessableEntity
            };

            context.Response.StatusCode = statusCode;

            await context.Response.WriteAsJsonAsync(new ProblemDetails
            {
                Type = $"https://httpstatuses.com/{statusCode}",
                Title = statusCode switch
                {
                    400 => "Bad Request",
                    401 => "Unauthorized",
                    403 => "Forbidden",
                    409 => "Conflict",
                    422 => "Unprocessable Entity",
                    _ => "Error"
                },
                Status = statusCode,
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
