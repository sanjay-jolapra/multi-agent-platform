using MultiAgentPlatform.Domain.Enums;

namespace MultiAgentPlatform.Application.Dtos;

public record ApplicationListItemDto(
    Guid Id, string Name, string Slug, ApplicationStatus Status, HealthState HealthState,
    DateTimeOffset? LastHeartbeatAt, string OwnerUserId, string Version, DateTimeOffset CreatedAt);

public record DashboardMetricsDto(
    int TotalApplications, int RunningApplications, int StoppedApplications,
    int Healthy, int Degraded, int Unhealthy, int Offline, int Unknown,
    int AgentExecutionsTotal, int AgentExecutionsSucceeded, int AgentExecutionsFailed,
    double AverageAgentExecutionMs, int ConnectorFailuresLast24h, int RecentDeploymentsCount);

public record DashboardFilterDto(string? UserId, Guid? ApplicationId, HealthState? HealthState,
    Guid? AgentDefinitionId, Guid? ConnectorDefinitionId, DateTimeOffset? From, DateTimeOffset? To);

public record HeartbeatPayloadDto(
    Guid ApplicationId, string Status, double LatencyMs, double ErrorRate,
    string Version, DateTimeOffset Timestamp, Dictionary<string, string>? Details);
