namespace MultiAgentPlatform.Domain.Enums;

public enum ApplicationStatus
{
    Draft,
    Configuring,
    Ready,
    Deploying,
    Running,
    Stopped,
    Failed
}

public enum HealthState
{
    Unknown,
    Healthy,
    Degraded,
    Unhealthy,
    Offline
}

public enum AgentRole
{
    Orchestrator,
    InputConnector,
    DataValidation,
    Processing,
    DecisionMaking,
    OutputConnector,
    ErrorHandling,
    Monitoring
}

public enum ExecutionStatus
{
    Pending,
    Running,
    Success,
    Failed,
    TimedOut,
    Skipped
}

public enum ConnectorType
{
    Api,
    File,
    Database,
    Webhook,
    Email,
    Manual
}

public enum ConnectorDirection
{
    Input,
    Output
}

public enum SenderType
{
    User,
    Assistant,
    System
}

public enum DeploymentAction
{
    Created,
    ConfigUpdated,
    Deployed,
    Started,
    Stopped,
    Restarted,
    Failed
}

public enum PlatformRole
{
    PlatformAdministrator,
    ApplicationOwner,
    ApplicationDeveloper,
    ApplicationUser,
    ReadOnlyAuditor
}

public enum AiProviderKind
{
    None,
    OpenAi,
    AzureOpenAi,
    Anthropic
}
