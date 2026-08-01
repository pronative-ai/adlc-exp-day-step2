namespace OuterloopLabApi.Exceptions;

public sealed class AuditRecordNotFoundException : Exception
{
    public AuditRecordNotFoundException(string auditId)
        : base($"No audit record was found for id '{auditId}'.")
    {
    }
}
