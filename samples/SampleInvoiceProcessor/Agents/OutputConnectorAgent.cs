using System.Text.Json;
using SampleInvoiceProcessor.Data;

namespace SampleInvoiceProcessor.Agents;

/// <summary>
/// Persists processed invoices to the SQLite database and writes a per-invoice JSON summary
/// into ./outgoing, simulating an outbound integration (e.g. notifying a downstream system).
/// </summary>
public class OutputConnectorAgent : IPipelineAgent
{
    private readonly InvoiceDbContext _dbContext;
    private readonly string _outgoingDir;

    public string Name => "OutputConnector";

    public OutputConnectorAgent(InvoiceDbContext dbContext)
    {
        _dbContext = dbContext;
        _outgoingDir = Path.Combine(AppContext.BaseDirectory, "outgoing");
    }

    public async Task<AgentResult> ExecuteAsync(AgentContext context, CancellationToken cancellationToken)
    {
        var processed = context.Data.TryGetValue("processedInvoices", out var raw) && raw is List<Invoice> list
            ? list
            : new List<Invoice>();

        if (processed.Count == 0)
        {
            return AgentResult.Ok(new Dictionary<string, object?> { ["insertedCount"] = 0 });
        }

        Directory.CreateDirectory(_outgoingDir);

        _dbContext.Invoices.AddRange(processed);
        await _dbContext.SaveChangesAsync(cancellationToken);

        foreach (var invoice in processed)
        {
            var summaryPath = Path.Combine(_outgoingDir, $"{invoice.Id}.json");
            var summaryJson = JsonSerializer.Serialize(invoice, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(summaryPath, summaryJson, cancellationToken);
        }

        return AgentResult.Ok(new Dictionary<string, object?> { ["insertedCount"] = processed.Count });
    }
}
