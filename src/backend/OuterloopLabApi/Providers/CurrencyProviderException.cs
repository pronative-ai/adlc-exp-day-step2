namespace OuterloopLabApi.Providers;

public sealed class CurrencyProviderException : Exception
{
    public CurrencyProviderException(string message, Exception? innerException = null) : base(message, innerException)
    {
    }
}
