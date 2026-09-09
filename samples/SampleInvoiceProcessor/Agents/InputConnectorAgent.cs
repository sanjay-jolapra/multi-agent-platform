using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace SampleInvoiceProcessor.Agents;

/// <summary>
/// Reads every *.json file dropped into ./incoming, parses each as a raw invoice payload
/// (expected keys: vendorName, invoiceNumber, amount), and moves handled files into
/// ./incoming/processed so a re-run never re-ingests them.
/// </summary>
public class InputConnectorAgent : IPipelineAgent
{
    private readonly ILogger<InputConnectorAgent> _logger;
    private readonly string _incomingDir;
    private readonly string _processedDir;

    public string Name => "InputConnector";

    public InputConnectorAgent(ILogger<InputConnectorAgent> logger)
    {
        _logger = logger;
        _incomingDir = Path.Combine(AppContext.BaseDirectory, "incoming");
        _processedDir = Path.Combine(_incomingDir, "processed");
    }

    public Task<AgentResult> ExecuteAsync(AgentContext context, CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(_incomingDir);
        Directory.CreateDirectory(_processedDir);

        var invoices = new List<Dictionary<string, object?>>();
        var files = Directory.GetFiles(_incomingDir, "*.json", SearchOption.TopDirectoryOnly);

        foreach (var file in files)
        {
            try
            {
                var json = File.ReadAllText(file);
                var parsed = JsonSerializer.Deserialize<Dictionary<string, object?>>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (parsed is not null)
                {
                    invoices.Add(NormalizeElementValues(parsed));
                }

                var destination = Path.Combine(_processedDir, Path.GetFileName(file));
                File.Move(file, destination, overwrite: true);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to read incoming invoice file {File}", file);
                context.Trace.Add($"{Name}: failed to read {Path.GetFileName(file)} - {ex.Message}");
            }
        }

        context.Data["invoices"] = invoices;
        return Task.FromResult(AgentResult.Ok(new Dictionary<string, object?> { ["fileCount"] = files.Length, ["invoiceCount"] = invoices.Count }));
    }

    private static Dictionary<string, object?> NormalizeElementValues(Dictionary<string, object?> raw)
    {
        var result = new Dictionary<string, object?>();
        foreach (var (key, value) in raw)
        {
            result[key] = value is JsonElement element ? ExtractValue(element) : value;
        }
        return result;
    }

    private static object? ExtractValue(JsonElement element) => element.ValueKind switch
    {
        JsonValueKind.String => element.GetString(),
        JsonValueKind.Number => element.ToString(),
        JsonValueKind.True => true,
        JsonValueKind.False => false,
        JsonValueKind.Null => null,
        _ => element.ToString()
    };
}
