namespace OuterloopLabApi.Contracts;

public sealed class CreateCurrencyConversionRequest
{
    public decimal Amount { get; init; }

    public string SourceCurrency { get; init; } = string.Empty;

    public string TargetCurrency { get; init; } = string.Empty;
}
