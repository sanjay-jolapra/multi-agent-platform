using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using MultiAgentPlatform.Application.Dtos;
using MultiAgentPlatform.Domain.Enums;
using MultiAgentPlatform.Infrastructure.Persistence;

namespace MultiAgentPlatform.Web.Pages.Dashboard;

[Authorize]
public class IndexModel : PageModel
{
    private readonly MainDbContext _db;

    public IndexModel(MainDbContext db)
    {
        _db = db;
    }

    public DashboardFilterDto Filter { get; set; } = new(null, null, null, null, null, null, null);

    public DashboardMetricsDto Metrics { get; set; } = null!;

    public List<ApplicationListItemDto> Applications { get; set; } = new();

    public async Task OnGetAsync(DashboardFilterDto filter)
    {
        Filter = filter;
        var appsQuery = _db.GeneratedApplications.AsQueryable();

        if (!string.IsNullOrWhiteSpace(Filter.UserId))
        {
            appsQuery = appsQuery.Where(a => a.OwnerUserId == Filter.UserId);
        }

        if (Filter.ApplicationId is not null)
        {
            appsQuery = appsQuery.Where(a => a.Id == Filter.ApplicationId);
        }

        if (Filter.HealthState is not null)
        {
            appsQuery = appsQuery.Where(a => a.HealthState == Filter.HealthState);
        }

        var allApps = await appsQuery.ToListAsync();

        var agentLogsQuery = _db.AgentExecutionLogs.AsQueryable();
        var connectorLogsQuery = _db.ConnectorExecutionLogs.AsQueryable();

        if (Filter.AgentDefinitionId is not null)
        {
            agentLogsQuery = agentLogsQuery.Where(l => l.AgentDefinitionId == Filter.AgentDefinitionId);
        }

        if (Filter.ConnectorDefinitionId is not null)
        {
            connectorLogsQuery = connectorLogsQuery.Where(l => l.ConnectorDefinitionId == Filter.ConnectorDefinitionId);
        }

        if (Filter.From is not null)
        {
            agentLogsQuery = agentLogsQuery.Where(l => l.StartedAt >= Filter.From);
            connectorLogsQuery = connectorLogsQuery.Where(l => l.StartedAt >= Filter.From);
        }

        if (Filter.To is not null)
        {
            agentLogsQuery = agentLogsQuery.Where(l => l.StartedAt <= Filter.To);
            connectorLogsQuery = connectorLogsQuery.Where(l => l.StartedAt <= Filter.To);
        }

        var agentLogs = await agentLogsQuery.ToListAsync();
        var completedAgentLogs = agentLogs.Where(l => l.CompletedAt is not null).ToList();

        var since24h = DateTimeOffset.UtcNow.AddHours(-24);
        var connectorFailures24h = await connectorLogsQuery
            .Where(l => l.Status == ExecutionStatus.Failed && l.StartedAt >= since24h)
            .CountAsync();

        var recentDeploymentsCount = await _db.DeploymentLogs
            .Where(d => d.Timestamp >= since24h)
            .CountAsync();

        Metrics = new DashboardMetricsDto(
            TotalApplications: allApps.Count,
            RunningApplications: allApps.Count(a => a.Status == ApplicationStatus.Running),
            StoppedApplications: allApps.Count(a => a.Status == ApplicationStatus.Stopped),
            Healthy: allApps.Count(a => a.HealthState == HealthState.Healthy),
            Degraded: allApps.Count(a => a.HealthState == HealthState.Degraded),
            Unhealthy: allApps.Count(a => a.HealthState == HealthState.Unhealthy),
            Offline: allApps.Count(a => a.HealthState == HealthState.Offline),
            Unknown: allApps.Count(a => a.HealthState == HealthState.Unknown),
            AgentExecutionsTotal: agentLogs.Count,
            AgentExecutionsSucceeded: agentLogs.Count(l => l.Status == ExecutionStatus.Success),
            AgentExecutionsFailed: agentLogs.Count(l => l.Status == ExecutionStatus.Failed),
            AverageAgentExecutionMs: completedAgentLogs.Count == 0
                ? 0
                : completedAgentLogs.Average(l => (l.CompletedAt!.Value - l.StartedAt).TotalMilliseconds),
            ConnectorFailuresLast24h: connectorFailures24h,
            RecentDeploymentsCount: recentDeploymentsCount);

        Applications = allApps
            .OrderByDescending(a => a.CreatedAt)
            .Select(a => new ApplicationListItemDto(
                a.Id, a.Name, a.Slug, a.Status, a.HealthState, a.LastHeartbeatAt, a.OwnerUserId, a.Version, a.CreatedAt))
            .ToList();
    }
}
