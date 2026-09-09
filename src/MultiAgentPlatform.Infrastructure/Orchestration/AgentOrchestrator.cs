using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using MultiAgentPlatform.Domain.Entities;
using MultiAgentPlatform.Domain.Enums;
using MultiAgentPlatform.Domain.Interfaces;
using MultiAgentPlatform.Infrastructure.Agents;
using MultiAgentPlatform.Infrastructure.Persistence;

namespace MultiAgentPlatform.Infrastructure.Orchestration;

public class AgentOrchestrator : IAgentOrchestrator
{
    private readonly AgentFactory _agentFactory;
    private readonly IAiProviderFactory _aiProviderFactory;
    private readonly MainDbContext _dbContext;

    public AgentOrchestrator(AgentFactory agentFactory, IAiProviderFactory aiProviderFactory, MainDbContext dbContext)
    {
        _agentFactory = agentFactory;
        _aiProviderFactory = aiProviderFactory;
        _dbContext = dbContext;
    }

    public async Task<AgentResult> RunPipelineAsync(GeneratedApplication application, Dictionary<string, object?> triggerPayload, CancellationToken cancellationToken)
    {
        var correlationId = Guid.NewGuid().ToString("N");

        var agentDefinitions = await _dbContext.AgentDefinitions
            .Where(a => a.GeneratedApplicationId == application.Id && a.IsEnabled)
            .OrderBy(a => a.ExecutionOrder)
            .ToListAsync(cancellationToken);

        var connectors = await _dbContext.ConnectorDefinitions
            .Where(c => c.GeneratedApplicationId == application.Id)
            .ToListAsync(cancellationToken);

        var aiProvider = _aiProviderFactory.Create(application.AiProviderKind);

        var context = new AgentContext
        {
            GeneratedApplicationId = application.Id,
            CorrelationId = correlationId,
            Data = new Dictionary<string, object?>(triggerPayload)
        };

        AgentResult finalResult = AgentResult.Ok(context.Data);

        foreach (var definition in agentDefinitions)
        {
            var log = new AgentExecutionLog
            {
                AgentDefinitionId = definition.Id,
                GeneratedApplicationId = application.Id,
                CorrelationId = correlationId,
                Status = ExecutionStatus.Running,
                StartedAt = DateTimeOffset.UtcNow
            };
            _dbContext.AgentExecutionLogs.Add(log);
            await _dbContext.SaveChangesAsync(cancellationToken);

            var agent = _agentFactory.Create(definition, aiProvider, connectors);
            var (result, errorMessage) = await ExecuteWithRetryAsync(agent, context, definition, cancellationToken);

            if (result is { Success: true })
            {
                foreach (var (key, value) in result.Output)
                {
                    context.Data[key] = value;
                }

                log.Status = ExecutionStatus.Success;
                log.CompletedAt = DateTimeOffset.UtcNow;
                log.OutputSummary = Truncate(JsonSerializer.Serialize(result.Output));
                await _dbContext.SaveChangesAsync(cancellationToken);

                finalResult = AgentResult.Ok(context.Data);
                continue;
            }

            log.Status = ExecutionStatus.Failed;
            log.CompletedAt = DateTimeOffset.UtcNow;
            log.ErrorMessage = errorMessage;
            await _dbContext.SaveChangesAsync(cancellationToken);

            // ErrorHandling and Monitoring are best-effort extension points; their own failure
            // never stops the pipeline. Any other role's exhausted retries aborts the run.
            if (definition.Role is AgentRole.ErrorHandling or AgentRole.Monitoring)
            {
                continue;
            }

            finalResult = AgentResult.Fail(errorMessage ?? "Agent execution failed.");
            application.LastFailedExecutionAt = DateTimeOffset.UtcNow;
            await _dbContext.SaveChangesAsync(cancellationToken);
            return finalResult;
        }

        application.LastSuccessfulExecutionAt = DateTimeOffset.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);

        return finalResult;
    }

    private static async Task<(AgentResult? Result, string? ErrorMessage)> ExecuteWithRetryAsync(
        IAgent agent, AgentContext context, AgentDefinition definition, CancellationToken cancellationToken)
    {
        string? lastError = null;

        for (var attempt = 0; attempt <= definition.MaxRetries; attempt++)
        {
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(TimeSpan.FromSeconds(definition.TimeoutSeconds));

            try
            {
                var result = await agent.ExecuteAsync(context, timeoutCts.Token);
                if (result.Success)
                {
                    return (result, null);
                }

                lastError = result.ErrorMessage ?? "Agent reported failure.";
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                lastError = $"Agent '{definition.Name}' timed out after {definition.TimeoutSeconds}s.";
            }
            catch (Exception ex)
            {
                lastError = ex.Message;
            }

            if (attempt < definition.MaxRetries)
            {
                await Task.Delay(TimeSpan.FromSeconds(definition.RetryBackoffSeconds), cancellationToken);
            }
        }

        return (null, lastError);
    }

    private static string Truncate(string value) => value.Length <= 500 ? value : value[..500];
}
