namespace OuterloopLabApi.Contracts;

public sealed record CurrencyConversionAuditResponse(
    string AuditId,
    string SourceCurrency,
    string TargetCurrency,
    decimal OriginalAmount,
    decimal Rate,
    decimal ConvertedAmount,
    string? ProviderDate,
    string? ProviderSequenceMarker,
    string ProviderBaseUrl,
    DateTimeOffset ExecutedAtUtc);
