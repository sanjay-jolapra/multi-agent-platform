using MultiAgentPlatform.Domain.Entities;
using MultiAgentPlatform.Domain.Interfaces;

namespace MultiAgentPlatform.Infrastructure.Agents;

public class DecisionMakingAgentRunner : IAgent
{
    public DecisionMakingAgentRunner(AgentDefinition definition)
    {
        Definition = definition;
    }

    public AgentDefinition Definition { get; }

    public Task<AgentResult> ExecuteAsync(AgentContext context, CancellationToken cancellationToken)
    {
        var output = new Dictionary<string, object?>(context.Data);
        output["decision"] = context.Data.TryGetValue("status", out var status) && status is not null
            ? status
            : "approved";

        return Task.FromResult(AgentResult.Ok(output));
    }
}
