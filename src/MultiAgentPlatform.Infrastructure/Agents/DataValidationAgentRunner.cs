using System.Text.Json;
using MultiAgentPlatform.Domain.Entities;
using MultiAgentPlatform.Domain.Interfaces;

namespace MultiAgentPlatform.Infrastructure.Agents;

public class DataValidationAgentRunner : IAgent
{
    public DataValidationAgentRunner(AgentDefinition definition)
    {
        Definition = definition;
    }

    public AgentDefinition Definition { get; }

    public Task<AgentResult> ExecuteAsync(AgentContext context, CancellationToken cancellationToken)
    {
        var requiredKeys = ParseRequiredKeys();

        var records = context.Data.TryGetValue("records", out var recordsValue) && recordsValue is List<Dictionary<string, object?>> list
            ? list
            : new List<Dictionary<string, object?>> { context.Data };

        if (requiredKeys.Count == 0)
        {
            var output = new Dictionary<string, object?> { ["validRecords"] = records.Count, ["invalidRecords"] = 0 };
            return Task.FromResult(AgentResult.Ok(output));
        }

        var validCount = 0;
        var invalidCount = 0;

        foreach (var record in records)
        {
            var isValid = requiredKeys.All(key => record.TryGetValue(key, out var value) && value is not null);
            if (isValid) validCount++;
            else invalidCount++;
        }

        var result = new Dictionary<string, object?> { ["validRecords"] = validCount, ["invalidRecords"] = invalidCount };

        if (records.Count > 0 && validCount == 0)
        {
            return Task.FromResult(AgentResult.Fail("No records passed validation against the configured input contract."));
        }

        return Task.FromResult(AgentResult.Ok(result));
    }

    private List<string> ParseRequiredKeys()
    {
        try
        {
            var dict = JsonSerializer.Deserialize<Dictionary<string, object?>>(Definition.InputContractJson);
            return dict is null ? new List<string>() : dict.Keys.ToList();
        }
        catch
        {
            return new List<string>();
        }
    }
}
