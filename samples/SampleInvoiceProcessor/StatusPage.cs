using System.Text;
using SampleInvoiceProcessor.Agents;
using SampleInvoiceProcessor.Data;

namespace SampleInvoiceProcessor;

/// <summary>Renders the minimal server-side status page shown at GET /. Plain string
/// generation is deliberate here - this sample is small enough that a Razor view would add
/// ceremony without adding clarity.</summary>
public static class StatusPage
{
    public static string Render(IReadOnlyList<Invoice> recentInvoices, PipelineRunSnapshot snapshot)
    {
        var rows = new StringBuilder();
        foreach (var invoice in recentInvoices)
        {
            rows.Append($"""
                <tr>
                    <td>{Escape(invoice.VendorName)}</td>
                    <td>{Escape(invoice.InvoiceNumber)}</td>
                    <td class="text-end">{invoice.Amount:N2}</td>
                    <td class="text-end">{invoice.TaxAmount:N2}</td>
                    <td class="text-end">{invoice.TotalAmount:N2}</td>
                    <td><span class="badge {StatusBadgeClass(invoice.Status)}">{Escape(invoice.Status)}</span></td>
                    <td>{invoice.ReceivedAt:yyyy-MM-dd HH:mm:ss} UTC</td>
                </tr>

                """);
        }

        if (recentInvoices.Count == 0)
        {
            rows.Append("""<tr><td colspan="7" class="text-center text-muted">No invoices processed yet. Drop a .json file into ./incoming or click "Run pipeline now".</td></tr>""");
        }

        var lastRunAt = snapshot.LastRunAt is { } lastRun ? $"{lastRun:yyyy-MM-dd HH:mm:ss} UTC" : "never";
        var lastRunOutcome = snapshot.LastRunSucceeded switch
        {
            true => "<span class=\"badge bg-success\">Succeeded</span>",
            false => "<span class=\"badge bg-danger\">Failed</span>",
            null => "<span class=\"badge bg-secondary\">No runs yet</span>"
        };

        return $"""
            <!doctype html>
            <html lang="en">
            <head>
                <meta charset="utf-8" />
                <meta name="viewport" content="width=device-width, initial-scale=1" />
                <title>Sample Invoice Processor</title>
                <link href="https://cdn.jsdelivr.net/npm/bootstrap@5.3.3/dist/css/bootstrap.min.css" rel="stylesheet" />
            </head>
            <body class="bg-light">
                <nav class="navbar navbar-dark bg-dark mb-4">
                    <div class="container">
                        <span class="navbar-brand mb-0 h1">Sample Invoice Processor</span>
                    </div>
                </nav>
                <div class="container">
                    <div class="card mb-4">
                        <div class="card-body d-flex flex-wrap justify-content-between align-items-center gap-3">
                            <div>
                                <div><strong>Last run:</strong> {lastRunAt}</div>
                                <div><strong>Outcome:</strong> {lastRunOutcome}</div>
                                <div><strong>Run count:</strong> {snapshot.RunCount}</div>
                                <div><strong>Last run invoices / invalid:</strong> {snapshot.LastRunInvoiceCount} / {snapshot.LastRunInvalidCount}</div>
                            </div>
                            <form method="post" action="/admin/run-now">
                                <button type="submit" class="btn btn-primary">Run pipeline now</button>
                            </form>
                        </div>
                    </div>

                    <h5>Recent invoices</h5>
                    <div class="table-responsive">
                        <table class="table table-striped table-bordered bg-white">
                            <thead>
                                <tr>
                                    <th>Vendor</th>
                                    <th>Invoice #</th>
                                    <th class="text-end">Amount</th>
                                    <th class="text-end">Tax</th>
                                    <th class="text-end">Total</th>
                                    <th>Status</th>
                                    <th>Received At</th>
                                </tr>
                            </thead>
                            <tbody>
                                {rows}
                            </tbody>
                        </table>
                    </div>
                </div>
            </body>
            </html>
            """;
    }

    private static string StatusBadgeClass(string status) => status switch
    {
        "Processed" => "bg-success",
        "Rejected" => "bg-danger",
        _ => "bg-secondary"
    };

    private static string Escape(string value) => System.Net.WebUtility.HtmlEncode(value);
}
