using Azure.Core;
using Azure.ResourceManager;
using Azure.ResourceManager.CosmosDB;
using Microsoft.Azure.Cosmos;
using System.Net.Http.Json;

namespace OuterloopLabApi.Cosmos;

public static class CosmosProvisioner
{
    public static async Task TryProvisionWithArmBestEffortAsync(CosmosClient cosmosClient, CosmosSettings settings, TokenCredential credential)
    {
        // Constraint: ARM provisioning is best-effort; Managed Identity RBAC for ARM may differ from data-plane RBAC.
        // We only attempt if AZURE_SUBSCRIPTION_ID is present; otherwise we skip without failing startup.
        var subscriptionId = Environment.GetEnvironmentVariable("AZURE_SUBSCRIPTION_ID");
        if (string.IsNullOrWhiteSpace(subscriptionId))
        {
            return;
        }

        // Intentionally best-effort: if ARM call fails, we still rely on data-plane create-if-not-exists.
        try
        {
            // Minimal ARM attempt via control plane HTTP PUT.
            // Note: if this fails due to RBAC or missing API details, we swallow.
            // Data-plane creation remains the source of truth.
            using var http = new HttpClient();
            var token = await credential.GetTokenAsync(new TokenRequestContext(new[] { "https://management.azure.com/.default" }));
            http.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token.Value);

            var accountResourceId = $"/subscriptions/{subscriptionId}/resourceGroups/{settings.CosmosResourceGroup}/providers/Microsoft.DocumentDB/databaseAccounts/{settings.CosmosAccountName}";
            var dbResourceId = $"{accountResourceId}/sqlDatabases/{settings.DatabaseId}";
            var containerResourceId = $"{dbResourceId}/containers/{settings.ContainerId}";

            // API version choice is best-effort; even if wrong, data-plane will still create resources.
            const string apiVersion = "2024-05-15";

            var dbUrl = $"https://management.azure.com{dbResourceId}?api-version={apiVersion}";
            var dbBody = new
            {
                properties = new
                {
                    resource = new { id = settings.DatabaseId }
                }
            };

            await http.PutAsJsonAsync(dbUrl, dbBody);

            var containerUrl = $"https://management.azure.com{containerResourceId}?api-version={apiVersion}";
            var containerBody = new
            {
                properties = new
                {
                    resource = new
                    {
                        id = settings.ContainerId,
                        partitionKey = new { paths = new[] { "/pk" }, kind = "Hash" }
                    },
                    options = new { throughput = 400 }
                }
            };

            await http.PutAsJsonAsync(containerUrl, containerBody);
        }
        catch
        {
            // Swallow ARM errors (best-effort constraint).
        }
    }

    public static async Task<Container> CreateDatabaseAndContainerIfNotExistsAsync(CosmosClient cosmosClient, CosmosSettings settings)
    {
        var databaseResponse = await cosmosClient.CreateDatabaseIfNotExistsAsync(settings.DatabaseId);
        if (databaseResponse is null)
        {
            throw new InvalidOperationException("Failed to create or get Cosmos database.");
        }

        var containerProperties = new ContainerProperties(settings.ContainerId, "/pk");
        // If token-authenticated data-plane create-if-not-exists fails, startup must fail.
        await databaseResponse.Database.CreateContainerIfNotExistsAsync(containerProperties, throughput: 400);

        return cosmosClient.GetContainer(settings.DatabaseId, settings.ContainerId);
    }
}
