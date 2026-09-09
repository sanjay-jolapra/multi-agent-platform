using System.Text.Json;
using MultiAgentPlatform.Domain.Entities;
using MultiAgentPlatform.Domain.Enums;
using MultiAgentPlatform.Domain.Interfaces;

namespace MultiAgentPlatform.Infrastructure.Connectors;

public class FileConnector : IConnector
{
    public FileConnector(ConnectorDefinition definition)
    {
        Definition = definition;
    }

    public ConnectorDefinition Definition { get; }

    private record FileConfig(string Directory, string Pattern);

    private FileConfig ParseConfig()
    {
        try
        {
            using var doc = JsonDocument.Parse(Definition.ConfigJson);
            var root = doc.RootElement;
            var directory = root.TryGetProperty("directory", out var d) ? d.GetString() ?? "./data" : "./data";
            var pattern = root.TryGetProperty("pattern", out var p) ? p.GetString() ?? "*.json" : "*.json";
            return new FileConfig(directory, pattern);
        }
        catch
        {
            return new FileConfig("./data", "*.json");
        }
    }

    public async Task<ConnectorResult> ExecuteAsync(ConnectorContext context, CancellationToken cancellationToken)
    {
        var config = ParseConfig();

        try
        {
            System.IO.Directory.CreateDirectory(config.Directory);

            if (Definition.Direction == ConnectorDirection.Input)
            {
                var files = System.IO.Directory.GetFiles(config.Directory, config.Pattern);
                if (files.Length == 0)
                {
                    return ConnectorResult.Ok(0, new Dictionary<string, object?> { ["records"] = new List<Dictionary<string, object?>>() });
                }

                var file = files[0];
                var text = await File.ReadAllTextAsync(file, cancellationToken);

                List<Dictionary<string, object?>> records;
                try
                {
                    records = JsonSerializer.Deserialize<List<Dictionary<string, object?>>>(text) ?? new();
                }
                catch
                {
                    var single = JsonSerializer.Deserialize<Dictionary<string, object?>>(text);
                    records = single is null ? new() : new List<Dictionary<string, object?>> { single };
                }

                var processedDir = Path.Combine(config.Directory, "processed");
                System.IO.Directory.CreateDirectory(processedDir);
                var destination = Path.Combine(processedDir, Path.GetFileName(file));
                File.Move(file, destination, overwrite: true);

                return ConnectorResult.Ok(records.Count, new Dictionary<string, object?> { ["records"] = records });
            }

            var fileName = $"output-{DateTimeOffset.UtcNow:yyyyMMddHHmmssfff}.json";
            var outputPath = Path.Combine(config.Directory, fileName);
            var json = JsonSerializer.Serialize(context.Data, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(outputPath, json, cancellationToken);

            return ConnectorResult.Ok(1, new Dictionary<string, object?> { ["filePath"] = outputPath });
        }
        catch (Exception ex)
        {
            return ConnectorResult.Fail($"FileConnector: {ex.Message}");
        }
    }

    public Task<bool> TestConnectionAsync(CancellationToken cancellationToken)
    {
        var config = ParseConfig();
        try
        {
            System.IO.Directory.CreateDirectory(config.Directory);
            return Task.FromResult(System.IO.Directory.Exists(config.Directory));
        }
        catch
        {
            return Task.FromResult(false);
        }
    }
}
