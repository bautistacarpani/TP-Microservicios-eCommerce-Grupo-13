using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cart.API.Exceptions
{

    public class ValidationException : Exception
    {
        public string ErrorCode { get; }
        public ValidationException(string errorCode, string message) :
        base(message)
        {
            ErrorCode = errorCode;
        }
    }
}

