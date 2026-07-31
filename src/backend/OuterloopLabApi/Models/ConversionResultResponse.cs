namespace OuterloopLabApi.Models;

public sealed record ConversionResultResponse(
  string AuditId,
  decimal Amount,
  string FromCurrency,
  string ToCurrency,
  decimal ExchangeRate,
  decimal ConvertedAmount,
  DateTime ExecutionTimestampUtc,
  string? ProviderDateMarker,
  string? ProviderSequenceMarker);
