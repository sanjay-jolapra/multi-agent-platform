using MultiAgentPlatform.Domain.Enums;
using MultiAgentPlatform.Domain.Interfaces;

namespace MultiAgentPlatform.Infrastructure.AiProviders;

/// <summary>Safe fallback used when no AI provider/key is configured. Never performs network I/O.</summary>
public class NullAiProvider : IAiProvider
{
    public AiProviderKind Kind => AiProviderKind.None;

    public Task<AiCompletionResult> CompleteAsync(AiCompletionRequest request, CancellationToken cancellationToken)
    {
        return Task.FromResult(new AiCompletionResult { Content = string.Empty });
    }
}
