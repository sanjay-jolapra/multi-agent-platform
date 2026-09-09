# Multi-Agent Application Builder Platform

A chat-based platform where a user, through conversation, designs and generates standalone
multi-agent applications. The **Main Application** (this solution) hosts the wizard, live
preview, agent/connector management, and health-monitoring dashboard. Every application it
generates (see `samples/SampleInvoiceProcessor`) runs as its own independent, containerized
product with its own SQLite database, agent pipeline, and health endpoints.

## Solution layout (Clean Architecture)

```
MultiAgentPlatform.sln
src/
  MultiAgentPlatform.Domain          entities, enums, IAgent/IConnector/IAiProvider contracts
  MultiAgentPlatform.Application     wizard state machine, builder/orchestration service interfaces, DTOs, Options
  MultiAgentPlatform.Infrastructure  EF Core (SQLite), AI provider adapters, connector implementations,
                                      agent implementations, DI wiring
  MultiAgentPlatform.Web             Razor Pages + SignalR chat wizard, dashboard, ASP.NET Identity/RBAC
samples/
  SampleInvoiceProcessor             one complete, standalone generated application (reference example)
deploy/nginx/nginx.conf.sample       subdomain reverse-proxy example
docker-compose.yml                   main app + sample app + nginx, each as its own container
```

Domain has no dependencies. Application depends only on Domain. Infrastructure implements
Application/Domain contracts (EF Core, HTTP-based AI providers, connectors, agents). Web
depends on all three and contains no business logic of its own — only Identity/RBAC,
Razor Pages, the SignalR hub, and two small API controllers.

Patterns used: **Repository** (`IRepository<TEntity,TKey>` + `Repository<>`, with `MainDbContext`
as the Unit of Work), **Strategy** (`IAiProvider`, `IConnector` — one implementation per
provider/connector type), **Factory** (`IAiProviderFactory`, `IConnectorFactory`, `IAgentFactory`
build the right concrete type from a definition), **Builder** (`IApplicationBuilderService`
turns a finished wizard summary into a `GeneratedApplication` with its default agent/connector
pipeline), **Options pattern** (`AiProvidersOptions`, `HealthMonitoringOptions`,
`DeploymentOptions`), and centralized DI wiring in `Infrastructure/DependencyInjection.cs`.

## Running in Visual Studio 2022

Targets **.NET 9.0**. Requires Visual Studio 2022 17.12+ (or the .NET 9 SDK on the CLI) — VS
will prompt to install the .NET 9 SDK/workload on first open if it isn't present.

1. Open `MultiAgentPlatform.sln`.
2. Restore NuGet packages (VS does this automatically on load; all package versions are pinned
   to stable 9.0.x releases — no preview/deprecated packages are used).
3. **No EF Core migrations are checked in** (this environment had no `dotnet` SDK available to
   run `dotnet ef migrations add`, so none could be generated). `Program.cs` handles this by
   calling `Database.Migrate()` and falling back to `Database.EnsureCreated()` when no
   migrations exist — the app will create `App_Data/mainapp.db` automatically on first run.
   Once you have the SDK locally, you can and should add real migrations:
   ```
   dotnet tool install --global dotnet-ef   # first time only
   dotnet ef migrations add InitialCreate --project src/MultiAgentPlatform.Infrastructure --startup-project src/MultiAgentPlatform.Web
   dotnet ef database update --project src/MultiAgentPlatform.Infrastructure --startup-project src/MultiAgentPlatform.Web
   ```
4. Set **MultiAgentPlatform.Web** as the startup project and press F5.
5. On first run the app seeds five roles (`PlatformAdministrator`, `ApplicationOwner`,
   `ApplicationDeveloper`, `ApplicationUser`, `ReadOnlyAuditor`) and one admin user:
   `admin@platform.local` / `ChangeMe!123` (override the password via `Seed:AdminPassword` in
   config — **change it before any non-local use**).
6. Log in, open **Chat**, and describe the application you want. The wizard asks one topic at
   a time (business process, inputs, outputs, business rules, agents, users/roles, deployment,
   health monitoring, UI, AI provider) and shows a running summary; replying `confirm` generates
   the application (default 8-agent pipeline + inferred connectors) and switches the right-hand
   panel from "no preview yet" to a schema-driven live preview of its UI. **Dashboard** and
   **Applications** show fleet-wide and per-app metrics, health state, and history.

