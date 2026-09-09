using MultiAgentPlatform.Domain.Entities;
using MultiAgentPlatform.Domain.Enums;
using MultiAgentPlatform.Domain.Interfaces;

namespace MultiAgentPlatform.Infrastructure.Connectors;

public class ConnectorFactory : IConnectorFactory
{
    private readonly IHttpClientFactory _httpClientFactory;

    public ConnectorFactory(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public IConnector Create(ConnectorDefinition definition) => definition.Type switch
    {
        ConnectorType.Api => new ApiConnector(definition, _httpClientFactory),
        ConnectorType.File => new FileConnector(definition),
        ConnectorType.Database => new DatabaseConnector(definition),
        ConnectorType.Webhook => new WebhookConnector(definition, _httpClientFactory),
        ConnectorType.Manual => new ManualEntryConnector(definition),
        // Email connectors are not yet implemented as a distinct transport; fall back to a
        // safe no-op so a misconfigured/unsupported type never crashes the pipeline.
        ConnectorType.Email => new ManualEntryConnector(definition),
        _ => new ManualEntryConnector(definition)
    };
}
