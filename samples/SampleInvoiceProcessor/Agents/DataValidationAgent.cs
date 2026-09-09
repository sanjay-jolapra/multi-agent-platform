using System.Globalization;

namespace SampleInvoiceProcessor.Agents;

/// <summary>
/// Splits the raw invoices read by InputConnectorAgent into valid / invalid buckets.
/// A valid invoice has non-empty vendorName and invoiceNumber, and an amount that parses
/// as a positive decimal.
/// </summary>
public class DataValidationAgent : IPipelineAgent
{
    public string Name => "DataValidation";

    public Task<AgentResult> ExecuteAsync(AgentContext context, CancellationToken cancellationToken)
    {
        var invoices = context.Data.TryGetValue("invoices", out var raw) && raw is List<Dictionary<string, object?>> list
            ? list
            : new List<Dictionary<string, object?>>();

        var validInvoices = new List<Dictionary<string, object?>>();
        var invalidInvoices = new List<Dictionary<string, object?>>();

        foreach (var invoice in invoices)
        {
            var error = Validate(invoice);
            if (error is null)
            {
                validInvoices.Add(invoice);
            }
            else
            {
                invoice["validationError"] = error;
                invalidInvoices.Add(invoice);
            }
        }

        context.Data["validInvoices"] = validInvoices;
        context.Data["invalidInvoices"] = invalidInvoices;

        return Task.FromResult(AgentResult.Ok(new Dictionary<string, object?>
        {
            ["validCount"] = validInvoices.Count,
            ["invalidCount"] = invalidInvoices.Count
        }));
    }

    private static string? Validate(Dictionary<string, object?> invoice)
    {
        if (!invoice.TryGetValue("vendorName", out var vendorObj) || string.IsNullOrWhiteSpace(vendorObj?.ToString()))
        {
            return "Missing vendorName";
        }

        if (!invoice.TryGetValue("invoiceNumber", out var numberObj) || string.IsNullOrWhiteSpace(numberObj?.ToString()))
        {
            return "Missing invoiceNumber";
        }

        if (!invoice.TryGetValue("amount", out var amountObj) || amountObj is null)
        {
            return "Missing amount";
        }

        if (!decimal.TryParse(amountObj.ToString(), NumberStyles.Number, CultureInfo.InvariantCulture, out var amount) || amount <= 0)
        {
            return "amount must be a positive decimal";
        }

        return null;
    }
}
