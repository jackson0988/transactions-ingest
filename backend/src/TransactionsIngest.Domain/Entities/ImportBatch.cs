using TransactionsIngest.Domain.Enums;

namespace TransactionsIngest.Domain.Entities;

/// <summary>One import run. Every audit entry points back to the batch that caused it.</summary>
public class ImportBatch
{
    public Guid Id { get; set; }

    /// <summary>Who started it - "manual", "scheduler", "test"...</summary>
    public string Trigger { get; set; } = "manual";

    public DateTime StartedAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public ImportBatchStatus Status { get; set; } = ImportBatchStatus.Running;

    public int Inserted { get; set; }
    public int Updated { get; set; }
    public int Unchanged { get; set; }
    public int Revoked { get; set; }
    public int Restored { get; set; }
    public int Rejected { get; set; }
    public int Finalized { get; set; }

    public string? Error { get; set; }
}
