using TransactionsIngest.Application.Import.Reconciliation;

namespace TransactionsIngest.Application.Import.Models;

public sealed record SourceImportSummary(
    string SourceSystem,
    int Received,
    int Inserted,
    int Updated,
    int Unchanged,
    int Revoked,
    int Restored,
    int Rejected,
    string? Error)
{
    public static SourceImportSummary Failed(string sourceSystem, string error) =>
        new(sourceSystem, 0, 0, 0, 0, 0, 0, 0, error);

    public static SourceImportSummary From(ReconciliationResult r) =>
        new(r.SourceSystem, r.Received, r.Inserted, r.Updated, r.Unchanged, r.Revoked, r.Restored, r.Rejected.Count, null);
}
