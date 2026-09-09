using SampleInvoiceProcessor.Agents;

namespace SampleInvoiceProcessor.Services;

/// <summary>
/// Composes the fixed agent pipeline behind a single entry point. Both the background
/// polling service and the manual "Run pipeline now" admin action call this.
/// </summary>
public class InvoicePipelineRunner
{
    private readonly OrchestratorAgent _orchestrator;
    private readonly Guid _applicationId;

    public InvoicePipelineRunner(OrchestratorAgent orchestrator, IConfiguration configuration)
    {
        _orchestrator = orchestrator;
        _applicationId = Guid.TryParse(configuration["Application:Id"], out var id) ? id : Guid.Empty;
    }

    public async Task<AgentResult> RunOnceAsync(CancellationToken cancellationToken)
    {
        var context = new AgentContext
        {
            GeneratedApplicationId = _applicationId,
            CorrelationId = Guid.NewGuid().ToString("N")
        };

        return await _orchestrator.RunAsync(context, cancellationToken);
    }
}
