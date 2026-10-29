using Microsoft.Extensions.Options;
using TransactionsIngest.Application.Import;
using TransactionsIngest.Application.Import.Reconciliation;
using TransactionsIngest.Application.Sources;
using TransactionsIngest.Domain.Entities;
using TransactionsIngest.Domain.Enums;
using static TransactionsIngest.UnitTests.TestData.TransactionTestData;

namespace TransactionsIngest.UnitTests.Import.Reconciliation;

public class TransactionReconcilerTests
{
    private const string Source = "Store Uptown";
    private static readonly Guid BatchId = Guid.NewGuid();

    private readonly TransactionReconciler _sut = new(Options.Create(new ImportOptions { ReconciliationWindowHours = 24 }));

    private ReconciliationResult Run(IReadOnlyCollection<SourceTransaction> snapshot, params TransactionRecord[] stored) =>
        _sut.Reconcile(Source, snapshot, stored.ToDictionary(r => r.TransactionId), BatchId, Now);

    [Fact]
    public void New_transactions_are_inserted_with_created_audit()
    {
        var result = Run([Tx(1), Tx(2)]);

        Assert.Equal(2, result.Inserted);
        Assert.All(result.NewRecords, r => Assert.Equal(TransactionStatus.Active, r.Status));
        Assert.Equal(2, result.AuditEntries.Count(a => a.ChangeType == AuditChangeType.Created));
    }

    [Fact]
    public void Identical_snapshot_changes_nothing()
    {
        var tx = Tx(1);
        var stored = Stored(tx);

        var result = Run([tx], stored);

        Assert.False(result.HasChanges);
        Assert.Equal(1, result.Unchanged);
        Assert.Equal(1, stored.RevisionNumber);
    }

    [Fact]
    public void Changed_fields_are_updated_and_audited_per_field()
    {
        var stored = Stored(Tx(1, amount: 10m, product: "Mouse"));

        var result = Run([Tx(1, amount: 12.50m, product: "Wireless Mouse")], stored);

        Assert.Equal(1, result.Updated);
        Assert.Equal(12.50m, stored.Amount);
        Assert.Equal("Wireless Mouse", stored.ProductName);
        Assert.Equal(2, stored.RevisionNumber);

        var amountChange = Assert.Single(result.AuditEntries, a => a.FieldName == "Amount");
        Assert.Equal("10.00", amountChange.OldValue);
        Assert.Equal("12.50", amountChange.NewValue);
        Assert.Single(result.AuditEntries, a => a.FieldName == "ProductName");
    }

    [Fact]
    public void Amount_is_compared_after_rounding()
    {
        var stored = Stored(Tx(1, amount: 10.00m));

        var result = Run([Tx(1, amount: 10.001m)], stored);

        Assert.False(result.HasChanges);
    }

    [Fact]
    public void Missing_transaction_inside_window_is_revoked()
    {
        var stored = Stored(Tx(1, hoursAgo: 3));

        var result = Run([], stored);

        Assert.Equal(1, result.Revoked);
        Assert.Equal(TransactionStatus.Revoked, stored.Status);
        var audit = Assert.Single(result.AuditEntries);
        Assert.Equal(AuditChangeType.Revoked, audit.ChangeType);
    }

    [Fact]
    public void Missing_transaction_outside_window_is_not_revoked()
    {
        var stored = Stored(Tx(1, hoursAgo: 30));

        var result = Run([], stored);

        Assert.Equal(0, result.Revoked);
        Assert.Equal(TransactionStatus.Active, stored.Status);
    }

    [Fact]
    public void Revoked_transaction_that_shows_up_again_is_restored()
    {
        var tx = Tx(1);
        var stored = Stored(tx, TransactionStatus.Revoked);

        var result = Run([tx], stored);

        Assert.Equal(1, result.Restored);
        Assert.Equal(0, result.Updated);
        Assert.Equal(TransactionStatus.Active, stored.Status);
        Assert.Equal(2, stored.RevisionNumber);
    }

    [Fact]
    public void Finalized_transaction_is_never_modified()
    {
        var stored = Stored(Tx(1, amount: 10m), TransactionStatus.Finalized);

        var result = Run([Tx(1, amount: 99m)], stored);

        Assert.False(result.HasChanges);
        Assert.Equal(10m, stored.Amount);
    }

    [Fact]
    public void Invalid_row_is_rejected_and_does_not_revoke_the_stored_one()
    {
        var stored = Stored(Tx(1));
        var invalid = Tx(1, product: "A product name that is way too long");

        var result = Run([invalid], stored);

        Assert.Single(result.Rejected);
        Assert.Equal(0, result.Revoked);
        Assert.Equal(TransactionStatus.Active, stored.Status);
    }

    [Fact]
    public void Duplicate_ids_in_snapshot_keep_first_occurrence()
    {
        var result = Run([Tx(1, amount: 5m), Tx(1, amount: 6m)]);

        var record = Assert.Single(result.NewRecords);
        Assert.Equal(5m, record.Amount);
        Assert.Single(result.Rejected);
    }
}
