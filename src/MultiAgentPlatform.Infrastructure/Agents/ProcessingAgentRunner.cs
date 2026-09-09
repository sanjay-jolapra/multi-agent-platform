using MultiAgentPlatform.Domain.Entities;
using MultiAgentPlatform.Domain.Interfaces;

namespace MultiAgentPlatform.Infrastructure.Agents;

/// <summary>
/// Generic placeholder: real business-process logic is application-specific and described only
/// in Definition.SystemInstructions (natural language produced by the wizard), so there is no
/// generic way to execute it here. This copies data through and stamps a processing timestamp.
/// </summary>
public class ProcessingAgentRunner : IAgent
{
    public ProcessingAgentRunner(AgentDefinition definition)
    {
        Definition = definition;
    }

    public AgentDefinition Definition { get; }

    public Task<AgentResult> ExecuteAsync(AgentContext context, CancellationToken cancellationToken)
    {
        var output = new Dictionary<string, object?>(context.Data)
        {
            ["processedAt"] = DateTimeOffset.UtcNow
        };

        return Task.FromResult(AgentResult.Ok(output));
    }
}
