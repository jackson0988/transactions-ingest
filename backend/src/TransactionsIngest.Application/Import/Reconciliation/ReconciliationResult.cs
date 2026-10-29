using TransactionsIngest.Domain.Entities;

namespace TransactionsIngest.Application.Import.Reconciliation;

public sealed class ReconciliationResult(string sourceSystem, int received)
{
    public string SourceSystem { get; } = sourceSystem;
    public int Received { get; } = received;

    public List<TransactionRecord> NewRecords { get; } = [];
    public List<AuditEntry> AuditEntries { get; } = [];
    public List<RejectedTransaction> Rejected { get; } = [];

    public int Inserted => NewRecords.Count;
    public int Updated { get; internal set; }
    public int Unchanged { get; internal set; }
    public int Revoked { get; internal set; }
    public int Restored { get; internal set; }

    public bool HasChanges => NewRecords.Count > 0 || AuditEntries.Count > 0;

    internal void Reject(int transactionId, string reason) => Rejected.Add(new RejectedTransaction(transactionId, reason));
}
