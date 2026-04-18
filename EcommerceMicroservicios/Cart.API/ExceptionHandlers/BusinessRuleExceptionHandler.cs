using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Cart.API.Exceptions;

namespace Cart.API.ExceptionHandlers
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

            context.Response.StatusCode = StatusCodes.Status422UnprocessableEntity;

            var problem = new ProblemDetails
            {
                Type = "https://tools.ietf.org/html/rfc4918#section-11.2",
                Title = "Unprocessable Entity",
                Status = StatusCodes.Status422UnprocessableEntity,
                Detail = ex.Message,
                Instance = context.Request.Path
            };

            problem.Extensions["errorCode"] = ex.ErrorCode;
            problem.Extensions["errorMessage"] = ex.Message;

            await context.Response.WriteAsJsonAsync(problem, cancellationToken);

            return true;
        }
    }
}
