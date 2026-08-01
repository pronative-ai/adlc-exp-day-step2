namespace OuterloopLabApi.Configuration;

public sealed class CosmosDbOptions
{
    public string Uri { get; init; } = string.Empty;

    public string Database { get; init; } = "currency-conversion-db";

    public string Container { get; init; } = "currencyconversion";

    public string AccountName { get; init; } = string.Empty;

    public string ResourceGroup { get; init; } = string.Empty;

    public string Region { get; init; } = "Central India";

    public string ManagedIdentityClientId { get; init; } = string.Empty;
}
