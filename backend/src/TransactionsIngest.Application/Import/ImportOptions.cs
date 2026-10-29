using System.ComponentModel.DataAnnotations;

namespace TransactionsIngest.Application.Import;

public sealed class ImportOptions
{
    public const string SectionName = "Import";

    /// <summary>
    /// How long after TransactionTime a transaction can still be changed or revoked.
    /// After that it's locked, and finalized if EnableFinalization is on.
    /// </summary>
    [Range(1, 24 * 30)]
    public int ReconciliationWindowHours { get; set; } = 24;

    public bool EnableFinalization { get; set; } = true;

    public TimeSpan ReconciliationWindow => TimeSpan.FromHours(ReconciliationWindowHours);
}
