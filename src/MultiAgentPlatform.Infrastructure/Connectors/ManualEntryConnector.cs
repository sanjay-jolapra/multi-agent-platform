using MultiAgentPlatform.Domain.Entities;
using MultiAgentPlatform.Domain.Interfaces;

namespace MultiAgentPlatform.Infrastructure.Connectors;

/// <summary>No external I/O — represents data entered directly by a user through the generated app's UI.</summary>
public class ManualEntryConnector : IConnector
{
    public ManualEntryConnector(ConnectorDefinition definition)
    {
        Definition = definition;
    }

    public ConnectorDefinition Definition { get; }

    public Task<ConnectorResult> ExecuteAsync(ConnectorContext context, CancellationToken cancellationToken)
    {
        return Task.FromResult(ConnectorResult.Ok(1, context.Data));
    }

    public Task<bool> TestConnectionAsync(CancellationToken cancellationToken)
    {
        return Task.FromResult(true);
    }
}
