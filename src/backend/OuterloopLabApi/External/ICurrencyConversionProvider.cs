namespace OuterloopLabApi.External;

public interface ICurrencyConversionProvider
{
    Task<ProviderConversionResult> GetLatestRateAsync(string sourceCurrency, string targetCurrency);
}
