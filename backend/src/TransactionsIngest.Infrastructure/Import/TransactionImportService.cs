using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TransactionsIngest.Application.Import;
using TransactionsIngest.Application.Import.Models;
using TransactionsIngest.Application.Import.Reconciliation;
using TransactionsIngest.Application.Sources;
using TransactionsIngest.Domain.Constants;
using TransactionsIngest.Domain.Entities;
using TransactionsIngest.Domain.Enums;
using TransactionsIngest.Infrastructure.Persistence;

namespace TransactionsIngest.Infrastructure.Import;

public sealed class TransactionImportService(
    TransactionsDbContext db,
    IEnumerable<ITransactionSource> sources,
    TransactionReconciler reconciler,
    ImportLock importLock,
    TimeProvider timeProvider,
    IOptions<ImportOptions> options,
    ILogger<TransactionImportService> logger) : ITransactionImportService
{
    private readonly ImportOptions _options = options.Value;

    public async Task<ImportResult> RunAsync(string trigger, CancellationToken cancellationToken = default)
    {
        if (!importLock.TryEnter())
            throw new ImportAlreadyRunningException();

        try
        {
            return await RunCoreAsync(trigger, cancellationToken);
        }
        finally
        {
            importLock.Exit();
        }
    }

    private async Task<ImportResult> RunCoreAsync(string trigger, CancellationToken ct)
    {
        // one "now" for the whole batch so every source is judged against the same window
        var nowUtc = timeProvider.GetUtcNow().UtcDateTime;
        var batchId = Guid.NewGuid();

        db.ImportBatches.Add(new ImportBatch { Id = batchId, Trigger = trigger, StartedAtUtc = nowUtc });
        await db.SaveChangesAsync(ct);
        db.ChangeTracker.Clear();

        using var scope = logger.BeginScope(new Dictionary<string, object> { ["ImportBatchId"] = batchId });
        logger.LogInformation("Import {BatchId} started by {Trigger}", batchId, trigger);

        try
        {
            var summaries = new List<SourceImportSummary>();
            foreach (var source in sources)
            {
                summaries.Add(await ImportSourceAsync(source, batchId, nowUtc, ct));
            }

            var finalized = _options.EnableFinalization ? await FinalizeAsync(batchId, nowUtc, ct) : 0;

            return await CompleteBatchAsync(batchId, summaries, finalized, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Import {BatchId} failed", batchId);
            await MarkBatchFailedAsync(batchId, ex.Message);
            throw;
        }
    }

    private async Task<SourceImportSummary> ImportSourceAsync(ITransactionSource source, Guid batchId, DateTime nowUtc, CancellationToken ct)
    {
        IReadOnlyList<SourceTransaction> snapshot;
        try
        {
            snapshot = await source.GetSnapshotAsync(ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Important: don't reconcile at all when the fetch failed.
            // An empty snapshot would revoke every transaction of the last 24h.
            logger.LogError(ex, "Fetching snapshot from {SourceSystem} failed, source skipped", source.SourceSystem);
            return SourceImportSummary.Failed(source.SourceSystem, ex.Message);
        }

        var ids = snapshot.Select(t => t.TransactionId).Distinct().ToList();
        var windowStart = nowUtc - _options.ReconciliationWindow;

        var existing = await db.Transactions
            .Where(t => t.SourceSystem == source.SourceSystem)
            .Where(t => ids.Contains(t.TransactionId)
                        || (t.Status == TransactionStatus.Active && t.TransactionTimeUtc >= windowStart))
            .ToDictionaryAsync(t => t.TransactionId, ct);

        var result = reconciler.Reconcile(source.SourceSystem, snapshot, existing, batchId, nowUtc);

        foreach (var rejected in result.Rejected)
        {
            logger.LogWarning("Rejected {SourceSystem}/{TransactionId}: {Reason}",
                source.SourceSystem, rejected.TransactionId, rejected.Reason);
        }

        if (result.HasChanges)
        {
            db.Transactions.AddRange(result.NewRecords);
            db.AuditEntries.AddRange(result.AuditEntries);

            try
            {
                // one SaveChanges per source = one db transaction per source.
                // a failing source doesn't roll back the others.
                await db.SaveChangesAsync(ct);
            }
            catch (DbUpdateException ex)
            {
                logger.LogError(ex, "Saving changes for {SourceSystem} failed", source.SourceSystem);
                db.ChangeTracker.Clear();
                return SourceImportSummary.Failed(source.SourceSystem, "Could not save changes: " + ex.GetBaseException().Message);
            }
        }

        db.ChangeTracker.Clear();

        logger.LogInformation(
            "{SourceSystem}: received {Received}, inserted {Inserted}, updated {Updated}, revoked {Revoked}, restored {Restored}, unchanged {Unchanged}, rejected {Rejected}",
            source.SourceSystem, result.Received, result.Inserted, result.Updated, result.Revoked, result.Restored, result.Unchanged, result.Rejected.Count);

        return SourceImportSummary.From(result);
    }

    private async Task<int> FinalizeAsync(Guid batchId, DateTime nowUtc, CancellationToken ct)
    {
        var cutoff = nowUtc - _options.ReconciliationWindow;

        var due = await db.Transactions
            .Where(t => t.Status == TransactionStatus.Active && t.TransactionTimeUtc < cutoff)
            .ToListAsync(ct);

        if (due.Count == 0)
            return 0;

        foreach (var record in due)
        {
            db.AuditEntries.Add(record.FinalizeRecord(batchId, nowUtc));
        }

        await db.SaveChangesAsync(ct);
        db.ChangeTracker.Clear();

        logger.LogInformation("Finalized {Count} transactions older than {Cutoff:o}", due.Count, cutoff);
        return due.Count;
    }

    private async Task<ImportResult> CompleteBatchAsync(Guid batchId, List<SourceImportSummary> summaries, int finalized, CancellationToken ct)
    {
        var batch = await db.ImportBatches.SingleAsync(b => b.Id == batchId, ct);
        var failed = summaries.Where(s => s.Error is not null).ToList();

        batch.CompletedAtUtc = timeProvider.GetUtcNow().UtcDateTime;
        batch.Inserted = summaries.Sum(s => s.Inserted);
        batch.Updated = summaries.Sum(s => s.Updated);
        batch.Unchanged = summaries.Sum(s => s.Unchanged);
        batch.Revoked = summaries.Sum(s => s.Revoked);
        batch.Restored = summaries.Sum(s => s.Restored);
        batch.Rejected = summaries.Sum(s => s.Rejected);
        batch.Finalized = finalized;
        batch.Status = failed.Count switch
        {
            0 => ImportBatchStatus.Succeeded,
            _ when failed.Count == summaries.Count => ImportBatchStatus.Failed,
            _ => ImportBatchStatus.CompletedWithErrors,
        };
        batch.Error = failed.Count == 0
            ? null
            : Truncate(string.Join(" | ", failed.Select(f => $"{f.SourceSystem}: {f.Error}")));

        await db.SaveChangesAsync(ct);

        logger.LogInformation("Import {BatchId} finished with status {Status}", batchId, batch.Status);

        return new ImportResult(
            batch.Id, batch.Status, batch.StartedAtUtc, batch.CompletedAtUtc.Value,
            batch.Inserted, batch.Updated, batch.Unchanged, batch.Revoked, batch.Restored, batch.Rejected, batch.Finalized,
            summaries);
    }

    private async Task MarkBatchFailedAsync(Guid batchId, string error)
    {
        try
        {
            var completedAt = timeProvider.GetUtcNow().UtcDateTime;
            var message = Truncate(error);
            await db.ImportBatches
                .Where(b => b.Id == batchId)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(b => b.Status, ImportBatchStatus.Failed)
                    .SetProperty(b => b.Error, message)
                    .SetProperty(b => b.CompletedAtUtc, completedAt),
                    CancellationToken.None);
        }
        catch (Exception ex)
        {
            // already in a failure path, don't hide the original exception
            logger.LogWarning(ex, "Could not mark import batch {BatchId} as failed", batchId);
        }
    }

    private static string Truncate(string value) =>
        value.Length <= FieldLimits.MaxImportErrorLength ? value : value[..FieldLimits.MaxImportErrorLength];
}
