using TransactionsIngest.Application.Common.Models;
using TransactionsIngest.Application.Transactions.Models;

namespace TransactionsIngest.Application.Transactions;

public interface ITransactionQueryService
{
    Task<PagedResult<TransactionDto>> SearchAsync(TransactionSearchCriteria criteria, CancellationToken cancellationToken = default);

    Task<TransactionDto?> GetByIdAsync(long id, CancellationToken cancellationToken = default);

    /// <returns>null when the transaction does not exist.</returns>
    Task<IReadOnlyList<AuditEntryDto>?> GetAuditHistoryAsync(long id, CancellationToken cancellationToken = default);
}
