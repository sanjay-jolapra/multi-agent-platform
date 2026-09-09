using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MultiAgentPlatform.Application.Options;
using MultiAgentPlatform.Application.Services;
using MultiAgentPlatform.Application.Wizard;
using MultiAgentPlatform.Domain.Enums;
using MultiAgentPlatform.Domain.Interfaces;
using MultiAgentPlatform.Infrastructure.AiProviders;
using MultiAgentPlatform.Infrastructure.Connectors;
using MultiAgentPlatform.Infrastructure.Orchestration;
using MultiAgentPlatform.Infrastructure.Persistence;
using AgentFactory = MultiAgentPlatform.Infrastructure.Agents.AgentFactory;

namespace MultiAgentPlatform.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<MainDbContext>(o => o.UseSqlite(configuration.GetConnectionString("MainDb") ?? "Data Source=mainapp.db"));

        services.Configure<AiProvidersOptions>(configuration.GetSection(AiProvidersOptions.SectionName));
        services.Configure<HealthMonitoringOptions>(configuration.GetSection(HealthMonitoringOptions.SectionName));
        services.Configure<DeploymentOptions>(configuration.GetSection(DeploymentOptions.SectionName));

        services.AddHttpClient();

        services.AddScoped<IConnectorFactory, ConnectorFactory>();
        services.AddScoped<AgentFactory>();
        services.AddScoped<IAgentFactory>(sp => sp.GetRequiredService<AgentFactory>());
        services.AddScoped<IAiProviderFactory, AiProviderFactory>();
        services.AddScoped<IAgentOrchestrator, AgentOrchestrator>();
        services.AddScoped<IApplicationBuilderService, ApplicationBuilderService>();

        services.AddScoped(sp =>
        {
            var aiOptions = sp.GetRequiredService<IOptions<AiProvidersOptions>>().Value;
            var defaultKind = !string.IsNullOrEmpty(aiOptions.OpenAi.ApiKey) ? AiProviderKind.OpenAi
                : !string.IsNullOrEmpty(aiOptions.AzureOpenAi.ApiKey) ? AiProviderKind.AzureOpenAi
                : !string.IsNullOrEmpty(aiOptions.Anthropic.ApiKey) ? AiProviderKind.Anthropic
                : AiProviderKind.None;
            return new WizardService(sp.GetRequiredService<IAiProviderFactory>(), defaultKind);
        });
        services.AddScoped<IWizardService>(sp => sp.GetRequiredService<WizardService>());

        services.AddScoped(sp => new HealthEvaluationService(sp.GetRequiredService<IOptions<HealthMonitoringOptions>>().Value));
        services.AddScoped<IHealthEvaluationService>(sp => sp.GetRequiredService<HealthEvaluationService>());

        return services;
    }
}
