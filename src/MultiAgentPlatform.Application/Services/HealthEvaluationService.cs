using MultiAgentPlatform.Application.Options;
using MultiAgentPlatform.Domain.Enums;

namespace MultiAgentPlatform.Application.Services;

public interface IHealthEvaluationService
{
    /// <summary>Derives Healthy/Degraded/Unhealthy/Offline/Unknown from the reported state and elapsed time since the last heartbeat.</summary>
    HealthState Evaluate(string? reportedStatus, DateTimeOffset? lastHeartbeatAt, DateTimeOffset now);
}

public class HealthEvaluationService : IHealthEvaluationService
{
    private readonly HealthMonitoringOptions _options;

    public HealthEvaluationService(HealthMonitoringOptions options) => _options = options;

    public HealthState Evaluate(string? reportedStatus, DateTimeOffset? lastHeartbeatAt, DateTimeOffset now)
    {
        if (lastHeartbeatAt is null)
        {
            return HealthState.Unknown;
        }

        var missedIntervals = (now - lastHeartbeatAt.Value).TotalSeconds / Math.Max(1, _options.ExpectedHeartbeatIntervalSeconds);

        if (missedIntervals >= _options.OfflineAfterMissedHeartbeats)
        {
            return HealthState.Offline;
        }

        if (missedIntervals >= _options.DegradedAfterMissedHeartbeats)
        {
            return HealthState.Degraded;
        }

        return reportedStatus?.ToLowerInvariant() switch
        {
            "healthy" or "up" => HealthState.Healthy,
            "degraded" => HealthState.Degraded,
            "unhealthy" or "down" => HealthState.Unhealthy,
            _ => HealthState.Unknown
        };
    }
}
