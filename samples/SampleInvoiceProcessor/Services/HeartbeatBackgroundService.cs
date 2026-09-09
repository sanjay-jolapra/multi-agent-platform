using System.Text;
using System.Text.Json;
using SampleInvoiceProcessor.Agents;

namespace SampleInvoiceProcessor.Services;

/// <summary>
/// Periodically POSTs a heartbeat to the main platform's /api/v1/health/heartbeat endpoint,
/// shaped like the platform's HeartbeatPayloadDto. This is purely informational: the sample
/// app must keep working fully standalone even if the main platform is unreachable, so every
/// failure is caught and only logged as a warning.
/// </summary>
public class HeartbeatBackgroundService : BackgroundService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IPipelineStatusStore _statusStore;
    private readonly ILogger<HeartbeatBackgroundService> _logger;
    private readonly IConfiguration _configuration;
    private readonly TimeSpan _interval;
    private readonly TimeSpan _pollingInterval;
    private readonly Guid _applicationId;
    private readonly string _mainAppBaseUrl;

    public HeartbeatBackgroundService(
        IHttpClientFactory httpClientFactory,
        IPipelineStatusStore statusStore,
        ILogger<HeartbeatBackgroundService> logger,
        IConfiguration configuration)
    {
        _httpClientFactory = httpClientFactory;
        _statusStore = statusStore;
        _logger = logger;
        _configuration = configuration;

        _interval = TimeSpan.FromSeconds(configuration.GetValue<int?>("Heartbeat:IntervalSeconds") ?? 30);
        _pollingInterval = TimeSpan.FromSeconds(configuration.GetValue<int?>("Pipeline:PollingIntervalSeconds") ?? 30);
        _applicationId = Guid.TryParse(configuration["Application:Id"], out var id) ? id : Guid.Empty;
        _mainAppBaseUrl = configuration["Heartbeat:MainAppBaseUrl"] ?? "http://localhost:5000";
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await SendHeartbeatAsync(stoppingToken);

            try
            {
                await Task.Delay(_interval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // Expected during shutdown.
            }
        }
    }

    private async Task SendHeartbeatAsync(CancellationToken cancellationToken)
    {
        try
        {
            var snapshot = _statusStore.Snapshot;
            var status = HealthStatusHelper.Evaluate(snapshot, _pollingInterval);
            var totalInvoices = snapshot.LastRunInvoiceCount;
            var errorRate = totalInvoices > 0 ? (double)snapshot.LastRunInvalidCount / totalInvoices : 0d;

            var payload = new HeartbeatPayload(
                _applicationId,
                status.ToString(),
                snapshot.LastRunDurationMs,
                errorRate,
                "1.0.0",
                DateTimeOffset.UtcNow,
                new Dictionary<string, string>
                {
                    ["lastRunAt"] = snapshot.LastRunAt?.ToString("O") ?? "never",
                    ["runCount"] = snapshot.RunCount.ToString()
                });

            var json = JsonSerializer.Serialize(payload);
            using var content = new StringContent(json, Encoding.UTF8, "application/json");

            var client = _httpClientFactory.CreateClient(nameof(HeartbeatBackgroundService));
            var requestUri = $"{_mainAppBaseUrl.TrimEnd('/')}/api/v1/health/heartbeat";

            using var response = await client.PostAsync(requestUri, content, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Heartbeat to {Url} returned {StatusCode}", requestUri, response.StatusCode);
            }
        }
        catch (Exception ex)
        {
            // The main platform may be unreachable (e.g. running this sample standalone) -
            // that must never affect this app's own functionality, so we only log a warning.
            _logger.LogWarning(ex, "Failed to send heartbeat to main platform");
        }
    }

    // Field names match the main platform's HeartbeatPayloadDto exactly via camelCase JSON
    // property names, without taking a compile-time reference to that DTO.
    private record HeartbeatPayload(
        [property: System.Text.Json.Serialization.JsonPropertyName("applicationId")] Guid ApplicationId,
        [property: System.Text.Json.Serialization.JsonPropertyName("status")] string Status,
        [property: System.Text.Json.Serialization.JsonPropertyName("latencyMs")] double LatencyMs,
        [property: System.Text.Json.Serialization.JsonPropertyName("errorRate")] double ErrorRate,
        [property: System.Text.Json.Serialization.JsonPropertyName("version")] string Version,
        [property: System.Text.Json.Serialization.JsonPropertyName("timestamp")] DateTimeOffset Timestamp,
        [property: System.Text.Json.Serialization.JsonPropertyName("details")] Dictionary<string, string> Details);
}
