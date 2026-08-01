namespace OuterloopLabApi.Conversion;

public interface IConversionAuditRepository
{
    Task CreateAsync(ConversionAuditDocument document);
    Task<ConversionAuditDocument?> GetByIdAsync(string id);
}
