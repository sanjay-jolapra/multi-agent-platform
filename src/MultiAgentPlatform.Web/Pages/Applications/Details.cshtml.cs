using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using MultiAgentPlatform.Domain.Entities;
using MultiAgentPlatform.Domain.Enums;
using MultiAgentPlatform.Infrastructure.Identity;
using MultiAgentPlatform.Infrastructure.Persistence;

namespace MultiAgentPlatform.Web.Pages.Applications;

[Authorize]
public class DetailsModel : PageModel
{
    private readonly MainDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;

    public DetailsModel(MainDbContext db, UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    public GeneratedApplication Application { get; set; } = null!;
    public List<AgentDefinition> Agents { get; set; } = new();
    public List<ConnectorDefinition> Connectors { get; set; } = new();
    public List<HealthCheckRecord> HealthHistory { get; set; } = new();
    public List<AgentExecutionLog> ExecutionHistory { get; set; } = new();
    public List<DeploymentLog> DeploymentHistory { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        var application = await _db.GeneratedApplications.FirstOrDefaultAsync(a => a.Id == id);

        if (application is null)
        {
            return NotFound();
        }

        var userId = _userManager.GetUserId(User);
        var isOwner = application.OwnerUserId == userId;
        var isPrivileged = User.IsInRole(nameof(PlatformRole.PlatformAdministrator)) ||
                            User.IsInRole(nameof(PlatformRole.ReadOnlyAuditor));

        if (!isOwner && !isPrivileged)
        {
            return Forbid();
        }

        Application = application;

        Agents = await _db.AgentDefinitions
            .Where(a => a.GeneratedApplicationId == id)
            .OrderBy(a => a.ExecutionOrder)
            .ToListAsync();

        Connectors = await _db.ConnectorDefinitions
            .Where(c => c.GeneratedApplicationId == id)
            .ToListAsync();

        HealthHistory = await _db.HealthCheckRecords
            .Where(h => h.GeneratedApplicationId == id)
            .OrderByDescending(h => h.Timestamp)
            .Take(50)
            .ToListAsync();

        ExecutionHistory = await _db.AgentExecutionLogs
            .Where(l => l.GeneratedApplicationId == id)
            .OrderByDescending(l => l.StartedAt)
            .Take(50)
            .ToListAsync();

        DeploymentHistory = await _db.DeploymentLogs
            .Where(d => d.GeneratedApplicationId == id)
            .OrderByDescending(d => d.Timestamp)
            .ToListAsync();

        return Page();
    }
}
