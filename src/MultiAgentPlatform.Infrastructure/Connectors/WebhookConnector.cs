using System.Text;
using System.Text.Json;
using MultiAgentPlatform.Domain.Entities;
using MultiAgentPlatform.Domain.Enums;
using MultiAgentPlatform.Domain.Interfaces;

namespace MultiAgentPlatform.Infrastructure.Connectors;

public class WebhookConnector : IConnector
{
    private readonly IHttpClientFactory _httpClientFactory;

    public WebhookConnector(ConnectorDefinition definition, IHttpClientFactory httpClientFactory)
    {
        Definition = definition;
        _httpClientFactory = httpClientFactory;
    }

    public ConnectorDefinition Definition { get; }

    private string? ParseUrl()
    {
        try
        {
            using var doc = JsonDocument.Parse(Definition.ConfigJson);
            return doc.RootElement.TryGetProperty("url", out var u) ? u.GetString() : null;
        }
        catch
        {
            return null;
        }
    }

    public async Task<ConnectorResult> ExecuteAsync(ConnectorContext context, CancellationToken cancellationToken)
    {
        if (Definition.Direction == ConnectorDirection.Input)
        {
            return ConnectorResult.Fail("Webhook connectors are output-only");
        }

        var url = ParseUrl();
        if (string.IsNullOrWhiteSpace(url))
        {
            return ConnectorResult.Fail("WebhookConnector: missing or malformed 'url' in ConfigJson.");
        }

        try
        {
            var client = _httpClientFactory.CreateClient();
            var json = JsonSerializer.Serialize(context.Data);
            using var response = await client.PostAsync(url, new StringContent(json, Encoding.UTF8, "application/json"), cancellationToken);
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return ConnectorResult.Fail($"WebhookConnector: POST failed with status {response.StatusCode}: {responseBody}");
            }

            return ConnectorResult.Ok(1, new Dictionary<string, object?> { ["response"] = responseBody });
        }
        catch (Exception ex)
        {
            return ConnectorResult.Fail($"WebhookConnector: {ex.Message}");
        }
    }

    public async Task<bool> TestConnectionAsync(CancellationToken cancellationToken)
    {
        var url = ParseUrl();
        if (string.IsNullOrWhiteSpace(url))
        {
            return false;
        }

        try
        {
            var client = _httpClientFactory.CreateClient();
            using var request = new HttpRequestMessage(HttpMethod.Head, url);
            using var response = await client.SendAsync(request, cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }
}
