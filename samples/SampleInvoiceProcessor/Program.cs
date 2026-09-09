using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using SampleInvoiceProcessor.Agents;
using SampleInvoiceProcessor.Data;
using SampleInvoiceProcessor.Services;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("InvoiceDb") ?? "Data Source=App_Data/invoices.db";
var dbDirectory = Path.GetDirectoryName(Path.Combine(AppContext.BaseDirectory, connectionString.Replace("Data Source=", string.Empty)));
if (!string.IsNullOrEmpty(dbDirectory))
{
    Directory.CreateDirectory(dbDirectory);
}

builder.Services.AddDbContext<InvoiceDbContext>(options => options.UseSqlite(connectionString));

builder.Services.AddSingleton<IPipelineStatusStore, PipelineStatusStore>();

builder.Services.AddScoped<InputConnectorAgent>();
builder.Services.AddScoped<DataValidationAgent>();
builder.Services.AddScoped<ProcessingAgent>();
builder.Services.AddScoped<OutputConnectorAgent>();
builder.Services.AddScoped<MonitoringAgent>();
builder.Services.AddScoped<ErrorHandlingAgent>();
builder.Services.AddScoped<OrchestratorAgent>();
builder.Services.AddScoped<InvoicePipelineRunner>();

builder.Services.AddHttpClient();
builder.Services.AddHostedService<PipelinePollingService>();
builder.Services.AddHostedService<HeartbeatBackgroundService>();

builder.Services.AddHealthChecks()
    .AddDbContextCheck<InvoiceDbContext>(name: "database")
    .AddCheck<PipelineHealthCheck>("pipeline");

var app = builder.Build();

// EnsureCreated (not EF Core migrations) is intentional for this sample: there is no dotnet
// SDK available to author/run `dotnet ef migrations add` in the environment this was built in,
// and EnsureCreated is the documented approach for a simple, self-contained sample database
// with a schema that never changes after deployment.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<InvoiceDbContext>();
    db.Database.EnsureCreated();
}

// Liveness intentionally runs zero checks (Predicate = _ => false): it only proves the process
// is up and answering HTTP requests, and must never depend on the database or pipeline state -
// otherwise a slow DB or a stalled pipeline would make an orchestrator kill a perfectly live
// container. Readiness below runs the full check set instead.
app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false });
app.MapHealthChecks("/health/ready");

app.MapGet("/", async (InvoiceDbContext db, IPipelineStatusStore statusStore) =>
{
    var recentInvoices = await db.Invoices
        .OrderByDescending(i => i.ReceivedAt)
        .Take(50)
        .ToListAsync();

    var html = StatusPage.Render(recentInvoices, statusStore.Snapshot);
    return Results.Content(html, "text/html");
});

app.MapPost("/admin/run-now", async (InvoicePipelineRunner runner, CancellationToken ct) =>
{
    await runner.RunOnceAsync(ct);
    return Results.Redirect("/");
});

app.Run();
