using TransactionsIngest.Domain.Constants;
using TransactionsIngest.Domain.Enums;

namespace TransactionsIngest.Domain.Entities;

public class AuditEntry
{
    public long Id { get; set; }

    public long TransactionRecordId { get; set; }
    public TransactionRecord Transaction { get; set; } = null!;

    public Guid? ImportBatchId { get; set; }

    public AuditChangeType ChangeType { get; set; }

    // null for whole-row events like Created
    public string? FieldName { get; set; }
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }

    public DateTime ChangedAtUtc { get; set; }

    public static AuditEntry Create(
        TransactionRecord transaction,
        AuditChangeType changeType,
        Guid? batchId,
        DateTime changedAtUtc,
        string? fieldName = null,
        string? oldValue = null,
        string? newValue = null) => new()
    {
        // set the navigation, not only the FK - for new records Id is still 0 here
        Transaction = transaction,
        TransactionRecordId = transaction.Id,
        ImportBatchId = batchId,
        ChangeType = changeType,
        FieldName = fieldName,
        OldValue = Clip(oldValue),
        NewValue = Clip(newValue),
        ChangedAtUtc = changedAtUtc,
    };

    private static string? Clip(string? value) =>
        value is { Length: > FieldLimits.MaxAuditValueLength } ? value[..FieldLimits.MaxAuditValueLength] : value;
}
