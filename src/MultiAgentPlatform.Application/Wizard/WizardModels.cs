namespace MultiAgentPlatform.Application.Wizard;

/// <summary>
/// One topic collected by the in-chat wizard before an application can be generated.
/// Order here is the order questions are asked, one topic at a time.
/// </summary>
public enum WizardTopic
{
    NamePurpose,
    BusinessProcess,
    InputSources,
    OutputDestinations,
    BusinessRules,
    AgentsResponsibilities,
    UsersRoles,
    DeploymentSettings,
    HealthMonitoring,
    UiRequirements,
    AiProviderKey,
    ReviewAndConfirm
}

public static class WizardTopics
{
    public static readonly WizardTopic[] Ordered =
    {
        WizardTopic.NamePurpose,
        WizardTopic.BusinessProcess,
        WizardTopic.InputSources,
        WizardTopic.OutputDestinations,
        WizardTopic.BusinessRules,
        WizardTopic.AgentsResponsibilities,
        WizardTopic.UsersRoles,
        WizardTopic.DeploymentSettings,
        WizardTopic.HealthMonitoring,
        WizardTopic.UiRequirements,
        WizardTopic.AiProviderKey,
        WizardTopic.ReviewAndConfirm
    };

    public static string QuestionFor(WizardTopic topic) => topic switch
    {
        WizardTopic.NamePurpose => "What should we call this application, and in one or two sentences, what business objective is it for?",
        WizardTopic.BusinessProcess => "Walk me through the business process step by step — what happens from start to finish?",
        WizardTopic.InputSources => "Where does the data come from? (e.g. an API, uploaded files, a database, a webhook, an email inbox, or manual entry)",
        WizardTopic.OutputDestinations => "Where should results go once processed? (e.g. an API, a database, a file, or a notification)",
        WizardTopic.BusinessRules => "Are there any business rules or validation the data must satisfy (required fields, thresholds, approval rules, security requirements)?",
        WizardTopic.AgentsResponsibilities => "Which distinct steps/agents should handle this (e.g. validation, decision-making, error-handling)? Describe each one's responsibility, or say 'use the default pipeline'.",
        WizardTopic.UsersRoles => "Who will use this application, and what roles do they need (e.g. owner, developer, regular user, auditor)?",
        WizardTopic.DeploymentSettings => "Any deployment preferences — a specific subdomain/slug, expected load, or is the default single-container Docker setup fine?",
        WizardTopic.HealthMonitoring => "How often should this app report its health/heartbeat to the platform, and what should count as degraded vs unhealthy?",
        WizardTopic.UiRequirements => "What should the UI look like — a simple form, a dashboard with a table, both? Any specific fields or actions you need visible?",
        WizardTopic.AiProviderKey => "Which AI provider should power this application's agents (OpenAI, Azure OpenAI, or Anthropic), and do you have an API key configured already, or would you like to add one now?",
        WizardTopic.ReviewAndConfirm => "Here's what I've understood — please review and reply 'confirm' to generate the application, or tell me what to change.",
        _ => "Could you tell me more?"
    };
}

public class WizardState
{
    public Dictionary<WizardTopic, string> Answers { get; set; } = new();
    public bool Confirmed { get; set; }
    public bool Completed { get; set; }

    public WizardTopic? NextUnansweredTopic()
    {
        foreach (var topic in WizardTopics.Ordered)
        {
            if (!Answers.ContainsKey(topic))
            {
                return topic;
            }
        }

        return null;
    }

    public bool AllTopicsAnswered() => WizardTopics.Ordered
        .Where(t => t != WizardTopic.ReviewAndConfirm)
        .All(Answers.ContainsKey);
}

/// <summary>Snapshot of a fully-answered wizard, used to build the GeneratedApplication + its default agents/connectors.</summary>
public record WizardSummary(
    string Name,
    string Purpose,
    string BusinessProcess,
    string InputSources,
    string OutputDestinations,
    string BusinessRules,
    string AgentsResponsibilities,
    string UsersRoles,
    string DeploymentSettings,
    string HealthMonitoring,
    string UiRequirements,
    string AiProviderKey);
