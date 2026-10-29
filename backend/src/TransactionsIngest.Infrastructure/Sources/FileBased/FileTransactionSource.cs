using System.Text.Json;
using Microsoft.Extensions.Logging;
using TransactionsIngest.Application.Sources;
using TransactionsIngest.Infrastructure.Sources.Options;

namespace TransactionsIngest.Infrastructure.Sources.FileBased;

/// <summary>
/// Mock source system. Reads its "API response" from a json file on every call, so you can edit
/// the file while the app runs and hit import again to simulate corrections / removals.
/// </summary>
public sealed class FileTransactionSource(
    SourceSystemOptions options,
    string dataDirectory,
    SampleDataAnchor anchor,
    ILogger<FileTransactionSource> logger) : ITransactionSource
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public string SourceSystem => options.Name;

    public async Task<IReadOnlyList<SourceTransaction>> GetSnapshotAsync(CancellationToken cancellationToken = default)
    {
        if (options.SimulateFailure)
            throw new SourceSystemUnavailableException(options.Name, "simulated outage (SimulateFailure=true)");

        var path = Path.Combine(dataDirectory, options.File);
        if (!File.Exists(path))
            throw new SourceSystemUnavailableException(options.Name, $"sample file '{path}' not found");

        await using var stream = File.OpenRead(path);
        var rows = await JsonSerializer.DeserializeAsync<List<SampleRow>>(stream, JsonOptions, cancellationToken) ?? [];

        logger.LogDebug("Read {Count} rows for {SourceSystem} from {Path}", rows.Count, options.Name, path);

        return rows.Select(ToSourceTransaction).ToList();
    }

    private SourceTransaction ToSourceTransaction(SampleRow row) => new(
        row.SourceSystem ?? options.Name,
        row.TransactionId,
        row.CardNumber,
        row.LocationCode ?? string.Empty,
        row.ProductName ?? string.Empty,
        row.Amount,
        row.TransactionTime ?? (row.HoursAgo is { } hours ? anchor.UtcNow.AddHours(-hours) : default));

    // hoursAgo lets the sample files keep transactions inside the 24h window no matter when you run it
    private sealed record SampleRow(
        string? SourceSystem,
        int TransactionId,
        string? CardNumber,
        string? LocationCode,
        string? ProductName,
        decimal Amount,
        DateTime? TransactionTime,
        double? HoursAgo);
}
