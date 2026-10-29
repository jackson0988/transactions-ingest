using System.ComponentModel.DataAnnotations;
using TransactionsIngest.Application.Transactions.Models;
using TransactionsIngest.Domain.Constants;
using TransactionsIngest.Domain.Enums;

namespace TransactionsIngest.Api.Contracts.Requests;

public sealed class TransactionSearchRequest
{
    [Range(1, int.MaxValue)]
    public int Page { get; init; } = 1;

    [Range(1, 200)]
    public int PageSize { get; init; } = 25;

    [MaxLength(FieldLimits.MaxStringLength)]
    public string? SourceSystem { get; init; }

    public TransactionStatus? Status { get; init; }

    [MaxLength(100)]
    public string? Search { get; init; }

    /// <summary>sourceSystem, transactionId, locationCode, productName, amount, transactionTime, status</summary>
    public string? SortBy { get; init; }

    [RegularExpression("(?i)^(asc|desc)$", ErrorMessage = "sortDir must be 'asc' or 'desc'.")]
    public string? SortDir { get; init; }

    public TransactionSearchCriteria ToCriteria() => new(
        Page,
        PageSize,
        SourceSystem,
        Status,
        Search,
        SortBy,
        Descending: string.Equals(SortDir, "desc", StringComparison.OrdinalIgnoreCase));
}
