namespace Orders.API.Exceptions;

public class ConflictException(string errorCode, string message) : Exception(message)
{
    public string ErrorCode { get; } = errorCode;
}