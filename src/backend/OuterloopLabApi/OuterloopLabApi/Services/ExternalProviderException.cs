namespace OuterloopLabApi.Services;

public sealed class ExternalProviderException : Exception
{
  public ExternalProviderException(string message) : base(message) {}
}
