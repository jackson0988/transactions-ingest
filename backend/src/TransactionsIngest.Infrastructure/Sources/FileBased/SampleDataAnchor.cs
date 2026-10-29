namespace TransactionsIngest.Infrastructure.Sources.FileBased;

/// <summary>
/// Fixed reference time for "hoursAgo" in the sample files, taken once at startup.
/// If it moved on every call the mock data would "change" every import and break idempotency.
/// </summary>
public sealed class SampleDataAnchor(TimeProvider timeProvider)
{
    public DateTime UtcNow { get; } = TruncateToMinute(timeProvider.GetUtcNow().UtcDateTime);

    private static DateTime TruncateToMinute(DateTime value) =>
        new(value.Ticks - value.Ticks % TimeSpan.TicksPerMinute, DateTimeKind.Utc);
}
