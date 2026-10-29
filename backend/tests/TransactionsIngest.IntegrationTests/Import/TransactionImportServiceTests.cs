using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using TransactionsIngest.Application.Import;
using TransactionsIngest.Application.Import.Models;
using TransactionsIngest.Application.Import.Reconciliation;
using TransactionsIngest.Domain.Entities;
using TransactionsIngest.Domain.Enums;
using TransactionsIngest.Infrastructure.Import;
using TransactionsIngest.IntegrationTests.Fixtures;
using static TransactionsIngest.IntegrationTests.Fixtures.FakeTransactionSource;

namespace TransactionsIngest.IntegrationTests.Import;

public sealed class TransactionImportServiceTests : IDisposable
{
    private readonly SqliteInMemoryDatabase _database = new();
    private readonly FakeTimeProvider _time = new(new DateTimeOffset(Now));
    private readonly FakeTransactionSource _uptown = new("Store Uptown");
    private readonly FakeTransactionSource _online = new("Online");
    private readonly ImportLock _lock = new();

    [Fact]
    public async Task Running_twice_with_the_same_data_is_idempotent()
    {
        _uptown.Items.AddRange([Tx(1), Tx(2)]);
        _online.Items.Add(Tx(1, source: "Online"));

        var first = await RunImportAsync();
        var auditAfterFirst = await CountAsync<AuditEntry>();
        var second = await RunImportAsync();

        Assert.Equal(3, first.Inserted);
        Assert.Equal(0, second.Inserted);
        Assert.Equal(0, second.Updated);
        Assert.Equal(0, second.Revoked);
        Assert.Equal(3, second.Unchanged);
        Assert.Equal(auditAfterFirst, await CountAsync<AuditEntry>());
        Assert.Equal(3, await CountAsync<TransactionRecord>());
    }

    [Fact]
    public async Task Same_transaction_id_in_different_sources_is_stored_separately()
    {
        _uptown.Items.Add(Tx(100001, amount: 24.99m));
        _online.Items.Add(Tx(100001, source: "Online", amount: 129.95m, location: "ONLINE"));

        await RunImportAsync();

        await using var db = _database.CreateContext();
        Assert.Equal(2, await db.Transactions.CountAsync(t => t.TransactionId == 100001));
    }

    [Fact]
    public async Task Removed_transaction_is_revoked_on_next_run()
    {
        _uptown.Items.AddRange([Tx(1), Tx(2)]);
        await RunImportAsync();

        _uptown.Items.RemoveAll(t => t.TransactionId == 2);
        _time.Advance(TimeSpan.FromHours(1));
        var result = await RunImportAsync();

        Assert.Equal(1, result.Revoked);
        await using var db = _database.CreateContext();
        var revoked = await db.Transactions.SingleAsync(t => t.TransactionId == 2);
        Assert.Equal(TransactionStatus.Revoked, revoked.Status);
        Assert.True(await db.AuditEntries.AnyAsync(a =>
            a.TransactionRecordId == revoked.Id && a.ChangeType == AuditChangeType.Revoked));
    }

    [Fact]
    public async Task Failed_source_does_not_revoke_its_transactions()
    {
        _uptown.Items.Add(Tx(1));
        await RunImportAsync();

        _uptown.Fail = true;
        var result = await RunImportAsync();

        Assert.Equal(ImportBatchStatus.CompletedWithErrors, result.Status);
        Assert.Equal(0, result.Revoked);
        Assert.NotNull(result.Sources.Single(s => s.SourceSystem == "Store Uptown").Error);

        await using var db = _database.CreateContext();
        Assert.Equal(TransactionStatus.Active, (await db.Transactions.SingleAsync()).Status);
    }

    [Fact]
    public async Task Transactions_older_than_window_get_finalized()
    {
        _uptown.Items.AddRange([Tx(1, hoursAgo: 30), Tx(2, hoursAgo: 1)]);

        var result = await RunImportAsync();

        Assert.Equal(1, result.Finalized);
        await using var db = _database.CreateContext();
        Assert.Equal(TransactionStatus.Finalized, (await db.Transactions.SingleAsync(t => t.TransactionId == 1)).Status);
        Assert.Equal(TransactionStatus.Active, (await db.Transactions.SingleAsync(t => t.TransactionId == 2)).Status);
    }

    [Fact]
    public async Task Import_is_rejected_while_another_one_is_running()
    {
        Assert.True(_lock.TryEnter());
        try
        {
            await Assert.ThrowsAsync<ImportAlreadyRunningException>(() => RunImportAsync());
        }
        finally
        {
            _lock.Exit();
        }
    }

    private async Task<ImportResult> RunImportAsync()
    {
        await using var db = _database.CreateContext();
        var options = Options.Create(new ImportOptions());
        var service = new TransactionImportService(
            db, [_uptown, _online], new TransactionReconciler(options), _lock, _time, options,
            NullLogger<TransactionImportService>.Instance);

        return await service.RunAsync("test");
    }

    private async Task<int> CountAsync<T>() where T : class
    {
        await using var db = _database.CreateContext();
        return await db.Set<T>().CountAsync();
    }

    public void Dispose()
    {
        _lock.Dispose();
        _database.Dispose();
    }
}
