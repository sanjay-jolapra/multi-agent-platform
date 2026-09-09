using Microsoft.Extensions.Logging;

namespace SampleInvoiceProcessor.Agents;

/// <summary>
/// Logs invalid invoices and any failure entries recorded in the trace by earlier stages.
/// Always succeeds so a bad batch never brings down the whole pipeline run.
/// </summary>
public class ErrorHandlingAgent : IPipelineAgent
{
    private readonly ILogger<ErrorHandlingAgent> _logger;

    public string Name => "ErrorHandling";

    public ErrorHandlingAgent(ILogger<ErrorHandlingAgent> logger)
    {
        _logger = logger;
    }

    public Task<AgentResult> ExecuteAsync(AgentContext context, CancellationToken cancellationToken)
    {
        if (context.Data.TryGetValue("invalidInvoices", out var raw) && raw is List<Dictionary<string, object?>> invalidInvoices)
        {
            foreach (var invoice in invalidInvoices)
            {
                _logger.LogWarning(
                    "Rejected invoice vendor={Vendor} number={Number} reason={Reason}",
                    invoice.GetValueOrDefault("vendorName"),
                    invoice.GetValueOrDefault("invoiceNumber"),
                    invoice.GetValueOrDefault("validationError"));
            }
        }

        foreach (var traceEntry in context.Trace)
        {
            _logger.LogWarning("Pipeline trace: {Entry}", traceEntry);
        }

        return Task.FromResult(AgentResult.Ok());
    }
}
