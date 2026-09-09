using System.Text.Json;
using Microsoft.Data.Sqlite;
using MultiAgentPlatform.Domain.Entities;
using MultiAgentPlatform.Domain.Enums;
using MultiAgentPlatform.Domain.Interfaces;

namespace MultiAgentPlatform.Infrastructure.Connectors;

public class DatabaseConnector : IConnector
{
    public DatabaseConnector(ConnectorDefinition definition)
    {
        Definition = definition;
    }

    public ConnectorDefinition Definition { get; }

    private record DbConfig(string? ConnectionString, string? Query);

    private DbConfig? ParseConfig()
    {
        try
        {
            using var doc = JsonDocument.Parse(Definition.ConfigJson);
            var root = doc.RootElement;
            var connectionString = root.TryGetProperty("connectionString", out var c) ? c.GetString() : null;
            var query = root.TryGetProperty("query", out var q) ? q.GetString() : null;
            return new DbConfig(connectionString, query);
        }
        catch
        {
            return null;
        }
    }

    public async Task<ConnectorResult> ExecuteAsync(ConnectorContext context, CancellationToken cancellationToken)
    {
        var config = ParseConfig();
        if (config is null || string.IsNullOrWhiteSpace(config.ConnectionString) || string.IsNullOrWhiteSpace(config.Query))
        {
            return ConnectorResult.Fail("DatabaseConnector: missing or malformed 'connectionString'/'query' in ConfigJson.");
        }

        try
        {
            using var connection = new SqliteConnection(config.ConnectionString);
            await connection.OpenAsync(cancellationToken);

            if (Definition.Direction == ConnectorDirection.Input)
            {
                using var command = connection.CreateCommand();
                command.CommandText = config.Query;

                var records = new List<Dictionary<string, object?>>();
                using var reader = await command.ExecuteReaderAsync(cancellationToken);
                while (await reader.ReadAsync(cancellationToken))
                {
                    var row = new Dictionary<string, object?>();
                    for (var i = 0; i < reader.FieldCount; i++)
                    {
                        row[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);
                    }
                    records.Add(row);
                }

                return ConnectorResult.Ok(records.Count, new Dictionary<string, object?> { ["records"] = records });
            }

            using var writeCommand = connection.CreateCommand();
            writeCommand.CommandText = config.Query;
            foreach (var (key, value) in context.Data)
            {
                var parameter = writeCommand.CreateParameter();
                parameter.ParameterName = $"@{key}";
                parameter.Value = value switch
                {
                    null => DBNull.Value,
                    string s => s,
                    bool b => b,
                    DateTime dt => dt,
                    DateTimeOffset dto => dto.UtcDateTime,
                    byte or sbyte or short or ushort or int or uint or long or ulong or float or double or decimal => value,
                    _ => DBNull.Value
                };
                writeCommand.Parameters.Add(parameter);
            }

            var affected = await writeCommand.ExecuteNonQueryAsync(cancellationToken);
            return ConnectorResult.Ok(affected, new Dictionary<string, object?> { ["rowsAffected"] = affected });
        }
        catch (Exception ex)
        {
            return ConnectorResult.Fail($"DatabaseConnector: {ex.Message}");
        }
    }

    public async Task<bool> TestConnectionAsync(CancellationToken cancellationToken)
    {
        var config = ParseConfig();
        if (config is null || string.IsNullOrWhiteSpace(config.ConnectionString))
        {
            return false;
        }

        try
        {
            using var connection = new SqliteConnection(config.ConnectionString);
            await connection.OpenAsync(cancellationToken);
            await connection.CloseAsync();
            return true;
        }
        catch
        {
            return false;
        }
    }
}
