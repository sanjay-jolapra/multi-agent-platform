namespace SampleInvoiceProcessor.Agents;

/// <summary>Shared execution context passed down the pipeline, mirroring the shape of the
/// main platform's MultiAgentPlatform.Domain.Interfaces.AgentContext.</summary>
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
/// Minimal local stand-in for the main platform's IAgent. This sample keeps its own tiny
/// copy instead of referencing MultiAgentPlatform.Domain so the generated app has zero
/// compile-time or runtime coupling to the platform - exactly what a real generated app
/// would look like once handed off.
/// </summary>
public interface IPipelineAgent
{
    string Name { get; }
    Task<AgentResult> ExecuteAsync(AgentContext context, CancellationToken cancellationToken);
}
