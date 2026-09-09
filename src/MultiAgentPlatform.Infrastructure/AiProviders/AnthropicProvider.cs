using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using MultiAgentPlatform.Application.Options;
using MultiAgentPlatform.Domain.Enums;
using MultiAgentPlatform.Domain.Interfaces;

namespace MultiAgentPlatform.Infrastructure.AiProviders;

/// <summary>Anthropic Messages API client. Degrades gracefully (empty result) instead of throwing.</summary>
public class AnthropicProvider : IAiProvider
{
    private readonly HttpClient _httpClient;
    private readonly AnthropicOptions _options;
    private readonly ILogger<AnthropicProvider>? _logger;

    public AnthropicProvider(HttpClient httpClient, AnthropicOptions options, ILogger<AnthropicProvider>? logger = null)
    {
        _httpClient = httpClient;
        _options = options;
        _logger = logger;
    }

    public AiProviderKind Kind => AiProviderKind.Anthropic;

    public async Task<AiCompletionResult> CompleteAsync(AiCompletionRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            return new AiCompletionResult { Content = string.Empty };
        }

        try
        {
            var body = new
            {
                model = _options.Model,
                max_tokens = request.MaxOutputTokens,
                system = request.SystemPrompt,
                messages = new[]
                {
                    new { role = "user", content = request.UserPrompt }
                }
            };

            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, $"{_options.BaseUrl.TrimEnd('/')}/messages")
            {
                Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json")
            };
            httpRequest.Headers.Add("x-api-key", _options.ApiKey);
            httpRequest.Headers.Add("anthropic-version", "2023-06-01");

            using var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger?.LogWarning("Anthropic completion failed with status {Status}: {Body}", response.StatusCode, json);
                return new AiCompletionResult { Content = string.Empty };
            }

            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            var content = root.GetProperty("content")[0].GetProperty("text").GetString() ?? string.Empty;

            int? promptTokens = null;
            int? completionTokens = null;
            if (root.TryGetProperty("usage", out var usage))
            {
                if (usage.TryGetProperty("input_tokens", out var it)) promptTokens = it.GetInt32();
                if (usage.TryGetProperty("output_tokens", out var ot)) completionTokens = ot.GetInt32();
            }

            return new AiCompletionResult { Content = content, PromptTokens = promptTokens, CompletionTokens = completionTokens };
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Anthropic completion call failed");
            return new AiCompletionResult { Content = string.Empty };
        }
    }
}
