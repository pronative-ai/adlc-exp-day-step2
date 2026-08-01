using Microsoft.Azure.Cosmos;
using OuterloopLabApi.Models;

namespace OuterloopLabApi.Audits;

public sealed class CosmosAuditRepository : IAuditRepository
{
    private readonly Container _container;

    public CosmosAuditRepository(Container container)
    {
        _container = container;
    }

    public Task AddAsync(AuditRecord record, CancellationToken cancellationToken)
    {
        return _container.CreateItemAsync(record, new PartitionKey(record.PartitionKey), cancellationToken: cancellationToken);
    }

    public async Task<AuditRecord?> GetByAuditIdAsync(string auditId, CancellationToken cancellationToken)
    {
        // Cross-partition query by id since callers only know auditId.
        var query = new QueryDefinition("SELECT * FROM c WHERE c.id = @id").WithParameter("@id", auditId);
        using var iterator = _container.GetItemQueryIterator<AuditRecord>(query, requestOptions: new QueryRequestOptions { MaxItemCount = 1 });
        if (iterator.HasMoreResults)
        {
            var page = await iterator.ReadNextAsync(cancellationToken);
            return page.Resource.FirstOrDefault();
        }

        return null;
    }

    public async Task<IReadOnlyList<AuditRecord>> ListRecentAsync(int limit, CancellationToken cancellationToken)
    {
        var safeLimit = Math.Clamp(limit, 1, 50);
        var query = new QueryDefinition("SELECT * FROM c ORDER BY c.executionTimestampUtc DESC OFFSET 0 LIMIT @limit").WithParameter("@limit", safeLimit);

        using var iterator = _container.GetItemQueryIterator<AuditRecord>(query, requestOptions: new QueryRequestOptions { MaxItemCount = safeLimit });
        var results = new List<AuditRecord>();
        while (iterator.HasMoreResults && results.Count < safeLimit)
        {
            var page = await iterator.ReadNextAsync(cancellationToken);
            results.AddRange(page.Resource);
        }

        return results;
    }
}
