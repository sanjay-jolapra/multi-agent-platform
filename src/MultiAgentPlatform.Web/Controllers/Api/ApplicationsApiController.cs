using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MultiAgentPlatform.Application.Dtos;
using MultiAgentPlatform.Domain.Enums;
using MultiAgentPlatform.Infrastructure.Identity;
using MultiAgentPlatform.Infrastructure.Persistence;

namespace MultiAgentPlatform.Web.Controllers.Api;

[ApiController]
[Route("api/v1/applications")]
[Authorize]
public class ApplicationsApiController : ControllerBase
{
    private readonly MainDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;

    public ApplicationsApiController(MainDbContext db, UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    [HttpGet]
    public async Task<ActionResult<ApplicationListItemDto[]>> List()
    {
        var userId = _userManager.GetUserId(User);

        var apps = await _db.GeneratedApplications
            .Where(a => a.OwnerUserId == userId)
            .OrderByDescending(a => a.CreatedAt)
            .Select(a => new ApplicationListItemDto(
                a.Id, a.Name, a.Slug, a.Status, a.HealthState,
                a.LastHeartbeatAt, a.OwnerUserId, a.Version, a.CreatedAt))
            .ToArrayAsync();

        return Ok(apps);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Detail(Guid id)
    {
        var userId = _userManager.GetUserId(User);
        var app = await _db.GeneratedApplications.FirstOrDefaultAsync(a => a.Id == id);

        if (app is null)
        {
            return NotFound();
        }

        if (app.OwnerUserId != userId && !User.IsInRole(nameof(PlatformRole.PlatformAdministrator)) &&
            !User.IsInRole(nameof(PlatformRole.ReadOnlyAuditor)))
        {
            return Forbid();
        }

        var agents = await _db.AgentDefinitions
            .Where(a => a.GeneratedApplicationId == id)
            .OrderBy(a => a.ExecutionOrder)
            .ToListAsync();

        var connectors = await _db.ConnectorDefinitions
            .Where(c => c.GeneratedApplicationId == id)
            .ToListAsync();

        var recentLogs = await _db.AgentExecutionLogs
            .Where(l => l.GeneratedApplicationId == id)
            .OrderByDescending(l => l.StartedAt)
            .Take(25)
            .ToListAsync();

        return Ok(new
        {
            application = app,
            agents,
            connectors,
            recentExecutionLogs = recentLogs
        });
    }

    [HttpGet("{id:guid}/ui-schema")]
    public async Task<IActionResult> UiSchema(Guid id)
    {
        var userId = _userManager.GetUserId(User);
        var app = await _db.GeneratedApplications
            .Where(a => a.Id == id)
            .Select(a => new { a.UiSchemaJson, a.OwnerUserId })
            .FirstOrDefaultAsync();

        if (app is null)
        {
            return NotFound();
        }

        if (app.OwnerUserId != userId && !User.IsInRole(nameof(PlatformRole.PlatformAdministrator)) &&
            !User.IsInRole(nameof(PlatformRole.ReadOnlyAuditor)))
        {
            return Forbid();
        }

        return Content(app.UiSchemaJson, "application/json");
    }
}
