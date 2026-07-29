using Microsoft.Azure.Cosmos;
using OuterloopLabApi.Options;
using Microsoft.Extensions.Options;

namespace OuterloopLabApi.Infrastructure;

public sealed class CosmosProvisioner
{
  private readonly CosmosClient _cosmosClient;
  private readonly CosmosOptions _options;
  private readonly ILogger<CosmosProvisioner> _logger;

  public CosmosProvisioner(IOptions<CosmosOptions> options, ILogger<CosmosProvisioner> logger)
  {
    _options = options.Value;
    _logger = logger;

    var credential = new Azure.Identity.DefaultAzureCredential(new Azure.Identity.DefaultAzureCredentialOptions
    {
      ManagedIdentityClientId = _options.ManagedIdentityClientId
    });

    _cosmosClient = new CosmosClient(_options.Uri, credential);
  }

  public async Task EnsureResourcesAsync(CancellationToken ct)
  {
    try
    {
      var db = await _cosmosClient.CreateDatabaseIfNotExistsAsync(_options.DatabaseName, cancellationToken: ct);
      var props = new ContainerProperties(id: _options.ContainerName, partitionKeyPath: "/id");
      await db.Database.CreateContainerIfNotExistsAsync(props, throughput: 400, cancellationToken: ct);
    }
    catch (Exception ex)
    {
      // Best-effort: container may already exist or permissions may differ.
      _logger.LogWarning(ex, "Cosmos provisioning skipped.");
    }
  }
}
