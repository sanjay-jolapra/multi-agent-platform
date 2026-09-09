namespace SampleInvoiceProcessor.Agents;

/// <summary>
/// Stamps the run timestamp into the context and updates the shared IPipelineStatusStore so
/// health checks and the heartbeat service can report the pipeline's current state.
/// </summary>
public class MonitoringAgent : IPipelineAgent
{
    private readonly IPipelineStatusStore _statusStore;

    public string Name => "Monitoring";

    public MonitoringAgent(IPipelineStatusStore statusStore)
    {
        _statusStore = statusStore;
    }

    public Task<AgentResult> ExecuteAsync(AgentContext context, CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        context.Data["lastRunAt"] = now;

        var invalidCount = context.Data.TryGetValue("invalidInvoices", out var invalidRaw) && invalidRaw is List<Dictionary<string, object?>> invalid
            ? invalid.Count
            : 0;
        var validCount = context.Data.TryGetValue("validInvoices", out var validRaw) && validRaw is List<Dictionary<string, object?>> valid
            ? valid.Count
            : 0;

        var hardFailure = context.Trace.Any(t => t.Contains("[FAIL]"));

        var durationMs = context.Data.TryGetValue("_pipelineStartedAt", out var startedRaw) && startedRaw is DateTimeOffset startedAt
            ? (long)(now - startedAt).TotalMilliseconds
            : 0;

        _statusStore.RecordRun(
            succeeded: !hardFailure,
            durationMs: durationMs,
            invoiceCount: validCount + invalidCount,
            invalidCount: invalidCount);

        return Task.FromResult(AgentResult.Ok(new Dictionary<string, object?> { ["lastRunAt"] = now }));
    }
}
