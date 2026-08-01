namespace OuterloopLabApi.Conversion;

public sealed class ConversionRequest
{
    public string SourceCurrency { get; set; } = default!;
    public string TargetCurrency { get; set; } = default!;
    public decimal Amount { get; set; }
}
