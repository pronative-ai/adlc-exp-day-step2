namespace OuterloopLabApi.Models;

public sealed record CurrencyQuote(
    string SourceCurrency,
    string TargetCurrency,
    decimal Rate,
    string? ProviderDate,
    string? ProviderSequenceMarker,
    string ProviderBaseUrl);
