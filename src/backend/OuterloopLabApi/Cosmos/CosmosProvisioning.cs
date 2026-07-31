using Azure;
using Azure.Core;
using Azure.Identity;
using Azure.ResourceManager;
using Azure.ResourceManager.CosmosDB;
using Azure.ResourceManager.CosmosDB.Models;
using System.Threading;
using Microsoft.Azure.Cosmos;

namespace OuterloopLabApi.Cosmos;

public static class CosmosProvisioning
{
  public static async Task TryProvisionWithArmBestEffortAsync(CosmosSettings settings, TokenCredential credential)
  {
    try
    {
      // Best-effort only: if ARM cannot be reached (missing subscription id, RBAC, etc.), we continue.
      var subscriptionId = Environment.GetEnvironmentVariable("AZURE_SUBSCRIPTION_ID");
      if (string.IsNullOrWhiteSpace(subscriptionId)) return;

      var armClient = new ArmClient(credential);
      var subscription = armClient.GetSubscriptions().Get(subscriptionId);
      var resourceGroup = await subscription.Value.GetResourceGroups().GetAsync(settings.ResourceGroupName);
      var account = await resourceGroup.Value.GetCosmosDBAccounts().GetAsync(settings.AccountName);

      // The CosmosDB management SDK has evolving type names; we use dynamic-style calls via explicit methods.
      // If any of these ARM steps fail, we swallow (best-effort).
      await account.Value.GetCosmosDBSqlDatabases().CreateOrUpdateAsync(
        WaitUntil.Completed,
        settings.DatabaseName,
        new CosmosDBSqlDatabaseCreateOrUpdateContent(new AzureLocation(settings.Region), new CosmosDBSqlDatabaseResourceInfo(settings.DatabaseName)));

      var dbResource = account.Value.GetCosmosDBSqlDatabase(settings.DatabaseName, CancellationToken.None);
      await dbResource.Value.GetCosmosDBSqlContainers().CreateOrUpdateAsync(
        WaitUntil.Completed,
        settings.ContainerName,
        new CosmosDBSqlContainerCreateOrUpdateContent(new AzureLocation(settings.Region), new CosmosDBSqlContainerResourceInfo(settings.ContainerName)),
        CancellationToken.None);
    }
    catch
    {
      // Best-effort: ignore ARM errors.
    }
  }

  public static async Task EnsureDatabaseAndContainerExistAsync(CosmosSettings settings, TokenCredential credential)
  {
    var cosmosClient = new CosmosClient(settings.CosmosUri, credential);

    // Data-plane: required. Fail startup if this fails.
    var databaseResponse = await cosmosClient.CreateDatabaseIfNotExistsAsync(settings.DatabaseName, throughput: 400);

    var containerProperties = new ContainerProperties(id: settings.ContainerName, partitionKeyPath: "/auditId");
    await databaseResponse.Database.CreateContainerIfNotExistsAsync(containerProperties, throughput: 400);
  }
}
