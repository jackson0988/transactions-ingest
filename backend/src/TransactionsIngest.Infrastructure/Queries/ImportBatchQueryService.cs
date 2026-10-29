using Microsoft.EntityFrameworkCore;
using TransactionsIngest.Application.Import;
using TransactionsIngest.Application.Import.Models;
using TransactionsIngest.Infrastructure.Persistence;

namespace TransactionsIngest.Infrastructure.Queries;

public sealed class ImportBatchQueryService(TransactionsDbContext db) : IImportBatchQueryService
{
    public async Task<IReadOnlyList<ImportBatchDto>> GetRecentAsync(int take, CancellationToken cancellationToken = default) =>
        await db.ImportBatches.AsNoTracking()
            .OrderByDescending(b => b.StartedAtUtc)
            .Take(take)
            .Select(b => new ImportBatchDto(
                b.Id, b.Trigger, b.Status, b.StartedAtUtc, b.CompletedAtUtc,
                b.Inserted, b.Updated, b.Unchanged, b.Revoked, b.Restored, b.Rejected, b.Finalized, b.Error))
            .ToListAsync(cancellationToken);
}
