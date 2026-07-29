using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;
using OuterloopLabApi.Dtos;
using OuterloopLabApi.Options;
using Microsoft.Extensions.Options;

namespace OuterloopLabApi.Repositories;

public sealed class CosmosConversionAuditRepository : IConversionAuditRepository
{
  private readonly Container _container;

  public CosmosConversionAuditRepository(IOptions<CosmosOptions> options)
  {
    var optionsValue = options.Value;

    var credential = new Azure.Identity.DefaultAzureCredential(new Azure.Identity.DefaultAzureCredentialOptions
    {
      ManagedIdentityClientId = optionsValue.ManagedIdentityClientId
    });

    var cosmosClient = new CosmosClient(optionsValue.Uri, credential);

    var container = cosmosClient.GetContainer(optionsValue.DatabaseName, optionsValue.ContainerName);
    _container = container;
  }

  public async Task CreateAsync(ConversionAuditRecordDto record, CancellationToken ct)
  {
    // Immutable audit event: create a new document with partition key /id.
    await _container.CreateItemAsync(record, new PartitionKey(record.Id), cancellationToken: ct);
  }

  public async Task<ConversionAuditRecordDto?> GetByIdAsync(string id, CancellationToken ct)
  {
    try
    {
      var resp = await _container.ReadItemAsync<ConversionAuditRecordDto>(id, new PartitionKey(id), cancellationToken: ct);
      return resp.Resource;
    }
    catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
    {
      return null;
    }
  }
}
