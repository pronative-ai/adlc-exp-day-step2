using Microsoft.Azure.Cosmos;
using Azure.Core;
using OuterloopLabApi.Cosmos;
using OuterloopLabApi.Models;

namespace OuterloopLabApi.Repositories;

public sealed class CosmosAuditTrailRepository : IAuditTrailRepository
{
  private readonly CosmosSettings _settings;
  private readonly Container _container;

  public CosmosAuditTrailRepository(CosmosSettings settings, TokenCredential credential)
  {
    _settings = settings;
    var client = new CosmosClient(settings.CosmosUri, credential);
    _container = client.GetContainer(settings.DatabaseName, settings.ContainerName);
  }

  public async Task CreateAsync(AuditTrailRecord record, CancellationToken cancellationToken = default)
  {
    record.AuditId = record.AuditId.Trim();
    await _container.CreateItemAsync(record, new PartitionKey(record.AuditId), cancellationToken: cancellationToken);
  }

  public async Task<AuditTrailRecord?> GetByAuditIdAsync(string auditId, CancellationToken cancellationToken = default)
  {
    try
    {
      var response = await _container.ReadItemAsync<AuditTrailRecord>(auditId, new PartitionKey(auditId), cancellationToken: cancellationToken);
      return response.Resource;
    }
    catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
    {
      return null;
    }
  }
}
