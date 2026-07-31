namespace OuterloopLabApi.Models;

public sealed class CurrencyRateParseException : Exception
{
  public CurrencyRateParseException(string message, Exception? inner = null) : base(message, inner)
  {
  }
}
