namespace OuterloopLabApi.Dtos;

public sealed class ConversionQuoteRequestDto
{
  public decimal Amount { get; set; }
  public string? SourceCurrency { get; set; }
  public string? TargetCurrency { get; set; }
}
