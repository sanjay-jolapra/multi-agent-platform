using MultiAgentPlatform.Domain.Entities;
using MultiAgentPlatform.Domain.Enums;
using MultiAgentPlatform.Domain.Interfaces;

namespace MultiAgentPlatform.Infrastructure.Agents;

public class AgentFactory : IAgentFactory
{
    private readonly IConnectorFactory _connectorFactory;

    public AgentFactory(IConnectorFactory connectorFactory)
    {
        _connectorFactory = connectorFactory;
    }

    /// <summary>Plain interface method: no sibling connectors known, so connector-role agents see none configured.</summary>
    public IAgent Create(AgentDefinition definition, IAiProvider aiProvider) =>
        Create(definition, aiProvider, Array.Empty<ConnectorDefinition>());

    /// <summary>
    /// Preferred overload used by the orchestrator, which knows the owning application's full
    /// connector list and can hand the relevant subset to Input/Output connector agents.
    /// </summary>
    public IAgent Create(AgentDefinition definition, IAiProvider aiProvider, IReadOnlyList<ConnectorDefinition> applicationConnectors) =>
        definition.Role switch
        {
            AgentRole.Orchestrator => new OrchestratorAgentRunner(definition),
            AgentRole.InputConnector => new InputConnectorAgentRunner(definition, applicationConnectors, _connectorFactory),
            AgentRole.DataValidation => new DataValidationAgentRunner(definition),
            AgentRole.Processing => new ProcessingAgentRunner(definition),
            AgentRole.DecisionMaking => new DecisionMakingAgentRunner(definition),
            AgentRole.OutputConnector => new OutputConnectorAgentRunner(definition, applicationConnectors, _connectorFactory),
            AgentRole.ErrorHandling => new ErrorHandlingAgentRunner(definition),
            AgentRole.Monitoring => new MonitoringAgentRunner(definition),
            _ => throw new NotSupportedException($"Unsupported agent role: {definition.Role}")
        };
}
