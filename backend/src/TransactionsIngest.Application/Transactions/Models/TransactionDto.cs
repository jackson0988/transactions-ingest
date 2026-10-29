using TransactionsIngest.Domain.Enums;

namespace TransactionsIngest.Application.Transactions.Models;

public sealed record TransactionDto(
    long Id,
    string SourceSystem,
    int TransactionId,
    string? CardNumber,
    string LocationCode,
    string ProductName,
    decimal Amount,
    DateTime TransactionTime,
    TransactionStatus Status,
    int RevisionNumber,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc);
