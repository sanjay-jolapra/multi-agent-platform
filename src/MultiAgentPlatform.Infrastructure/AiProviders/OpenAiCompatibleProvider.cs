using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using MultiAgentPlatform.Application.Options;
using MultiAgentPlatform.Domain.Enums;
using MultiAgentPlatform.Domain.Interfaces;

namespace MultiAgentPlatform.Infrastructure.AiProviders;

/// <summary>OpenAI Chat Completions API client. Degrades gracefully (empty result) instead of throwing.</summary>
public class OpenAiCompatibleProvider : IAiProvider
{
    private readonly HttpClient _httpClient;
    private readonly OpenAiOptions _options;
    private readonly ILogger<OpenAiCompatibleProvider>? _logger;

    public OpenAiCompatibleProvider(HttpClient httpClient, OpenAiOptions options, ILogger<OpenAiCompatibleProvider>? logger = null)
    {
        _httpClient = httpClient;
        _options = options;
        _logger = logger;
    }

    public AiProviderKind Kind => AiProviderKind.OpenAi;

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
                messages = new[]
                {
                    new { role = "system", content = request.SystemPrompt },
                    new { role = "user", content = request.UserPrompt }
                },
                temperature = request.Temperature,
                max_tokens = request.MaxOutputTokens
            };

            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, $"{_options.BaseUrl.TrimEnd('/')}/chat/completions")
            {
                Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json")
            };
            httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);

            using var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger?.LogWarning("OpenAI completion failed with status {Status}: {Body}", response.StatusCode, json);
                return new AiCompletionResult { Content = string.Empty };
            }

            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            var content = root.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString() ?? string.Empty;

            int? promptTokens = null;
            int? completionTokens = null;
            if (root.TryGetProperty("usage", out var usage))
            {
                if (usage.TryGetProperty("prompt_tokens", out var pt)) promptTokens = pt.GetInt32();
                if (usage.TryGetProperty("completion_tokens", out var ct)) completionTokens = ct.GetInt32();
            }

            return new AiCompletionResult { Content = content, PromptTokens = promptTokens, CompletionTokens = completionTokens };
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "OpenAI completion call failed");
            return new AiCompletionResult { Content = string.Empty };
        }
    }
}
