using Microsoft.Azure.Cosmos;

namespace OuterloopLabApi.Conversion;

public sealed class CosmosConversionAuditRepository : IConversionAuditRepository
{
    private readonly Container _container;

    public CosmosConversionAuditRepository(Container container)
    {
        _container = container;
    }

    public Task CreateAsync(ConversionAuditDocument document)
    {
        return _container.CreateItemAsync(document, new PartitionKey(document.PartitionKey));
    }

    public async Task<ConversionAuditDocument?> GetByIdAsync(string id)
    {
        try
        {
            var response = await _container.ReadItemAsync<ConversionAuditDocument>(id, new PartitionKey(id));
            return response.Resource;
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
    }
}
