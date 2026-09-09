using MultiAgentPlatform.Domain.Entities;
using MultiAgentPlatform.Domain.Interfaces;

namespace MultiAgentPlatform.Infrastructure.Agents;

public class MonitoringAgentRunner : IAgent
{
    public MonitoringAgentRunner(AgentDefinition definition)
    {
        Definition = definition;
    }

    public AgentDefinition Definition { get; }

    public Task<AgentResult> ExecuteAsync(AgentContext context, CancellationToken cancellationToken)
    {
        var output = new Dictionary<string, object?>(context.Data)
        {
            ["monitoredAt"] = DateTimeOffset.UtcNow
        };

        return Task.FromResult(AgentResult.Ok(output));
    }
}
