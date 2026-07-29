namespace OuterloopLabApi.Dtos;

public sealed class ConversionAuditRecordDto
{
  public string Id { get; set; } = string.Empty;
  public decimal Amount { get; set; }
  public string SourceCurrency { get; set; } = string.Empty;
  public string TargetCurrency { get; set; } = string.Empty;
  public decimal ExchangeRate { get; set; }
  public decimal ConvertedAmount { get; set; }
  public string? ProviderResponseId { get; set; }
  public DateTime QuotedAtUtc { get; set; }
}
