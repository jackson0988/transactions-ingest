using TransactionsIngest.Domain.Enums;

namespace TransactionsIngest.Application.Import.Models;

public sealed record ImportBatchDto(
    Guid Id,
    string Trigger,
    ImportBatchStatus Status,
    DateTime StartedAtUtc,
    DateTime? CompletedAtUtc,
    int Inserted,
    int Updated,
    int Unchanged,
    int Revoked,
    int Restored,
    int Rejected,
    int Finalized,
    string? Error);
