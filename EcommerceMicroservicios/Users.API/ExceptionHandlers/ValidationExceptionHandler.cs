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
    public class ValidationExceptionHandler : IExceptionHandler
    {
        public async ValueTask<bool> TryHandleAsync(
            HttpContext context,
            Exception exception,
            CancellationToken cancellationToken)
        {
            if (exception is not ValidationException ex)
                return false;

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
