using Microsoft.Extensions.Diagnostics.HealthChecks;
using SampleInvoiceProcessor.Agents;

namespace SampleInvoiceProcessor.Services;

/// <summary>Reports the pipeline's health for /health/ready using the same thresholds the
/// heartbeat service uses, via HealthStatusHelper.</summary>
public class PipelineHealthCheck : IHealthCheck
{
    private readonly IPipelineStatusStore _statusStore;
    private readonly IConfiguration _configuration;

    public PipelineHealthCheck(IPipelineStatusStore statusStore, IConfiguration configuration)
    {
        _statusStore = statusStore;
        _configuration = configuration;
    }

    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var seconds = _configuration.GetValue<int?>("Pipeline:PollingIntervalSeconds") ?? 30;
        var interval = TimeSpan.FromSeconds(seconds);
        var snapshot = _statusStore.Snapshot;
        var status = HealthStatusHelper.Evaluate(snapshot, interval);

        var data = new Dictionary<string, object>
        {
            ["lastRunAt"] = snapshot.LastRunAt?.ToString("O") ?? "never",
            ["runCount"] = snapshot.RunCount,
            ["lastRunSucceeded"] = snapshot.LastRunSucceeded ?? false
        };

        return Task.FromResult(status switch
        {
            PipelineHealthStatus.Healthy => HealthCheckResult.Healthy("Pipeline is running on schedule", data),
            PipelineHealthStatus.Degraded => HealthCheckResult.Degraded("Pipeline run is overdue", null, data),
            _ => HealthCheckResult.Unhealthy("Pipeline has not run recently or last run failed", null, data)
        });
    }
}
