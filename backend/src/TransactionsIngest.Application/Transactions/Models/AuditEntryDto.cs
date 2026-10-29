using TransactionsIngest.Domain.Enums;

namespace TransactionsIngest.Application.Transactions.Models;

public sealed record AuditEntryDto(
    long Id,
    long TransactionRecordId,
    Guid? ImportBatchId,
    AuditChangeType ChangeType,
    string? FieldName,
    string? OldValue,
    string? NewValue,
    DateTime ChangedAtUtc);
