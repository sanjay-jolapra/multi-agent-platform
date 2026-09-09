using System.Text;
using MultiAgentPlatform.Domain.Enums;
using MultiAgentPlatform.Domain.Interfaces;

namespace MultiAgentPlatform.Application.Wizard;

/// <summary>
/// Default implementation. When an AI provider is configured it is used to normalize each
/// answer (e.g. turning a rambling description of inputs into a short structured line); when
/// none is configured yet (the platform has no key, or the user hasn't picked one), the raw
/// answer is stored as-is so the wizard still makes progress without ever hard-failing.
/// </summary>
public class WizardService : IWizardService
{
    private readonly IAiProviderFactory _aiProviderFactory;
    private readonly AiProviderKind _defaultProviderKind;

    public WizardService(IAiProviderFactory aiProviderFactory, AiProviderKind defaultProviderKind = AiProviderKind.None)
    {
        _aiProviderFactory = aiProviderFactory;
        _defaultProviderKind = defaultProviderKind;
    }

    public async Task<WizardTurnResult> ProcessUserMessageAsync(WizardState currentState, string userMessage, CancellationToken cancellationToken)
    {
        var topic = currentState.NextUnansweredTopic();

        if (topic is null)
        {
            currentState.Completed = true;
            return new WizardTurnResult("This application has already been fully configured.", currentState, true, RenderSummary(currentState));
        }

        if (topic == WizardTopic.ReviewAndConfirm)
        {
            var confirmed = userMessage.Trim().ToLowerInvariant() is "confirm" or "yes" or "looks good" or "generate";
            if (confirmed)
            {
                currentState.Answers[WizardTopic.ReviewAndConfirm] = "confirmed";
                currentState.Confirmed = true;
                currentState.Completed = true;
                return new WizardTurnResult(
                    "Great — generating the application now with the default agent/connector pipeline. You can refine agents and connectors afterwards.",
                    currentState, true, RenderSummary(currentState));
            }

            // Treat anything else as a correction: reopen the topic it most likely refers to, otherwise just note it.
            currentState.Answers.Remove(WizardTopic.ReviewAndConfirm);
            return new WizardTurnResult(
                $"Got it — noted: \"{userMessage}\". {WizardTopics.QuestionFor(WizardTopic.ReviewAndConfirm)}\n\n{RenderSummary(currentState)}",
                currentState, false, null);
        }

        var normalized = await NormalizeAnswerAsync(topic.Value, userMessage, cancellationToken);
        currentState.Answers[topic.Value] = normalized;

        var next = currentState.NextUnansweredTopic();
        if (next is null)
        {
            currentState.Completed = false;
            return new WizardTurnResult(RenderSummary(currentState) + "\n\n" + WizardTopics.QuestionFor(WizardTopic.ReviewAndConfirm), currentState, false, RenderSummary(currentState));
        }

        return new WizardTurnResult(WizardTopics.QuestionFor(next.Value), currentState, false, null);
    }

    private async Task<string> NormalizeAnswerAsync(WizardTopic topic, string rawAnswer, CancellationToken cancellationToken)
    {
        if (_defaultProviderKind == AiProviderKind.None || string.IsNullOrWhiteSpace(rawAnswer))
        {
            return rawAnswer.Trim();
        }

        try
        {
            var provider = _aiProviderFactory.Create(_defaultProviderKind);
            var result = await provider.CompleteAsync(new AiCompletionRequest
            {
                SystemPrompt = "You extract concise, structured requirements for a business application from a user's free-text answer. " +
                                "Reply with a single short paragraph capturing the key facts only — no preamble, no follow-up questions.",
                UserPrompt = $"Topic: {topic}. User answer: {rawAnswer}",
                MaxOutputTokens = 300
            }, cancellationToken);

            return string.IsNullOrWhiteSpace(result.Content) ? rawAnswer.Trim() : result.Content.Trim();
        }
        catch
        {
            // AI provider unreachable/misconfigured — degrade gracefully rather than block the wizard.
            return rawAnswer.Trim();
        }
    }

    public WizardSummary BuildSummary(WizardState state)
    {
        string Get(WizardTopic t) => state.Answers.TryGetValue(t, out var v) ? v : string.Empty;

        var namePurpose = Get(WizardTopic.NamePurpose);
        var (name, purpose) = SplitNameAndPurpose(namePurpose);

        return new WizardSummary(
            Name: name,
            Purpose: purpose,
            BusinessProcess: Get(WizardTopic.BusinessProcess),
            InputSources: Get(WizardTopic.InputSources),
            OutputDestinations: Get(WizardTopic.OutputDestinations),
            BusinessRules: Get(WizardTopic.BusinessRules),
            AgentsResponsibilities: Get(WizardTopic.AgentsResponsibilities),
            UsersRoles: Get(WizardTopic.UsersRoles),
            DeploymentSettings: Get(WizardTopic.DeploymentSettings),
            HealthMonitoring: Get(WizardTopic.HealthMonitoring),
            UiRequirements: Get(WizardTopic.UiRequirements),
            AiProviderKey: Get(WizardTopic.AiProviderKey));
    }

    private static (string Name, string Purpose) SplitNameAndPurpose(string namePurpose)
    {
        if (string.IsNullOrWhiteSpace(namePurpose))
        {
            return ("Untitled Application", string.Empty);
        }

        var parts = namePurpose.Split(new[] { '.', ':', '-' }, 2, StringSplitOptions.TrimEntries);
        return parts.Length == 2 ? (parts[0], parts[1]) : (namePurpose, namePurpose);
    }

    private string RenderSummary(WizardState state)
    {
        var summary = BuildSummary(state);
        var sb = new StringBuilder();
        sb.AppendLine("**Summary so far**");
        sb.AppendLine($"- Name: {summary.Name}");
        if (!string.IsNullOrWhiteSpace(summary.Purpose)) sb.AppendLine($"- Purpose: {summary.Purpose}");
        if (!string.IsNullOrWhiteSpace(summary.BusinessProcess)) sb.AppendLine($"- Business process: {summary.BusinessProcess}");
        if (!string.IsNullOrWhiteSpace(summary.InputSources)) sb.AppendLine($"- Inputs: {summary.InputSources}");
        if (!string.IsNullOrWhiteSpace(summary.OutputDestinations)) sb.AppendLine($"- Outputs: {summary.OutputDestinations}");
        if (!string.IsNullOrWhiteSpace(summary.BusinessRules)) sb.AppendLine($"- Business rules: {summary.BusinessRules}");
        if (!string.IsNullOrWhiteSpace(summary.AgentsResponsibilities)) sb.AppendLine($"- Agents: {summary.AgentsResponsibilities}");
        if (!string.IsNullOrWhiteSpace(summary.UsersRoles)) sb.AppendLine($"- Users/roles: {summary.UsersRoles}");
        if (!string.IsNullOrWhiteSpace(summary.DeploymentSettings)) sb.AppendLine($"- Deployment: {summary.DeploymentSettings}");
        if (!string.IsNullOrWhiteSpace(summary.HealthMonitoring)) sb.AppendLine($"- Health monitoring: {summary.HealthMonitoring}");
        if (!string.IsNullOrWhiteSpace(summary.UiRequirements)) sb.AppendLine($"- UI: {summary.UiRequirements}");
        if (!string.IsNullOrWhiteSpace(summary.AiProviderKey)) sb.AppendLine($"- AI provider: {summary.AiProviderKey}");
        return sb.ToString();
    }
}
