namespace OuterloopLabApi.Exceptions;

public sealed class RateProviderUnavailableException : Exception
{
    public RateProviderUnavailableException(string message, Exception? innerException)
        : base(message, innerException)
    {
    }
}
