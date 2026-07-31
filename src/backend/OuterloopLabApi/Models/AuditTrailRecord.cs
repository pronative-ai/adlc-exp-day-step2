namespace OuterloopLabApi.Models;

public sealed class AuditTrailRecord
{
  // Cosmos item id + partition key.
  public string AuditId { get; set; } = default!;
  public decimal Amount { get; set; }
  public string FromCurrency { get; set; } = default!;
  public string ToCurrency { get; set; } = default!;
  public decimal ExchangeRate { get; set; }
  public decimal ConvertedAmount { get; set; }
  public DateTime ExecutionTimestampUtc { get; set; }
  public string ProviderBaseUrl { get; set; } = default!;
  public string? ProviderDateMarker { get; set; }
  public string? ProviderSequenceMarker { get; set; }
}
