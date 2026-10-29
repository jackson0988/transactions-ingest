using TransactionsIngest.Application.Sources;

namespace TransactionsIngest.IntegrationTests.Fixtures;

internal sealed class FakeTransactionSource(string name) : ITransactionSource
{
    public string SourceSystem => name;

    public List<SourceTransaction> Items { get; } = [];

    public bool Fail { get; set; }

    public Task<IReadOnlyList<SourceTransaction>> GetSnapshotAsync(CancellationToken cancellationToken = default) =>
        Fail
            ? throw new SourceSystemUnavailableException(name, "simulated outage")
            : Task.FromResult<IReadOnlyList<SourceTransaction>>(Items.ToList());

    public static readonly DateTime Now = new(2026, 3, 1, 12, 0, 0, DateTimeKind.Utc);

    public static SourceTransaction Tx(
        int id,
        string source = "Store Uptown",
        decimal amount = 10m,
        double hoursAgo = 1,
        string location = "UPTOWN") =>
        new(source, id, "***1111", location, "Wireless Mouse", amount, Now.AddHours(-hoursAgo));
}
