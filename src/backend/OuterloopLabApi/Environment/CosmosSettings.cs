using Microsoft.Extensions.Configuration;

namespace OuterloopLabApi.Cosmos;

public sealed class CosmosSettings
{
    public CosmosSettings(
        string cosmosDbUri,
        string databaseId,
        string containerId,
        string cosmosAccountName,
        string cosmosResourceGroup,
        string cosmosRegion,
        string azureManagedIdentityClientId)
    {
        CosmosDbUri = cosmosDbUri;
        DatabaseId = databaseId;
        ContainerId = containerId;
        CosmosAccountName = cosmosAccountName;
        CosmosResourceGroup = cosmosResourceGroup;
        CosmosRegion = cosmosRegion;
        AzureManagedIdentityClientId = azureManagedIdentityClientId;
    }

    public string CosmosDbUri { get; }
    public string DatabaseId { get; }
    public string ContainerId { get; }

    public string CosmosAccountName { get; }
    public string CosmosResourceGroup { get; }
    public string CosmosRegion { get; }

    public string AzureManagedIdentityClientId { get; }

    public static CosmosSettings FromEnvironment(IConfiguration config)
    {
        string Require(string key)
            => string.IsNullOrWhiteSpace(config[key])
                ? throw new InvalidOperationException($"Missing required environment variable '{key}'.")
                : config[key]!;

        return new CosmosSettings(
            cosmosDbUri: Require("COSMOS_DB_URI"),
            databaseId: Require("COSMOS_DB_DATABASE"),
            containerId: Require("COSMOS_DB_CONTAINER"),
            cosmosAccountName: Require("COSMOS_DB_ACCOUNT_NAME"),
            cosmosResourceGroup: Require("COSMOS_DB_RESOURCE_GROUP"),
            cosmosRegion: Require("COSMOS_DB_REGION"),
            azureManagedIdentityClientId: Require("AZURE_MANAGED_IDENTITY_CLIENT_ID"));
    }
}
