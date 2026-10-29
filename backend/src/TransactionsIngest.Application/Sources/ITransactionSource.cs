namespace TransactionsIngest.Application.Sources;

/// <summary>
/// One upstream system (Store Uptown, Store Downtown, Online...). Returns the full
/// current snapshot - anything missing from it is treated as removed by the source.
/// </summary>
public interface ITransactionSource
{
    string SourceSystem { get; }

    Task<IReadOnlyList<SourceTransaction>> GetSnapshotAsync(CancellationToken cancellationToken = default);
}
