using Notifications.API.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Notifications.API.ExceptionHandler
{
   
    public class BusinessRuleExceptionHandler : BaseExceptionHandler
    {
        protected override bool CanHandle(Exception exception)
            => exception is BusinessRuleException;

        protected override int StatusCode => StatusCodes.Status409Conflict;

        protected override string GetErrorCode(Exception exception)
            => ((BusinessRuleException)exception).ErrorCode;
    }
}
