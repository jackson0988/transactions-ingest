using TransactionsIngest.Domain.Enums;

namespace TransactionsIngest.Application.Transactions.Models;

public sealed record TransactionSearchCriteria(
    int Page,
    int PageSize,
    string? SourceSystem,
    TransactionStatus? Status,
    string? Search,
    string? SortBy,
    bool Descending);
