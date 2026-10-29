namespace TransactionsIngest.Application.Sources;

public sealed class SourceSystemUnavailableException(string sourceSystem, string message, Exception? inner = null)
    : Exception($"Source system '{sourceSystem}' is unavailable: {message}", inner)
{
    public string SourceSystem { get; } = sourceSystem;
}
