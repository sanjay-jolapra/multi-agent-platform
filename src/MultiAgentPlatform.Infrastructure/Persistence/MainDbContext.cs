using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using MultiAgentPlatform.Domain.Entities;
using MultiAgentPlatform.Infrastructure.Identity;

namespace MultiAgentPlatform.Infrastructure.Persistence;

public class MainDbContext : IdentityDbContext<ApplicationUser>
{
    public MainDbContext(DbContextOptions<MainDbContext> options) : base(options)
    {
    }

    public DbSet<GeneratedApplication> GeneratedApplications => Set<GeneratedApplication>();
    public DbSet<AgentDefinition> AgentDefinitions => Set<AgentDefinition>();
    public DbSet<ConnectorDefinition> ConnectorDefinitions => Set<ConnectorDefinition>();
    public DbSet<AgentExecutionLog> AgentExecutionLogs => Set<AgentExecutionLog>();
    public DbSet<ConnectorExecutionLog> ConnectorExecutionLogs => Set<ConnectorExecutionLog>();
    public DbSet<Conversation> Conversations => Set<Conversation>();
    public DbSet<ChatMessage> ChatMessages => Set<ChatMessage>();
    public DbSet<HealthCheckRecord> HealthCheckRecords => Set<HealthCheckRecord>();
    public DbSet<DeploymentLog> DeploymentLogs => Set<DeploymentLog>();
    public DbSet<AuditLogEntry> AuditLogEntries => Set<AuditLogEntry>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<GeneratedApplication>(e =>
        {
            e.HasIndex(a => a.Slug).IsUnique();

            e.HasMany(a => a.Agents)
                .WithOne()
                .HasForeignKey(x => x.GeneratedApplicationId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasMany(a => a.Connectors)
                .WithOne()
                .HasForeignKey(x => x.GeneratedApplicationId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<AgentExecutionLog>(e =>
        {
            e.HasIndex(x => x.GeneratedApplicationId);
            e.HasIndex(x => x.CorrelationId);
        });

        builder.Entity<ConnectorExecutionLog>(e =>
        {
            e.HasIndex(x => x.GeneratedApplicationId);
            e.HasIndex(x => x.CorrelationId);
        });

        builder.Entity<Conversation>(e =>
        {
            e.HasMany(c => c.Messages)
                .WithOne()
                .HasForeignKey(m => m.ConversationId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<ChatMessage>(e =>
        {
            e.HasIndex(x => x.ConversationId);
        });

        builder.Entity<HealthCheckRecord>(e =>
        {
            e.HasIndex(x => x.GeneratedApplicationId);
        });

        builder.Entity<DeploymentLog>(e =>
        {
            e.HasIndex(x => x.GeneratedApplicationId);
        });

        builder.Entity<AuditLogEntry>(e =>
        {
            e.HasIndex(x => x.CorrelationId);
        });
    }
}
