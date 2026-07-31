namespace OuterloopLabApi.Models;

public sealed record CurrencyRate(
  decimal Rate,
  string BaseCurrency,
  string TargetCurrency,
  string? ProviderDateMarker,
  string? ProviderSequenceMarker);
