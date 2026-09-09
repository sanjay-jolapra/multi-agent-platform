using System.Text;
using System.Text.Json;
using MultiAgentPlatform.Domain.Entities;
using MultiAgentPlatform.Domain.Enums;
using MultiAgentPlatform.Domain.Interfaces;

namespace MultiAgentPlatform.Infrastructure.Connectors;

public class ApiConnector : IConnector
{
    private readonly IHttpClientFactory _httpClientFactory;

    public ApiConnector(ConnectorDefinition definition, IHttpClientFactory httpClientFactory)
    {
        Definition = definition;
        _httpClientFactory = httpClientFactory;
    }

    public ConnectorDefinition Definition { get; }

    private record ApiConfig(string? Url, string Method);

    private ApiConfig? ParseConfig()
    {
        try
        {
            using var doc = JsonDocument.Parse(Definition.ConfigJson);
            var root = doc.RootElement;
            var url = root.TryGetProperty("url", out var u) ? u.GetString() : null;
            var method = root.TryGetProperty("method", out var m) ? m.GetString() ?? "GET" : "GET";
            return new ApiConfig(url, method);
        }
        catch
        {
            return null;
        }
    }

    public async Task<ConnectorResult> ExecuteAsync(ConnectorContext context, CancellationToken cancellationToken)
    {
        var config = ParseConfig();
        if (config is null || string.IsNullOrWhiteSpace(config.Url))
        {
            return ConnectorResult.Fail("ApiConnector: missing or malformed 'url' in ConfigJson.");
        }

        try
        {
            var client = _httpClientFactory.CreateClient();

            if (Definition.Direction == ConnectorDirection.Input)
            {
                var body = await client.GetStringAsync(config.Url, cancellationToken);
                return ConnectorResult.Ok(1, new Dictionary<string, object?> { ["payload"] = body });
            }

            var json = JsonSerializer.Serialize(context.Data);
            using var response = await client.PostAsync(config.Url, new StringContent(json, Encoding.UTF8, "application/json"), cancellationToken);
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return ConnectorResult.Fail($"ApiConnector: output POST failed with status {response.StatusCode}: {responseBody}");
            }

            return ConnectorResult.Ok(1, new Dictionary<string, object?> { ["response"] = responseBody });
        }
        catch (Exception ex)
        {
            return ConnectorResult.Fail($"ApiConnector: {ex.Message}");
        }
    }

    public async Task<bool> TestConnectionAsync(CancellationToken cancellationToken)
    {
        var config = ParseConfig();
        if (config is null || string.IsNullOrWhiteSpace(config.Url))
        {
            return false;
        }

        try
        {
            var client = _httpClientFactory.CreateClient();
            using var request = new HttpRequestMessage(HttpMethod.Get, config.Url);
            using var response = await client.SendAsync(request, cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }
}
