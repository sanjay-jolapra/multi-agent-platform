using Microsoft.Extensions.Logging;

namespace SampleInvoiceProcessor.Agents;

/// <summary>
/// Runs the fixed invoice-processing pipeline in order:
///   Input -&gt; Validation -&gt; Processing -&gt; Output -&gt; Monitoring -&gt; ErrorHandling
/// A hard failure (exception or AgentResult.Fail) in Input/Validation/Processing/Output stops
/// the pipeline immediately. Monitoring and ErrorHandling always run (best-effort) even if an
/// earlier stage failed, and a failure in either of them is recorded but never stops the run -
/// they are observability/cleanup stages, not business-critical ones.
/// </summary>
public class OrchestratorAgent
{
    private static readonly HashSet<string> BestEffortStages = new() { "Monitoring", "ErrorHandling" };

    private readonly ILogger<OrchestratorAgent> _logger;
    private readonly InputConnectorAgent _input;
    private readonly DataValidationAgent _validation;
    private readonly ProcessingAgent _processing;
    private readonly OutputConnectorAgent _output;
    private readonly MonitoringAgent _monitoring;
    private readonly ErrorHandlingAgent _errorHandling;

    public OrchestratorAgent(
        ILogger<OrchestratorAgent> logger,
        InputConnectorAgent input,
        DataValidationAgent validation,
        ProcessingAgent processing,
        OutputConnectorAgent output,
        MonitoringAgent monitoring,
        ErrorHandlingAgent errorHandling)
    {
        _logger = logger;
        _input = input;
        _validation = validation;
        _processing = processing;
        _output = output;
        _monitoring = monitoring;
        _errorHandling = errorHandling;
    }

    public async Task<AgentResult> RunAsync(AgentContext context, CancellationToken cancellationToken)
    {
        context.Data["_pipelineStartedAt"] = DateTimeOffset.UtcNow;

        var criticalStages = new IPipelineAgent[] { _input, _validation, _processing, _output };
        var bestEffortStages = new IPipelineAgent[] { _monitoring, _errorHandling };

        foreach (var stage in criticalStages)
        {
            var stageResult = await RunStageAsync(stage, context, cancellationToken);
            if (!stageResult.Success)
            {
                context.Trace.Add($"[FAIL] {stage.Name}: {stageResult.ErrorMessage}");
                await RunBestEffortStagesAsync(bestEffortStages, context, cancellationToken);
                return AgentResult.Fail($"Pipeline stopped at {stage.Name}: {stageResult.ErrorMessage}");
            }
        }

        var finalResult = await RunBestEffortStagesAsync(bestEffortStages, context, cancellationToken);
        return finalResult;
    }

    private async Task<AgentResult> RunBestEffortStagesAsync(IPipelineAgent[] stages, AgentContext context, CancellationToken cancellationToken)
    {
        foreach (var stage in stages)
        {
            await RunStageAsync(stage, context, cancellationToken);
        }

        return AgentResult.Ok(new Dictionary<string, object?> { ["trace"] = context.Trace });
    }

    private async Task<AgentResult> RunStageAsync(IPipelineAgent stage, AgentContext context, CancellationToken cancellationToken)
    {
        try
        {
            context.Trace.Add($"[START] {stage.Name}");
            var result = await stage.ExecuteAsync(context, cancellationToken);
            context.Trace.Add(result.Success ? $"[OK] {stage.Name}" : $"[FAIL] {stage.Name}: {result.ErrorMessage}");
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Pipeline stage {Stage} threw an unhandled exception", stage.Name);
            var isBestEffort = BestEffortStages.Contains(stage.Name);
            context.Trace.Add($"[{(isBestEffort ? "WARN" : "FAIL")}] {stage.Name}: {ex.Message}");
            return AgentResult.Fail(ex.Message);
        }
    }
}
