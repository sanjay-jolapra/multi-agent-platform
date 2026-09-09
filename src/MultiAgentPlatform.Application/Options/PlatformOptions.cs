namespace MultiAgentPlatform.Application.Options;

/// <summary>Bound from configuration section "AiProviders" (appsettings / environment / user-secrets). No keys are ever hard-coded.</summary>
public class AiProvidersOptions
{
    public const string SectionName = "AiProviders";

    public OpenAiOptions OpenAi { get; set; } = new();
    public AzureOpenAiOptions AzureOpenAi { get; set; } = new();
    public AnthropicOptions Anthropic { get; set; } = new();
}

public class OpenAiOptions
{
    public string? ApiKey { get; set; }
    public string Model { get; set; } = "gpt-4o-mini";
    public string BaseUrl { get; set; } = "https://api.openai.com/v1";
}

public class AzureOpenAiOptions
{
    public string? ApiKey { get; set; }
    public string? Endpoint { get; set; }
    public string? Deployment { get; set; }
}

public class AnthropicOptions
{
    public string? ApiKey { get; set; }
    public string Model { get; set; } = "claude-sonnet-5";
    public string BaseUrl { get; set; } = "https://api.anthropic.com/v1";
}

/// <summary>Bound from configuration section "HealthMonitoring".</summary>
public class HealthMonitoringOptions
{
    public const string SectionName = "HealthMonitoring";

    public int DegradedAfterMissedHeartbeats { get; set; } = 2;
    public int OfflineAfterMissedHeartbeats { get; set; } = 5;
    public int ExpectedHeartbeatIntervalSeconds { get; set; } = 30;
}

/// <summary>Bound from configuration section "Deployment".</summary>
public class DeploymentOptions
{
    public const string SectionName = "Deployment";

    public string BaseDomain { get; set; } = "localhost";
    public int PortRangeStart { get; set; } = 6000;
    public int PortRangeEnd { get; set; } = 6999;
}
