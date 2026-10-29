namespace TransactionsIngest.Infrastructure.Sources.Options;

public sealed class TransactionSourcesOptions
{
    public const string SectionName = "TransactionSources";

    /// <summary>Relative paths are resolved from the content root.</summary>
    public string SampleDataPath { get; set; } = "SampleData";

    public List<SourceSystemOptions> Sources { get; set; } = [];
}
