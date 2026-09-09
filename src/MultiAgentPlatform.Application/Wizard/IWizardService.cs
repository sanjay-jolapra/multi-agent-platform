namespace MultiAgentPlatform.Application.Wizard;

public record WizardTurnResult(string AssistantMessage, WizardState State, bool ReadyToBuild, string? SummaryMarkdown);

/// <summary>
/// Drives the one-topic-at-a-time application wizard: parses free text (via the configured
/// AI provider when available, falling back to simple heuristics otherwise), fills in the
/// slots required by section 4.1 of the spec, and asks only for what's still missing.
/// </summary>
public interface IWizardService
{
    Task<WizardTurnResult> ProcessUserMessageAsync(WizardState currentState, string userMessage, CancellationToken cancellationToken);

    WizardSummary BuildSummary(WizardState state);
}
