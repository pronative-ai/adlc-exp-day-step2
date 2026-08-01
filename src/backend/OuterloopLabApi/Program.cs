using Azure.Core;
using Azure.Identity;
using Azure.ResourceManager;
using Azure.ResourceManager.CosmosDB;
using Azure.ResourceManager.CosmosDB.Models;
using Azure.ResourceManager.Resources;
using OuterloopLabApi.Audits;
using OuterloopLabApi.Configuration;
using OuterloopLabApi.Conversion;
using OuterloopLabApi.Models;
using OuterloopLabApi.Providers;
using Microsoft.Azure.Cosmos;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Runtime-only configuration: no appsettings fallback.
var config = AppConfigLoader.LoadFromEnvironment();

var credential = new DefaultAzureCredential(new DefaultAzureCredentialOptions
{
    ManagedIdentityClientId = config.AzureManagedIdentityClientId,
});

// Token-authenticated data-plane access.
var cosmosClient = new CosmosClient(config.CosmosDbUri, credential);

// Best-effort control-plane provisioning.
TryProvisionCosmosSqlAsync(config, credential).GetAwaiter().GetResult();

var database = await cosmosClient.CreateDatabaseIfNotExistsAsync(config.CosmosDbDatabase, throughput: config.CosmosThroughput);
var containerProperties = new ContainerProperties(config.CosmosDbContainer, partitionKeyPath: "/partitionKey");
var containerResponse = await database.Database.CreateContainerIfNotExistsAsync(containerProperties, throughput: config.CosmosThroughput);
var container = containerResponse.Container;

builder.Services.AddSingleton<IAuditRepository>(_ => new CosmosAuditRepository(container));
builder.Services.AddSingleton<ConversionService>();

builder.Services.AddHttpClient<ICurrencyRateProvider, FrankfurterCurrencyRateProvider>(client =>
{
    client.BaseAddress = new Uri(config.CurrencyApiBaseUrl.TrimEnd('/'));
    client.Timeout = TimeSpan.FromSeconds(10);
});

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.MapPost("/api/convert", async (ConvertRequest request, ConversionService svc, CancellationToken ct) =>
{
    try
    {
        var result = await svc.ConvertAsync(request, ct);
        return Results.Ok(result);
    }
    catch (CurrencyProviderException ex)
    {
        return Results.Problem(
            title: "Upstream rate provider failure",
            detail: ex.Message,
            statusCode: StatusCodes.Status503ServiceUnavailable);
    }
    catch (InvalidOperationException ex)
    {
        return Results.Problem(
            title: "Invalid conversion request",
            detail: ex.Message,
            statusCode: StatusCodes.Status400BadRequest);
    }
});

app.MapGet("/api/audits", async (int? limit, IAuditRepository repo, CancellationToken ct) =>
{
    var items = await repo.ListRecentAsync(limit ?? 10, ct);
    return Results.Ok(items.Select(ToConvertResponse));
});

app.MapGet("/api/audits/{auditId}", async (string auditId, IAuditRepository repo, CancellationToken ct) =>
{
    var item = await repo.GetByAuditIdAsync(auditId, ct);
    if (item is null)
        return Results.NotFound();
    return Results.Ok(ToConvertResponse(item));
});

app.Run();

static ConvertResponse ToConvertResponse(AuditRecord record) => new()
{
    AuditId = record.Id,
    Rate = record.Rate,
    ConvertedAmount = record.ConvertedAmount,
    ExecutionTimestampUtc = record.ExecutionTimestampUtc,
    ProviderDate = record.ProviderDate,
    ProviderSequenceMarker = record.ProviderSequenceMarker,
};

static async Task TryProvisionCosmosSqlAsync(AppConfig config, TokenCredential credential)
{
    // Requirement: ARM provisioning is best-effort; if it fails, startup still proceeds to data-plane create-if-not-exists.
    // Managed Identity RBAC for ARM can differ from data-plane RBAC.
    try
    {
        var subscriptionId = Environment.GetEnvironmentVariable("SUBSCRIPTION_ID")
            ?? Environment.GetEnvironmentVariable("AZURE_SUBSCRIPTION_ID");
        if (string.IsNullOrWhiteSpace(subscriptionId))
            return;

        var armClient = new ArmClient(credential, subscriptionId);
        var rgId = new Azure.Core.ResourceIdentifier($"/subscriptions/{subscriptionId}/resourceGroups/{config.CosmosDbResourceGroup}");
        ResourceGroupResource rg = armClient.GetResourceGroupResource(rgId);

        var cosmosAccounts = rg.GetCosmosDBAccounts();
        var cosmosAccount = cosmosAccounts.Get(config.CosmosDbAccountName);

        var sqlDbCollection = cosmosAccount.Value.GetCosmosDBSqlDatabases();
        var dbInfo = new CosmosDBSqlDatabaseResourceInfo(config.CosmosDbDatabase);
        var dbContent = new CosmosDBSqlDatabaseCreateOrUpdateContent(new Azure.Core.AzureLocation(config.CosmosDbRegion), dbInfo);
        var dbOp = sqlDbCollection.CreateOrUpdate(Azure.ResourceManager.WaitUntil.Completed, config.CosmosDbDatabase, dbContent);
        var sqlDbResource = dbOp.Value;

        var sqlContainerCollection = sqlDbResource.GetCosmosDBSqlContainers();
        var partitionKey = new CosmosDBContainerPartitionKey
        {
            Kind = CosmosDBPartitionKind.Hash,
            Paths = new List<string> { "/partitionKey" }
        };
        var containerInfo = new CosmosDBSqlContainerResourceInfo(config.CosmosDbContainer)
        {
            PartitionKey = partitionKey
        };

        var containerContent = new CosmosDBSqlContainerCreateOrUpdateContent(new Azure.Core.AzureLocation(config.CosmosDbRegion), containerInfo);
        var containerOp = sqlContainerCollection.CreateOrUpdate(Azure.ResourceManager.WaitUntil.Completed, config.CosmosDbContainer, containerContent);
    }
    catch
    {
        // Best-effort: do not fail startup for ARM RBAC differences.
    }
}
