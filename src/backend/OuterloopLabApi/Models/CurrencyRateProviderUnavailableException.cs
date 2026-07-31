namespace OuterloopLabApi.Models;

public sealed class CurrencyRateProviderUnavailableException : Exception
{
  public CurrencyRateProviderUnavailableException(string message, Exception? inner = null) : base(message, inner)
  {
  }
}
