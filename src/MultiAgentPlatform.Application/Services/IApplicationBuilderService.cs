using MultiAgentPlatform.Application.Wizard;
using MultiAgentPlatform.Domain.Entities;

namespace MultiAgentPlatform.Application.Services;

/// <summary>
/// Builder pattern: turns a completed wizard summary into a persisted <see cref="GeneratedApplication"/>
/// with a default agent pipeline (Orchestrator + one agent per configured input/output/rule) and
/// connector definitions inferred from the described input sources / output destinations.
/// </summary>
public interface IApplicationBuilderService
{
    GeneratedApplication BuildFromSummary(WizardSummary summary, string ownerUserId);
}
