namespace OuterloopLabApi.External;

public sealed class ProviderConversionResult
{
    public ProviderConversionResult(decimal rate, string? providerBaseCurrency, string? providerDate, string? providerSequence)
    {
        Rate = rate;
        ProviderBaseCurrency = providerBaseCurrency;
        ProviderDate = providerDate;
        ProviderSequence = providerSequence;
    }

    public decimal Rate { get; }
    public string? ProviderBaseCurrency { get; }
    public string? ProviderDate { get; }
    public string? ProviderSequence { get; }
}
