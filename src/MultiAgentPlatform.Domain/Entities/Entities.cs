using MultiAgentPlatform.Domain.Enums;

namespace MultiAgentPlatform.Domain.Entities;

public class GeneratedApplication
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string OwnerUserId { get; set; } = string.Empty;
    public ApplicationStatus Status { get; set; } = ApplicationStatus.Draft;
    public HealthState HealthState { get; set; } = HealthState.Unknown;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    public int? DeploymentPort { get; set; }
    public string Version { get; set; } = "1.0.0";
    public DateTimeOffset? LastHeartbeatAt { get; set; }
    public DateTimeOffset? LastSuccessfulExecutionAt { get; set; }
    public DateTimeOffset? LastFailedExecutionAt { get; set; }

    /// <summary>Serialized WizardSummary/config JSON: business process, rules, users, deployment/health settings.</summary>
    public string ConfigJson { get; set; } = "{}";

    /// <summary>Schema-driven UI definition (Bootstrap-rendered, no code execution) shown in the live preview.</summary>
    public string UiSchemaJson { get; set; } = "{}";

    /// <summary>Which configured AI provider this app's agents use.</summary>
    public AiProviderKind AiProviderKind { get; set; } = AiProviderKind.None;

    public List<AgentDefinition> Agents { get; set; } = new();
    public List<ConnectorDefinition> Connectors { get; set; } = new();
}

public class AgentDefinition
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid GeneratedApplicationId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public AgentRole Role { get; set; }
    public string SystemInstructions { get; set; } = string.Empty;
    public string InputContractJson { get; set; } = "{}";
    public string OutputContractJson { get; set; } = "{}";
    public string ToolsJson { get; set; } = "[]";
    public int TimeoutSeconds { get; set; } = 30;
    public int MaxRetries { get; set; } = 2;
    public int RetryBackoffSeconds { get; set; } = 2;
    public int ExecutionOrder { get; set; }
    public bool IsEnabled { get; set; } = true;
    public ExecutionStatus LastExecutionStatus { get; set; } = ExecutionStatus.Pending;
}

public class ConnectorDefinition
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid GeneratedApplicationId { get; set; }
    public string Name { get; set; } = string.Empty;
    public ConnectorType Type { get; set; }
    public ConnectorDirection Direction { get; set; }
    public string ConfigJson { get; set; } = "{}";
    public string? CredentialReferenceKey { get; set; }
    public string FieldMappingJson { get; set; } = "{}";
    public int TimeoutSeconds { get; set; } = 30;
    public int MaxRetries { get; set; } = 2;
    public bool IsEnabled { get; set; } = true;
}

public class AgentExecutionLog
{
    public long Id { get; set; }
    public Guid AgentDefinitionId { get; set; }
    public Guid GeneratedApplicationId { get; set; }
    public string CorrelationId { get; set; } = string.Empty;
    public DateTimeOffset StartedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? CompletedAt { get; set; }
    public ExecutionStatus Status { get; set; } = ExecutionStatus.Running;
    public string? InputSummary { get; set; }
    public string? OutputSummary { get; set; }
    public string? ErrorMessage { get; set; }
}

public class ConnectorExecutionLog
{
    public long Id { get; set; }
    public Guid ConnectorDefinitionId { get; set; }
    public Guid GeneratedApplicationId { get; set; }
    public string CorrelationId { get; set; } = string.Empty;
    public DateTimeOffset StartedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? CompletedAt { get; set; }
    public ExecutionStatus Status { get; set; } = ExecutionStatus.Running;
    public string? RecordsProcessed { get; set; }
    public string? ErrorMessage { get; set; }
}

public class Conversation
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string UserId { get; set; } = string.Empty;
    public Guid? GeneratedApplicationId { get; set; }
    public string Title { get; set; } = "New application";
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>Serialized WizardState so the multi-step wizard can resume across requests.</summary>
    public string WizardStateJson { get; set; } = "{}";

    public List<ChatMessage> Messages { get; set; } = new();
}

public class ChatMessage
{
    public long Id { get; set; }
    public Guid ConversationId { get; set; }
    public SenderType SenderType { get; set; }
    public string Content { get; set; } = string.Empty;
    public int? PromptTokens { get; set; }
    public int? CompletionTokens { get; set; }
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
    public ExecutionStatus Status { get; set; } = ExecutionStatus.Success;
    public string? ErrorDetails { get; set; }
    public string CorrelationId { get; set; } = string.Empty;
}

public class HealthCheckRecord
{
    public long Id { get; set; }
    public Guid GeneratedApplicationId { get; set; }
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
    public HealthState State { get; set; }
    public double LatencyMs { get; set; }
    public double ErrorRate { get; set; }
    public string RawPayloadJson { get; set; } = "{}";
}

public class DeploymentLog
{
    public long Id { get; set; }
    public Guid GeneratedApplicationId { get; set; }
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
    public DeploymentAction Action { get; set; }
    public string Details { get; set; } = string.Empty;
}

public class AuditLogEntry
{
    public long Id { get; set; }
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
    public string? UserId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string? EntityType { get; set; }
    public string? EntityId { get; set; }
    public string? DetailsJson { get; set; }
    public string CorrelationId { get; set; } = string.Empty;
    public string LogLevel { get; set; } = "Information";
}
