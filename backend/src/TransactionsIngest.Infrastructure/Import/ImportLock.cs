namespace TransactionsIngest.Infrastructure.Import;

/// <summary>
/// Makes sure only one import runs at a time (manual click + scheduler at the same minute etc).
/// In-process only - if the API is ever scaled out this has to become a db / distributed lock.
/// </summary>
public sealed class ImportLock : IDisposable
{
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    public bool TryEnter() => _semaphore.Wait(0);

    public void Exit() => _semaphore.Release();

    public void Dispose() => _semaphore.Dispose();
}
