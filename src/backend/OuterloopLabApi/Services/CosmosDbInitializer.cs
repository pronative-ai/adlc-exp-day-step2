using Azure;
using Azure.Core;
using Azure.ResourceManager;
using Azure.ResourceManager.CosmosDB;
using Azure.ResourceManager.CosmosDB.Models;
using Microsoft.Azure.Cosmos;
using OuterloopLabApi.Configuration;

namespace OuterloopLabApi.Services;

/// <summary>
/// Ensures the Cosmos DB database and container exist before the web application starts.
/// Control-plane (ARM) provisioning is best-effort; the token-authenticated data-plane
/// create-if-not-exists must succeed or startup fails.
/// </summary>
public static class CosmosDbInitializer
{
    private const string TenantIdPartitionKeyPath = "/tenantId";

    public static async Task EnsureReadyAsync(
        CosmosDbOptions options,
        TokenCredential credential,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        await TryProvisionViaArmAsync(options, credential, logger, cancellationToken);

        var cosmosClient = CreateCosmosClient(options, credential);
        var database = (await cosmosClient.CreateDatabaseIfNotExistsAsync(
            options.Database, (ThroughputProperties?)null, null, cancellationToken)).Database;
        await database.CreateContainerIfNotExistsAsync(
            options.Container, TenantIdPartitionKeyPath, (int?)null, null, cancellationToken);
    }

    private static async Task TryProvisionViaArmAsync(
        CosmosDbOptions options,
        TokenCredential credential,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        try
        {
            var armClient = new ArmClient(credential);
            var subscription = await armClient.GetDefaultSubscriptionAsync(cancellationToken);
            var resourceGroup = (await subscription.GetResourceGroupAsync(options.ResourceGroup, cancellationToken)).Value;
            var account = (await resourceGroup.GetCosmosDBAccountAsync(options.AccountName, cancellationToken)).Value;

            var databases = account.GetCosmosDBSqlDatabases();
            if (!(await databases.ExistsAsync(options.Database, cancellationToken)).Value)
            {
                var databaseContent = new CosmosDBSqlDatabaseCreateOrUpdateContent(
                    new AzureLocation(options.Region),
                    new CosmosDBSqlDatabaseResourceInfo(options.Database))
                {
                    Options = new CosmosDBCreateUpdateConfig()
                };
                await databases.CreateOrUpdateAsync(WaitUntil.Completed, options.Database, databaseContent, cancellationToken);
            }

            var databaseResource = (await databases.GetAsync(options.Database, cancellationToken)).Value;
            var containers = databaseResource.GetCosmosDBSqlContainers();
            if (!(await containers.ExistsAsync(options.Container, cancellationToken)).Value)
            {
                var containerContent = new CosmosDBSqlContainerCreateOrUpdateContent(
                    new AzureLocation(options.Region),
                    new CosmosDBSqlContainerResourceInfo(options.Container)
                    {
                        PartitionKey = new CosmosDBContainerPartitionKey { Paths = { TenantIdPartitionKeyPath } }
                    })
                {
                    Options = new CosmosDBCreateUpdateConfig()
                };
                await containers.CreateOrUpdateAsync(WaitUntil.Completed, options.Container, containerContent, cancellationToken);
            }

            logger.LogInformation(
                "Cosmos DB resources ensured via control plane (database '{Database}', container '{Container}').",
                options.Database, options.Container);
        }
        catch (Exception ex)
        {
            logger.LogWarning(
                "Best-effort control-plane provisioning skipped (database '{Database}', container '{Container}'): {Message}",
                options.Database, options.Container, ex.Message);
        }
    }

    private static CosmosClient CreateCosmosClient(CosmosDbOptions options, TokenCredential credential)
    {
        return new CosmosClient(options.Uri, credential, new CosmosClientOptions
        {
            ConnectionMode = ConnectionMode.Direct,
            SerializerOptions = new CosmosSerializationOptions
            {
                PropertyNamingPolicy = CosmosPropertyNamingPolicy.CamelCase,
                Indented = false,
            },
        });
    }
}
