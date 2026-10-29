using TransactionsIngest.Domain.Enums;

namespace TransactionsIngest.Domain.Entities;

/// <summary>
/// A transaction as we store it. Logical key is (SourceSystem, TransactionId),
/// Id is just a surrogate key for the db / api.
/// </summary>
public class TransactionRecord
{
    public long Id { get; set; }

    public string SourceSystem { get; set; } = string.Empty;
    public int TransactionId { get; set; }

    public string? CardNumber { get; set; }
    public string LocationCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTime TransactionTimeUtc { get; set; }

    public TransactionStatus Status { get; set; } = TransactionStatus.Active;
    public int RevisionNumber { get; set; } = 1;

    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
    public Guid? LastImportBatchId { get; set; }

    public List<AuditEntry> AuditEntries { get; set; } = [];

    public AuditEntry Revoke(Guid batchId, DateTime nowUtc) =>
        ChangeStatus(TransactionStatus.Revoked, AuditChangeType.Revoked, batchId, nowUtc);

    public AuditEntry Restore(Guid batchId, DateTime nowUtc) =>
        ChangeStatus(TransactionStatus.Active, AuditChangeType.Restored, batchId, nowUtc);

    public AuditEntry FinalizeRecord(Guid batchId, DateTime nowUtc) =>
        ChangeStatus(TransactionStatus.Finalized, AuditChangeType.Finalized, batchId, nowUtc);

    public void Touch(Guid batchId, DateTime nowUtc)
    {
        RevisionNumber++;
        UpdatedAtUtc = nowUtc;
        LastImportBatchId = batchId;
    }

    private AuditEntry ChangeStatus(TransactionStatus newStatus, AuditChangeType changeType, Guid batchId, DateTime nowUtc)
    {
        var oldStatus = Status;
        Status = newStatus;
        Touch(batchId, nowUtc);
        return AuditEntry.Create(this, changeType, batchId, nowUtc, nameof(Status), oldStatus.ToString(), newStatus.ToString());
    }
}
