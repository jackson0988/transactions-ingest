using TransactionsIngest.Application.Import.Models;

namespace TransactionsIngest.Application.Import;

/// <summary>
/// Entry point for an import run. The API calls it for manual imports; an hourly
/// scheduler (Hangfire, Quartz, a k8s CronJob hitting the endpoint...) can call the same thing.
/// </summary>
public interface ITransactionImportService
{
    /// <exception cref="ImportAlreadyRunningException">another import is in progress.</exception>
    Task<ImportResult> RunAsync(string trigger, CancellationToken cancellationToken = default);
}
