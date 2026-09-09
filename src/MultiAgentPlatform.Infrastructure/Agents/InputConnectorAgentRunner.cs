using MultiAgentPlatform.Domain.Entities;
using MultiAgentPlatform.Domain.Enums;
using MultiAgentPlatform.Domain.Interfaces;

namespace MultiAgentPlatform.Infrastructure.Agents;

public class InputConnectorAgentRunner : IAgent
{
    private readonly IEnumerable<ConnectorDefinition> _connectors;
    private readonly IConnectorFactory _connectorFactory;

    public InputConnectorAgentRunner(AgentDefinition definition, IEnumerable<ConnectorDefinition> connectors, IConnectorFactory connectorFactory)
    {
        Definition = definition;
        _connectors = connectors;
        _connectorFactory = connectorFactory;
    }

    public AgentDefinition Definition { get; }

    public async Task<AgentResult> ExecuteAsync(AgentContext context, CancellationToken cancellationToken)
    {
        var relevant = _connectors.Where(c => c.IsEnabled && c.Direction == ConnectorDirection.Input).ToList();
        if (relevant.Count == 0)
        {
            return AgentResult.Ok(context.Data);
        }

        var output = new Dictionary<string, object?>();
        var errors = new List<string>();
        var successCount = 0;

        foreach (var connectorDefinition in relevant)
        {
            var connector = _connectorFactory.Create(connectorDefinition);
            var connectorContext = new ConnectorContext
            {
                GeneratedApplicationId = context.GeneratedApplicationId,
                CorrelationId = context.CorrelationId,
                Data = context.Data
            };

            var result = await connector.ExecuteAsync(connectorContext, cancellationToken);
            if (result.Success)
            {
                successCount++;
                foreach (var (key, value) in result.Output)
                {
                    output[key] = value;
                    context.Data[key] = value;
                }
            }
            else
            {
                errors.Add($"{connectorDefinition.Name}: {result.ErrorMessage}");
            }
        }

        if (errors.Count > 0)
        {
            output["connectorErrors"] = errors;
        }

        if (successCount == 0)
        {
            return AgentResult.Fail($"All input connectors failed: {string.Join("; ", errors)}");
        }

        return AgentResult.Ok(output);
    }
}
