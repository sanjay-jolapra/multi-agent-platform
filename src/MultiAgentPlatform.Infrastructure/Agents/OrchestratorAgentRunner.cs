using MultiAgentPlatform.Domain.Entities;
using MultiAgentPlatform.Domain.Interfaces;

namespace MultiAgentPlatform.Infrastructure.Agents;

/// <summary>
/// The Orchestrator role's actual scheduling/ordering logic lives in
/// <see cref="MultiAgentPlatform.Infrastructure.Orchestration.AgentOrchestrator"/>; as a pipeline
/// step it is a simple pass-through.
/// </summary>
public class OrchestratorAgentRunner : IAgent
{
    public OrchestratorAgentRunner(AgentDefinition definition)
    {
        Definition = definition;
    }

    public AgentDefinition Definition { get; }

    public Task<AgentResult> ExecuteAsync(AgentContext context, CancellationToken cancellationToken)
    {
        return Task.FromResult(AgentResult.Ok(context.Data));
    }
}
