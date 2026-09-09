using MultiAgentPlatform.Domain.Enums;

namespace MultiAgentPlatform.Domain.Interfaces;

public class AiCompletionRequest
{
    public string SystemPrompt { get; init; } = string.Empty;
    public string UserPrompt { get; init; } = string.Empty;
    public double Temperature { get; init; } = 0.2;
    public int MaxOutputTokens { get; init; } = 800;
}

public class AiCompletionResult
{
    public string Content { get; init; } = string.Empty;
    public int? PromptTokens { get; init; }
    public int? CompletionTokens { get; init; }
}

/// <summary>
/// Abstraction over any LLM backend so the wizard and generated-app agents are never coupled
/// to one AI vendor. Concrete providers (OpenAI, Azure OpenAI, Anthropic, ...) live in
/// Infrastructure and read their API keys from configuration/environment — never hard-coded.
/// </summary>
public interface IAiProvider
{
    AiProviderKind Kind { get; }
    Task<AiCompletionResult> CompleteAsync(AiCompletionRequest request, CancellationToken cancellationToken);
}

/// <summary>Resolves the configured <see cref="IAiProvider"/> for a given provider kind (Strategy pattern).</summary>
public interface IAiProviderFactory
{
    IAiProvider Create(AiProviderKind kind);
}
