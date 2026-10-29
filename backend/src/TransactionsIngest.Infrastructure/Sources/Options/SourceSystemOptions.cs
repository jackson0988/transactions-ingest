namespace TransactionsIngest.Infrastructure.Sources.Options;

public sealed class SourceSystemOptions
{
    public string Name { get; set; } = string.Empty;
    public string File { get; set; } = string.Empty;
    public bool Enabled { get; set; } = true;

    /// <summary>For demos - makes the source throw so you can see a failed source doesn't revoke anything.</summary>
    public bool SimulateFailure { get; set; }
}
