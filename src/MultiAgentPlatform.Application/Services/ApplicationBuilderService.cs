using System.Text.Json;
using System.Text.RegularExpressions;
using MultiAgentPlatform.Application.Wizard;
using MultiAgentPlatform.Domain.Entities;
using MultiAgentPlatform.Domain.Enums;

namespace MultiAgentPlatform.Application.Services;

public class ApplicationBuilderService : IApplicationBuilderService
{
    public GeneratedApplication BuildFromSummary(WizardSummary summary, string ownerUserId)
    {
        var app = new GeneratedApplication
        {
            Name = summary.Name,
            Slug = Slugify(summary.Name),
            Description = summary.Purpose,
            OwnerUserId = ownerUserId,
            Status = ApplicationStatus.Ready,
            AiProviderKind = InferProviderKind(summary.AiProviderKey),
            ConfigJson = JsonSerializer.Serialize(summary),
            UiSchemaJson = JsonSerializer.Serialize(BuildUiSchema(summary))
        };

        var order = 0;
        app.Agents.Add(NewAgent(app.Id, "Orchestrator", AgentRole.Orchestrator,
            "Determine execution order and coordinate all other agents for this application's pipeline.", order++));

        app.Connectors.AddRange(BuildInputConnectors(app.Id, summary.InputSources));
        app.Agents.Add(NewAgent(app.Id, "Input Connector Agent", AgentRole.InputConnector,
            $"Pull data from the configured input source(s): {summary.InputSources}", order++));

        app.Agents.Add(NewAgent(app.Id, "Data Validation Agent", AgentRole.DataValidation,
            $"Validate incoming records against these business rules: {summary.BusinessRules}", order++));

        app.Agents.Add(NewAgent(app.Id, "Processing Agent", AgentRole.Processing,
            $"Execute the core business process: {summary.BusinessProcess}", order++));

        app.Agents.Add(NewAgent(app.Id, "Decision-Making Agent", AgentRole.DecisionMaking,
            "Decide the outcome/routing for each record based on validation and processing results.", order++));

        app.Connectors.AddRange(BuildOutputConnectors(app.Id, summary.OutputDestinations));
        app.Agents.Add(NewAgent(app.Id, "Output Connector Agent", AgentRole.OutputConnector,
            $"Push results to the configured output destination(s): {summary.OutputDestinations}", order++));

        app.Agents.Add(NewAgent(app.Id, "Error-Handling Agent", AgentRole.ErrorHandling,
            "Catch and log failures from any agent, apply retry policy, and escalate unresolved errors.", order++));

        app.Agents.Add(NewAgent(app.Id, "Monitoring Agent", AgentRole.Monitoring,
            $"Track execution health and report heartbeats per: {summary.HealthMonitoring}", order));

        return app;
    }

    private static AgentDefinition NewAgent(Guid appId, string name, AgentRole role, string instructions, int order) => new()
    {
        GeneratedApplicationId = appId,
        Name = name,
        Description = instructions,
        Role = role,
        SystemInstructions = instructions,
        ExecutionOrder = order
    };

    private static List<ConnectorDefinition> BuildInputConnectors(Guid appId, string inputSources)
    {
        var connectors = new List<ConnectorDefinition>();
        var text = inputSources.ToLowerInvariant();
        void Add(string name, ConnectorType type) => connectors.Add(new ConnectorDefinition
        {
            GeneratedApplicationId = appId,
            Name = name,
            Type = type,
            Direction = ConnectorDirection.Input
        });

        if (text.Contains("api")) Add("API Input", ConnectorType.Api);
        if (text.Contains("file") || text.Contains("upload")) Add("File Input", ConnectorType.File);
        if (text.Contains("database") || text.Contains("db")) Add("Database Input", ConnectorType.Database);
        if (text.Contains("webhook")) Add("Webhook Input", ConnectorType.Webhook);
        if (text.Contains("email") || text.Contains("exchange") || text.Contains("mail")) Add("Email Input", ConnectorType.Email);
        if (connectors.Count == 0 || text.Contains("manual")) Add("Manual Entry", ConnectorType.Manual);

        return connectors;
    }

    private static List<ConnectorDefinition> BuildOutputConnectors(Guid appId, string outputDestinations)
    {
        var connectors = new List<ConnectorDefinition>();
        var text = outputDestinations.ToLowerInvariant();
        void Add(string name, ConnectorType type) => connectors.Add(new ConnectorDefinition
        {
            GeneratedApplicationId = appId,
            Name = name,
            Type = type,
            Direction = ConnectorDirection.Output
        });

        if (text.Contains("api")) Add("API Output", ConnectorType.Api);
        if (text.Contains("file")) Add("File Output", ConnectorType.File);
        if (text.Contains("database") || text.Contains("db")) Add("Database Output", ConnectorType.Database);
        if (text.Contains("notif") || text.Contains("webhook")) Add("Webhook/Notification Output", ConnectorType.Webhook);
        if (connectors.Count == 0) Add("Database Output", ConnectorType.Database);

        return connectors;
    }

    private static AiProviderKind InferProviderKind(string aiProviderKey)
    {
        var text = aiProviderKey.ToLowerInvariant();
        if (text.Contains("azure")) return AiProviderKind.AzureOpenAi;
        if (text.Contains("anthropic") || text.Contains("claude")) return AiProviderKind.Anthropic;
        if (text.Contains("openai")) return AiProviderKind.OpenAi;
        return AiProviderKind.None;
    }

    private static object BuildUiSchema(WizardSummary summary) => new
    {
        title = summary.Name,
        pages = new[]
        {
            new
            {
                name = "Home",
                fields = new[]
                {
                    new { label = "Record ID", type = "text", name = "recordId" },
                    new { label = "Status", type = "select", name = "status", options = new[] { "Pending", "Approved", "Rejected" } },
                    new { label = "Notes", type = "textarea", name = "notes" }
                },
                actions = new[] { "Submit", "Approve", "Reject" }
            }
        }
    };

    private static string Slugify(string name)
    {
        var slug = Regex.Replace(name.Trim().ToLowerInvariant(), "[^a-z0-9]+", "-").Trim('-');
        return string.IsNullOrWhiteSpace(slug) ? $"app-{Guid.NewGuid():N}"[..12] : slug;
    }
}
