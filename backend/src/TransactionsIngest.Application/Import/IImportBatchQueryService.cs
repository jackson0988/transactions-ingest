using TransactionsIngest.Application.Import.Models;

namespace TransactionsIngest.Application.Import;

public interface IImportBatchQueryService
{
    Task<IReadOnlyList<ImportBatchDto>> GetRecentAsync(int take, CancellationToken cancellationToken = default);
}
