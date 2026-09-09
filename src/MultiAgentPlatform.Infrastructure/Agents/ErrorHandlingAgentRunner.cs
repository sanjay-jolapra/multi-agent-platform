using MultiAgentPlatform.Domain.Entities;
using MultiAgentPlatform.Domain.Interfaces;

namespace MultiAgentPlatform.Infrastructure.Agents;

/// <summary>
/// Configurable extension point for app-specific error handling. The orchestrator already
/// records each stage's own failures in AgentExecutionLog/context.Trace, so this agent is a
/// best-effort no-op that always succeeds (the orchestrator never stops the pipeline on this
/// role's failure either way).
/// </summary>
public class ErrorHandlingAgentRunner : IAgent
{
    public ErrorHandlingAgentRunner(AgentDefinition definition)
    {
        Definition = definition;
    }

    public AgentDefinition Definition { get; }

    public Task<AgentResult> ExecuteAsync(AgentContext context, CancellationToken cancellationToken)
    {
        return Task.FromResult(AgentResult.Ok(context.Data));
    }
}
