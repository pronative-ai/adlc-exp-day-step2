using System.Net;
using Microsoft.Azure.Cosmos;
using OuterloopLabApi.Configuration;
using OuterloopLabApi.Models;

namespace OuterloopLabApi.Services;

public sealed class CosmosAuditRepository : IAuditRepository
{
    private readonly CosmosClient _cosmosClient;
    private readonly CosmosDbOptions _options;

    public CosmosAuditRepository(CosmosClient cosmosClient, CosmosDbOptions options)
    {
        _cosmosClient = cosmosClient;
        _options = options;
    }

    public async Task AddAsync(AuditRecord record, CancellationToken cancellationToken)
    {
        var container = _cosmosClient.GetContainer(_options.Database, _options.Container);
        await container.CreateItemAsync(record, new PartitionKey(record.TenantId), cancellationToken: cancellationToken);
    }

    public async Task<AuditRecord?> GetAsync(string tenantId, string auditId, CancellationToken cancellationToken)
    {
        var container = _cosmosClient.GetContainer(_options.Database, _options.Container);
        try
        {
            var response = await container.ReadItemAsync<AuditRecord>(
                auditId, new PartitionKey(tenantId), cancellationToken: cancellationToken);
            return response.Resource;
        }
        catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }
    }
}
