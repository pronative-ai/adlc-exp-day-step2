namespace OuterloopLabApi.Exceptions;

public sealed class InvalidConversionException : Exception
{
    public InvalidConversionException(string message)
        : base(message)
    {
    }
}
