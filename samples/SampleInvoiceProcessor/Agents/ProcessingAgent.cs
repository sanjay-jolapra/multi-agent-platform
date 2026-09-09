using System.Globalization;
using SampleInvoiceProcessor.Data;

namespace SampleInvoiceProcessor.Agents;

/// <summary>
/// Computes tax and total for each validated invoice and builds the Invoice entities that
/// OutputConnectorAgent will persist.
/// </summary>
public class ProcessingAgent : IPipelineAgent
{
    private const decimal TaxRate = 0.10m;

    public string Name => "Processing";

    public Task<AgentResult> ExecuteAsync(AgentContext context, CancellationToken cancellationToken)
    {
        var validInvoices = context.Data.TryGetValue("validInvoices", out var raw) && raw is List<Dictionary<string, object?>> list
            ? list
            : new List<Dictionary<string, object?>>();

        var processed = new List<Invoice>();

        foreach (var invoice in validInvoices)
        {
            var amount = decimal.Parse(invoice["amount"]!.ToString()!, NumberStyles.Number, CultureInfo.InvariantCulture);
            var taxAmount = decimal.Round(amount * TaxRate, 2, MidpointRounding.AwayFromZero);
            var totalAmount = amount + taxAmount;

            processed.Add(new Invoice
            {
                Id = Guid.NewGuid(),
                VendorName = invoice["vendorName"]!.ToString()!,
                InvoiceNumber = invoice["invoiceNumber"]!.ToString()!,
                Amount = amount,
                TaxAmount = taxAmount,
                TotalAmount = totalAmount,
                Status = "Processed",
                ReceivedAt = DateTimeOffset.UtcNow,
                ProcessedAt = DateTimeOffset.UtcNow
            });
        }

        context.Data["processedInvoices"] = processed;
        return Task.FromResult(AgentResult.Ok(new Dictionary<string, object?> { ["processedCount"] = processed.Count }));
    }
}
