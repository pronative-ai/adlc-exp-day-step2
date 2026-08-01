namespace OuterloopLabApi.Configuration;

public sealed class AppConfig
{
    public required string CosmosDbUri { get; init; }
    public required string CosmosDbDatabase { get; init; }
    public required string CosmosDbContainer { get; init; }
    public required string CosmosDbAccountName { get; init; }
    public required string CosmosDbResourceGroup { get; init; }
    public required string CosmosDbRegion { get; init; }
    public required string AzureManagedIdentityClientId { get; init; }

    public string CurrencyApiBaseUrl { get; init; } = "https://frankfurter.dev";
    public int CosmosThroughput { get; init; } = 400;
}

public static class AppConfigLoader
{
    public static AppConfig LoadFromEnvironment()
    {
        static string Require(string key)
        {
            var v = Environment.GetEnvironmentVariable(key);
            if (string.IsNullOrWhiteSpace(v))
                throw new InvalidOperationException($"Missing required environment variable: {key}");
            return v;
        }

        return new AppConfig
        {
            CosmosDbUri = Require("COSMOS_DB_URI"),
            CosmosDbDatabase = Require("COSMOS_DB_DATABASE"),
            CosmosDbContainer = Require("COSMOS_DB_CONTAINER"),
            CosmosDbAccountName = Require("COSMOS_DB_ACCOUNT_NAME"),
            CosmosDbResourceGroup = Require("COSMOS_DB_RESOURCE_GROUP"),
            CosmosDbRegion = Require("COSMOS_DB_REGION"),
            AzureManagedIdentityClientId = Require("AZURE_MANAGED_IDENTITY_CLIENT_ID"),

            CurrencyApiBaseUrl = Environment.GetEnvironmentVariable("CURRENCY_API_BASE_URL")
                ?? "https://frankfurter.dev",
        };
    }
}
