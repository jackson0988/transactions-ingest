using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using TransactionsIngest.Infrastructure.Persistence;

namespace TransactionsIngest.IntegrationTests.Fixtures;

/// <summary>In-memory sqlite lives as long as the connection is open, so we keep one per test class.</summary>
internal sealed class SqliteInMemoryDatabase : IDisposable
{
    private readonly SqliteConnection _connection = new("DataSource=:memory:");

    public SqliteInMemoryDatabase()
    {
        _connection.Open();
        using var db = CreateContext();
        db.Database.EnsureCreated();
    }

    public TransactionsDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<TransactionsDbContext>().UseSqlite(_connection).Options);

    public void Dispose() => _connection.Dispose();
}
