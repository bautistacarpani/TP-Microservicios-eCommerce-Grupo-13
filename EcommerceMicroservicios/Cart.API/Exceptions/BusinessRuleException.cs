using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cart.API.Exceptions
{

    public class BusinessRuleException : Exception
    {
        public string ErrorCode { get; }
        public BusinessRuleException(string errorCode, string message) :
        base(message)
        {
            ErrorCode = errorCode;
        }
    }
}

