using Azure;
using Azure.Core;
using Azure.ResourceManager;
using Azure.ResourceManager.CosmosDB;
using Azure.ResourceManager.CosmosDB.Models;
using Azure.ResourceManager.Resources;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Options;
using OuterloopLabApi.Configuration;

namespace OuterloopLabApi.Services;

public interface ICosmosStartupInitializer
{
    Task InitializeAsync(CancellationToken cancellationToken);
}

public sealed class CosmosStartupInitializer : ICosmosStartupInitializer
{
    private readonly CosmosClient _cosmosClient;
    private readonly TokenCredential _tokenCredential;
    private readonly CosmosOptions _cosmosOptions;
    private readonly ILogger<CosmosStartupInitializer> _logger;

    public CosmosStartupInitializer(
        CosmosClient cosmosClient,
        TokenCredential tokenCredential,
        IOptions<CosmosOptions> cosmosOptions,
        ILogger<CosmosStartupInitializer> logger)
    {
        _cosmosClient = cosmosClient;
        _tokenCredential = tokenCredential;
        _cosmosOptions = cosmosOptions.Value;
        _logger = logger;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        await TryProvisionWithArmAsync(cancellationToken);
        await EnsureDataPlaneResourcesAsync(cancellationToken);
    }

    private async Task TryProvisionWithArmAsync(CancellationToken cancellationToken)
    {
        try
        {
            ArmClient armClient = new(_tokenCredential);
            SubscriptionResource subscription = await armClient.GetDefaultSubscriptionAsync(cancellationToken);
            ResourceGroupResource resourceGroup = (await subscription.GetResourceGroups().GetAsync(_cosmosOptions.ResourceGroup, cancellationToken)).Value;
            CosmosDBAccountResource account = (await resourceGroup.GetCosmosDBAccounts().GetAsync(_cosmosOptions.AccountName, cancellationToken)).Value;

            CosmosDBSqlDatabaseCreateOrUpdateContent databaseContent = new(
                new AzureLocation(_cosmosOptions.Region),
                new CosmosDBSqlDatabaseResourceInfo(_cosmosOptions.DatabaseName));

            ArmOperation<CosmosDBSqlDatabaseResource> databaseOperation = await account
                .GetCosmosDBSqlDatabases()
                .CreateOrUpdateAsync(WaitUntil.Completed, _cosmosOptions.DatabaseName, databaseContent, cancellationToken);

            CosmosDBSqlDatabaseResource database = databaseOperation.Value;
            CosmosDBContainerPartitionKey partitionKey = new();
            partitionKey.Paths.Add("/id");

            CosmosDBSqlContainerResourceInfo containerResource = new(_cosmosOptions.ContainerName)
            {
                PartitionKey = partitionKey,
            };

            CosmosDBSqlContainerCreateOrUpdateContent containerContent = new(
                new AzureLocation(_cosmosOptions.Region),
                containerResource);

            await database
                .GetCosmosDBSqlContainers()
                .CreateOrUpdateAsync(WaitUntil.Completed, _cosmosOptions.ContainerName, containerContent, cancellationToken);
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "Best-effort ARM provisioning for Cosmos DB database '{DatabaseName}' and container '{ContainerName}' could not be completed.", _cosmosOptions.DatabaseName, _cosmosOptions.ContainerName);
        }
    }

    private async Task EnsureDataPlaneResourcesAsync(CancellationToken cancellationToken)
    {
        DatabaseResponse databaseResponse = await _cosmosClient.CreateDatabaseIfNotExistsAsync(_cosmosOptions.DatabaseName, cancellationToken: cancellationToken);

        ContainerProperties containerProperties = new(_cosmosOptions.ContainerName, "/id");
        await databaseResponse.Database.CreateContainerIfNotExistsAsync(containerProperties, cancellationToken: cancellationToken);

        _logger.LogInformation("Cosmos DB data-plane database '{DatabaseName}' and container '{ContainerName}' are available.", _cosmosOptions.DatabaseName, _cosmosOptions.ContainerName);
    }
}
