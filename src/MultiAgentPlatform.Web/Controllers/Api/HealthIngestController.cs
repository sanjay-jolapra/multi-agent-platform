using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MultiAgentPlatform.Application.Dtos;
using MultiAgentPlatform.Application.Services;
using MultiAgentPlatform.Domain.Entities;
using MultiAgentPlatform.Infrastructure.Persistence;

namespace MultiAgentPlatform.Web.Controllers.Api;

// AllowAnonymous: generated applications call this endpoint from outside the platform's login
// session. In production this should be gated by a per-application shared secret/API key
// (e.g. validated against ConnectorDefinition.CredentialReferenceKey) rather than left open.
[ApiController]
[Route("api/v1/health")]
[AllowAnonymous]
public class HealthIngestController : ControllerBase
{
    private readonly MainDbContext _db;
    private readonly IHealthEvaluationService _healthEvaluationService;

    public HealthIngestController(MainDbContext db, IHealthEvaluationService healthEvaluationService)
    {
        _db = db;
        _healthEvaluationService = healthEvaluationService;
    }

    [HttpPost("heartbeat")]
    public async Task<IActionResult> Heartbeat([FromBody] HeartbeatPayloadDto dto)
    {
        var application = await _db.GeneratedApplications.FirstOrDefaultAsync(a => a.Id == dto.ApplicationId);

        if (application is null)
        {
            return NotFound();
        }

        var now = DateTimeOffset.UtcNow;
        var healthState = _healthEvaluationService.Evaluate(dto.Status, now, now);

        application.HealthState = healthState;
        application.LastHeartbeatAt = dto.Timestamp;
        application.UpdatedAt = now;

        _db.HealthCheckRecords.Add(new HealthCheckRecord
        {
            GeneratedApplicationId = application.Id,
            Timestamp = now,
            State = healthState,
            LatencyMs = dto.LatencyMs,
            ErrorRate = dto.ErrorRate,
            RawPayloadJson = System.Text.Json.JsonSerializer.Serialize(dto)
        });

        await _db.SaveChangesAsync();

        return Ok();
    }
}
