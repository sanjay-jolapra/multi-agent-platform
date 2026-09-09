using MultiAgentPlatform.Domain.Entities;
using MultiAgentPlatform.Domain.Enums;

namespace MultiAgentPlatform.Domain.Interfaces;

/// <summary>Shared execution context passed down the agent pipeline by the orchestrator.</summary>
public class AgentContext
{
    public required Guid GeneratedApplicationId { get; init; }
    public required string CorrelationId { get; init; }
    public Dictionary<string, object?> Data { get; init; } = new();
    public List<string> Trace { get; init; } = new();
}

public class AgentResult
{
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
    public Dictionary<string, object?> Output { get; init; } = new();

    public static AgentResult Ok(Dictionary<string, object?>? output = null) => new() { Success = true, Output = output ?? new() };
    public static AgentResult Fail(string error) => new() { Success = false, ErrorMessage = error };
}

/// <summary>
/// Every generated-application agent (orchestrator, connector, validation, processing, etc.)
/// implements this. Concrete behavior is driven by <see cref="AgentDefinition"/> so new agent
/// "types" can be added without changing the orchestrator or existing agents.
/// </summary>
public interface IAgent
{
    AgentDefinition Definition { get; }
    Task<AgentResult> ExecuteAsync(AgentContext context, CancellationToken cancellationToken);
}

/// <summary>Builds a runnable <see cref="IAgent"/> for a given definition (Factory pattern).</summary>
public interface IAgentFactory
{
    IAgent Create(AgentDefinition definition, IAiProvider aiProvider);
}

/// <summary>Runs an application's agent pipeline in the order decided by the Orchestrator agent.</summary>
public interface IAgentOrchestrator
{
    Task<AgentResult> RunPipelineAsync(GeneratedApplication application, Dictionary<string, object?> triggerPayload, CancellationToken cancellationToken);
}
