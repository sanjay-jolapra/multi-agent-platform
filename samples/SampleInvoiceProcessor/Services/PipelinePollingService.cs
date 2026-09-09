namespace SampleInvoiceProcessor.Services;

/// <summary>
/// Runs the invoice pipeline on a fixed interval, each run in its own DI scope (InvoicePipelineRunner
/// and InvoiceDbContext are scoped). A failed run is logged and never stops the loop.
/// </summary>
public class PipelinePollingService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<PipelinePollingService> _logger;
    private readonly TimeSpan _interval;

    public PipelinePollingService(IServiceScopeFactory scopeFactory, ILogger<PipelinePollingService> logger, IConfiguration configuration)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        var seconds = configuration.GetValue<int?>("Pipeline:PollingIntervalSeconds") ?? 30;
        _interval = TimeSpan.FromSeconds(seconds);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var runner = scope.ServiceProvider.GetRequiredService<InvoicePipelineRunner>();
                var result = await runner.RunOnceAsync(stoppingToken);
                if (!result.Success)
                {
                    _logger.LogWarning("Invoice pipeline run completed with failure: {Error}", result.ErrorMessage);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Invoice pipeline run threw an unhandled exception");
            }

            try
            {
                await Task.Delay(_interval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // Expected during shutdown.
            }
        }
    }
}
