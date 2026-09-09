using Microsoft.Extensions.Options;
using MultiAgentPlatform.Application.Options;
using MultiAgentPlatform.Domain.Enums;
using MultiAgentPlatform.Domain.Interfaces;

namespace MultiAgentPlatform.Infrastructure.AiProviders;

public class AiProviderFactory : IAiProviderFactory
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly AiProvidersOptions _options;

    public AiProviderFactory(IHttpClientFactory httpClientFactory, IOptions<AiProvidersOptions> options)
    {
        _httpClientFactory = httpClientFactory;
        _options = options.Value;
    }

    public IAiProvider Create(AiProviderKind kind) => kind switch
    {
        AiProviderKind.OpenAi => new OpenAiCompatibleProvider(_httpClientFactory.CreateClient(), _options.OpenAi),
        AiProviderKind.AzureOpenAi => new AzureOpenAiProvider(_httpClientFactory.CreateClient(), _options.AzureOpenAi),
        AiProviderKind.Anthropic => new AnthropicProvider(_httpClientFactory.CreateClient(), _options.Anthropic),
        _ => new NullAiProvider()
    };
}
