using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Users.API.Exceptions
{
    public class NotFoundException : Exception
    {
        public string ErrorCode { get; }
        public NotFoundException(string errorCode, string message) : base(message)
        {
            ErrorCode = errorCode;
        }
    }
}

