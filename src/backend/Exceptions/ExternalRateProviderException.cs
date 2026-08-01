namespace OuterloopLabApi.Exceptions;

public sealed class ExternalRateProviderException : Exception
{
    public ExternalRateProviderException(string message)
        : base(message)
    {
    }

    public ExternalRateProviderException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