## Configuring an AI provider

No key is hard-coded anywhere. Configure one (or more) provider under the `AiProviders` section
of `appsettings.json`, or — preferred for real keys — via environment variables or
`dotnet user-secrets`:

```
dotnet user-secrets set "AiProviders:OpenAi:ApiKey" "sk-..."          --project src/MultiAgentPlatform.Web
dotnet user-secrets set "AiProviders:Anthropic:ApiKey" "sk-ant-..."   --project src/MultiAgentPlatform.Web
```

or as Docker/OS environment variables: `AiProviders__OpenAi__ApiKey`,
`AiProviders__AzureOpenAi__ApiKey`/`Endpoint`/`Deployment`, `AiProviders__Anthropic__ApiKey`.
With no key configured, the wizard still works end-to-end (it stores each answer verbatim
instead of LLM-normalizing it) — this is a deliberate graceful degradation, not a bug.

## Running with Docker

```
docker compose up --build
```

This builds and runs three containers: `mainapp` (this platform, port **5000**), `sampleapp`
(the reference generated app, port **5001**), and an `nginx` reverse proxy (port **80**) using
`deploy/nginx/nginx.conf.sample`, which documents wildcard-DNS + per-slug subdomain routing and
TLS termination for a real deployment, versus this sample's static two-host config. For local
development without Docker, each generated app is simply reached at `https://localhost:{port}`
(see `Deployment` options in `appsettings.json` for the port range a real deployment step would
allocate from).

## The reference generated application

`samples/SampleInvoiceProcessor` is a **fully standalone** app — it has no project or runtime
reference to the Main Application, only a best-effort outbound heartbeat POST (fails gracefully
if the platform is unreachable, per the spec's independence requirement). It has its own SQLite
database (`App_Data/invoices.db`), its own fixed 7-agent pipeline (Orchestrator → Input
Connector reading `./incoming/*.json` → Validation → Processing (tax/total calculation) →
Output Connector (DB insert + `./outgoing/` summary) → Error-Handling → Monitoring), a
`/health/live` and `/health/ready` endpoint pair, a background poller that runs the pipeline
every 30s, a background heartbeat sender, and a tiny Bootstrap status page at `/` with a
"run pipeline now" button. Run it standalone with `dotnet run --project samples/SampleInvoiceProcessor`
and drop a JSON file like `{"vendorName":"Acme","invoiceNumber":"INV-1","amount":100}` into its
`incoming/` folder.

## What's implemented vs. simplified for this MVP pass

This was built end-to-end in one pass, without access to a .NET SDK to compile or run it here —
every file was hand-written and cross-checked against the actual interfaces/entities on disk,
but **you should do a first `dotnet build` in Visual Studio and expect to fix a small number of
compile errors** before it runs (namespace glitches, an occasional missing `using`).

Implemented for real: the full wizard→build→pipeline flow, all 8 agent roles and 5 connector
types (Api/File/Database/Webhook/Manual — Email connectors fall back to a manual no-op stub,
flagged in code), 3 AI provider adapters (OpenAI-compatible, Azure OpenAI, Anthropic) behind one
abstraction, SignalR chat, schema-driven (non-code-executing) live preview, ASP.NET Identity
with the 5 specified roles, the health-ingest endpoint + dashboard metrics/filters, per-app
drill-down, audit/execution/deployment logging tables, and a fully standalone Dockerized sample
app with its own DB and health/heartbeat story.

Deliberately simplified rather than left unfinished: EF Core migrations (use `EnsureCreated()`
until you generate real migrations locally, see above), the "Processing"/"Decision-Making"
agents run generic pass-through logic driven by their configured `SystemInstructions` text
rather than dynamically-generated per-app business logic (arbitrary code generation/execution
was intentionally avoided per the spec's security requirement — a real implementation would
likely route these through the configured AI provider with the instructions as a system prompt,
which the abstraction already supports), and the heartbeat-ingest endpoint has no per-app shared
secret yet (flagged inline — add one before exposing it publicly).
