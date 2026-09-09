using SampleInvoiceProcessor.Agents;

namespace SampleInvoiceProcessor.Services;

public enum PipelineHealthStatus
{
    Healthy,
    Degraded,
    Unhealthy
}

/// <summary>
/// Shared threshold logic used by both PipelineHealthCheck (/health/ready) and
/// HeartbeatBackgroundService so the two never drift out of sync: Healthy if the last run
/// succeeded within 2x the polling interval, Degraded within 5x, Unhealthy otherwise (or if
/// there has never been a run, or the last run failed).
/// </summary>
public static class HealthStatusHelper
{
    public static PipelineHealthStatus Evaluate(PipelineRunSnapshot snapshot, TimeSpan pollingInterval)
    {
        if (snapshot.LastRunAt is null)
        {
            return PipelineHealthStatus.Unhealthy;
        }

        if (snapshot.LastRunSucceeded != true)
        {
            return PipelineHealthStatus.Unhealthy;
        }

        var age = DateTimeOffset.UtcNow - snapshot.LastRunAt.Value;

        if (age <= pollingInterval * 2)
        {
            return PipelineHealthStatus.Healthy;
        }

        if (age <= pollingInterval * 5)
        {
            return PipelineHealthStatus.Degraded;
        }

        return PipelineHealthStatus.Unhealthy;
    }
}
