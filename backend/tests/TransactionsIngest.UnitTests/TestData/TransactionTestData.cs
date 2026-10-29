using TransactionsIngest.Application.Sources;
using TransactionsIngest.Domain.Entities;
using TransactionsIngest.Domain.Enums;

namespace TransactionsIngest.UnitTests.TestData;

internal static class TransactionTestData
{
    public static readonly DateTime Now = new(2026, 3, 1, 12, 0, 0, DateTimeKind.Utc);

    public static SourceTransaction Tx(
        int id,
        string source = "Store Uptown",
        decimal amount = 10m,
        double hoursAgo = 1,
        string product = "Wireless Mouse",
        string? card = "***1111",
        string location = "UPTOWN") =>
        new(source, id, card, location, product, amount, Now.AddHours(-hoursAgo));

    public static TransactionRecord Stored(SourceTransaction tx, TransactionStatus status = TransactionStatus.Active) => new()
    {
        Id = tx.TransactionId,
        SourceSystem = tx.SourceSystem,
        TransactionId = tx.TransactionId,
        CardNumber = tx.CardNumber,
        LocationCode = tx.LocationCode,
        ProductName = tx.ProductName,
        Amount = tx.Amount,
        TransactionTimeUtc = tx.TransactionTime,
        Status = status,
        RevisionNumber = 1,
        CreatedAtUtc = Now.AddDays(-1),
    };
}
