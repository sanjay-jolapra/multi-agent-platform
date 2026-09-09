namespace SampleInvoiceProcessor.Agents;

public class PipelineRunSnapshot
{
    public DateTimeOffset? LastRunAt { get; init; }
    public bool? LastRunSucceeded { get; init; }
    public long RunCount { get; init; }
    public long LastRunDurationMs { get; init; }
    public int LastRunInvoiceCount { get; init; }
    public int LastRunInvalidCount { get; init; }
    public string? LastError { get; init; }
}

/// <summary>
/// In-memory snapshot of the pipeline's health, registered as a DI singleton (not a static
/// field) so it stays testable and so multiple consumers - health checks, the heartbeat
/// service, the status page - all read a single consistent, injectable source of truth.
/// </summary>
public interface IPipelineStatusStore
{
    PipelineRunSnapshot Snapshot { get; }

    void RecordRun(bool succeeded, long durationMs, int invoiceCount, int invalidCount, string? error = null);
}

public class PipelineStatusStore : IPipelineStatusStore
{
    private readonly object _lock = new();
    private PipelineRunSnapshot _snapshot = new();

    public PipelineRunSnapshot Snapshot
    {
        get
        {
            lock (_lock)
            {
                return _snapshot;
            }
        }
    }

    public void RecordRun(bool succeeded, long durationMs, int invoiceCount, int invalidCount, string? error = null)
    {
        lock (_lock)
        {
            _snapshot = new PipelineRunSnapshot
            {
                LastRunAt = DateTimeOffset.UtcNow,
                LastRunSucceeded = succeeded,
                RunCount = _snapshot.RunCount + 1,
                LastRunDurationMs = durationMs,
                LastRunInvoiceCount = invoiceCount,
                LastRunInvalidCount = invalidCount,
                LastError = error
            };
        }
    }
}
