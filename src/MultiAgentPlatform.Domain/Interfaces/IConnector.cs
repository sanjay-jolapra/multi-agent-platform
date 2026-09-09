using MultiAgentPlatform.Domain.Entities;

namespace MultiAgentPlatform.Domain.Interfaces;

public class ConnectorContext
{
    public required Guid GeneratedApplicationId { get; init; }
    public required string CorrelationId { get; init; }
    public Dictionary<string, object?> Data { get; init; } = new();
}

public class ConnectorResult
{
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
    public int RecordsProcessed { get; init; }
    public Dictionary<string, object?> Output { get; init; } = new();

    public static ConnectorResult Ok(int records = 0, Dictionary<string, object?>? output = null) =>
        new() { Success = true, RecordsProcessed = records, Output = output ?? new() };

    public static ConnectorResult Fail(string error) => new() { Success = false, ErrorMessage = error };
}

/// <summary>
/// An input or output connector, implemented as a first-class running agent rather than a
/// static integration script. New connector types are added by implementing this interface
/// and registering with <see cref="IConnectorFactory"/> — no existing connector changes.
/// </summary>
public interface IConnector
{
    ConnectorDefinition Definition { get; }
    Task<ConnectorResult> ExecuteAsync(ConnectorContext context, CancellationToken cancellationToken);
    Task<bool> TestConnectionAsync(CancellationToken cancellationToken);
}

/// <summary>Builds a runnable <see cref="IConnector"/> for a given definition (Strategy + Factory).</summary>
public interface IConnectorFactory
{
    IConnector Create(ConnectorDefinition definition);
}
