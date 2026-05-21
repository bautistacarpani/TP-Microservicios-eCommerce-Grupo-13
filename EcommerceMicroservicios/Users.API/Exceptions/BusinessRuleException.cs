using System;

namespace Users.API.Exceptions
{
    public class BusinessRuleException : Exception
    {
        public string ErrorCode { get; }

        // Usamos el constructor tradicional para asegurar que el valor se setee sí o sí
        public BusinessRuleException(string errorCode, string message) : base(message)
        {
            ErrorCode = errorCode;
        }
    }
}
