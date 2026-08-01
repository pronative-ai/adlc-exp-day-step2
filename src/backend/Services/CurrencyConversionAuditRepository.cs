using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Options;
using OuterloopLabApi.Configuration;
using OuterloopLabApi.Models;

namespace OuterloopLabApi.Services;

public interface ICurrencyConversionAuditRepository
{
    Task CreateAsync(CurrencyConversionAuditRecord record, CancellationToken cancellationToken);

    Task<CurrencyConversionAuditRecord?> GetByIdAsync(string auditId, CancellationToken cancellationToken);
}

public sealed class CurrencyConversionAuditRepository : ICurrencyConversionAuditRepository
{
    private readonly Container _container;

    public CurrencyConversionAuditRepository(CosmosClient cosmosClient, IOptions<CosmosOptions> cosmosOptions)
    {
        CosmosOptions options = cosmosOptions.Value;
        _container = cosmosClient.GetContainer(options.DatabaseName, options.ContainerName);
    }

    public async Task CreateAsync(CurrencyConversionAuditRecord record, CancellationToken cancellationToken)
    {
        await _container.CreateItemAsync(record, new PartitionKey(record.Id), cancellationToken: cancellationToken);
    }

    public async Task<CurrencyConversionAuditRecord?> GetByIdAsync(string auditId, CancellationToken cancellationToken)
    {
        try
        {
            ItemResponse<CurrencyConversionAuditRecord> response = await _container.ReadItemAsync<CurrencyConversionAuditRecord>(
                auditId,
                new PartitionKey(auditId),
                cancellationToken: cancellationToken);

            return response.Resource;
        }
        catch (CosmosException exception) when (exception.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
    }
}
