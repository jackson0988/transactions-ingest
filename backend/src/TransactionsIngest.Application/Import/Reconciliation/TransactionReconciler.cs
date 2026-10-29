using System.Globalization;
using Microsoft.Extensions.Options;
using TransactionsIngest.Application.Sources;
using TransactionsIngest.Domain.Entities;
using TransactionsIngest.Domain.Enums;

namespace TransactionsIngest.Application.Import.Reconciliation;

/// <summary>
/// Pure reconciliation of one source snapshot against what we already have stored.
/// No db access in here on purpose, so all the rules can be unit tested without EF.
/// Mutates the existing (tracked) records and returns new records + audit entries.
/// </summary>
public sealed class TransactionReconciler(IOptions<ImportOptions> options)
{
    private readonly TimeSpan _window = options.Value.ReconciliationWindow;

    /// <param name="existing">
    /// Stored records of this source, keyed by TransactionId. Needs to contain at least every id in the
    /// snapshot plus all Active records inside the window, otherwise revocations will be missed.
    /// </param>
    public ReconciliationResult Reconcile(
        string sourceSystem,
        IReadOnlyCollection<SourceTransaction> snapshot,
        IReadOnlyDictionary<int, TransactionRecord> existing,
        Guid batchId,
        DateTime nowUtc)
    {
        var result = new ReconciliationResult(sourceSystem, snapshot.Count);
        var windowStart = nowUtc - _window;
        var seen = new HashSet<int>();

        foreach (var raw in snapshot)
        {
            if (!seen.Add(raw.TransactionId))
            {
                result.Reject(raw.TransactionId, "Duplicate TransactionId in snapshot, first occurrence was used.");
                continue;
            }

            var errors = SourceTransactionValidator.Validate(raw, sourceSystem);
            if (errors.Count > 0)
            {
                // id stays in `seen` - a broken row from the source must not revoke the good one we have stored
                result.Reject(raw.TransactionId, string.Join(" ", errors));
                continue;
            }

            var incoming = SourceTransactionValidator.Normalize(raw, sourceSystem);

            if (!existing.TryGetValue(incoming.TransactionId, out var record))
            {
                Insert(result, incoming, batchId, nowUtc);
                continue;
            }

            if (IsLocked(record, windowStart))
            {
                result.Unchanged++;
                continue;
            }

            Apply(result, record, incoming, batchId, nowUtc);
        }

        foreach (var record in existing.Values)
        {
            if (record.Status != TransactionStatus.Active || seen.Contains(record.TransactionId))
                continue;

            // only inside the window - older ones simply aged out of the source snapshot
            if (IsLocked(record, windowStart))
                continue;

            result.AuditEntries.Add(record.Revoke(batchId, nowUtc));
            result.Revoked++;
        }

        return result;
    }

    private static bool IsLocked(TransactionRecord record, DateTime windowStart) =>
        record.Status == TransactionStatus.Finalized || record.TransactionTimeUtc < windowStart;

    private static void Insert(ReconciliationResult result, SourceTransaction incoming, Guid batchId, DateTime nowUtc)
    {
        var record = new TransactionRecord
        {
            SourceSystem = incoming.SourceSystem,
            TransactionId = incoming.TransactionId,
            CardNumber = incoming.CardNumber,
            LocationCode = incoming.LocationCode,
            ProductName = incoming.ProductName,
            Amount = incoming.Amount,
            TransactionTimeUtc = incoming.TransactionTime,
            Status = TransactionStatus.Active,
            RevisionNumber = 1,
            CreatedAtUtc = nowUtc,
            LastImportBatchId = batchId,
        };

        result.NewRecords.Add(record);
        result.AuditEntries.Add(AuditEntry.Create(record, AuditChangeType.Created, batchId, nowUtc));
    }

    private static void Apply(ReconciliationResult result, TransactionRecord record, SourceTransaction incoming, Guid batchId, DateTime nowUtc)
    {
        var changes = Diff(record, incoming).ToList();
        var wasRevoked = record.Status == TransactionStatus.Revoked;

        if (changes.Count == 0 && !wasRevoked)
        {
            result.Unchanged++;
            return;
        }

        foreach (var (field, oldValue, newValue) in changes)
        {
            result.AuditEntries.Add(AuditEntry.Create(record, AuditChangeType.Updated, batchId, nowUtc, field, oldValue, newValue));
        }

        record.CardNumber = incoming.CardNumber;
        record.LocationCode = incoming.LocationCode;
        record.ProductName = incoming.ProductName;
        record.Amount = incoming.Amount;
        record.TransactionTimeUtc = incoming.TransactionTime;

        if (changes.Count > 0)
            result.Updated++;

        if (wasRevoked)
        {
            // Restore() bumps the revision itself
            result.AuditEntries.Add(record.Restore(batchId, nowUtc));
            result.Restored++;
        }
        else
        {
            record.Touch(batchId, nowUtc);
        }
    }

    private static IEnumerable<(string Field, string? Old, string? New)> Diff(TransactionRecord r, SourceTransaction s)
    {
        if (!string.Equals(r.CardNumber, s.CardNumber, StringComparison.Ordinal))
            yield return (nameof(r.CardNumber), r.CardNumber, s.CardNumber);

        if (!string.Equals(r.LocationCode, s.LocationCode, StringComparison.Ordinal))
            yield return (nameof(r.LocationCode), r.LocationCode, s.LocationCode);

        if (!string.Equals(r.ProductName, s.ProductName, StringComparison.Ordinal))
            yield return (nameof(r.ProductName), r.ProductName, s.ProductName);

        if (r.Amount != s.Amount)
            yield return (nameof(r.Amount), FormatAmount(r.Amount), FormatAmount(s.Amount));

        if (r.TransactionTimeUtc != s.TransactionTime)
            yield return ("TransactionTime", FormatTime(r.TransactionTimeUtc), FormatTime(s.TransactionTime));
    }

    private static string FormatAmount(decimal value) => value.ToString("0.00", CultureInfo.InvariantCulture);

    private static string FormatTime(DateTime value) => value.ToString("yyyy-MM-ddTHH:mm:ss.FFFFFFFZ", CultureInfo.InvariantCulture);
}
