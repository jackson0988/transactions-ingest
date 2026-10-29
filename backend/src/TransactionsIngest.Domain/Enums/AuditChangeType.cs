namespace TransactionsIngest.Domain.Enums;

public enum AuditChangeType
{
    Created = 0,
    Updated = 1,
    Revoked = 2,
    Restored = 3,
    Finalized = 4,
}
