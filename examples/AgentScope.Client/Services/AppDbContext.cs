using Microsoft.EntityFrameworkCore;
using AgentScope.Client.Models;

namespace AgentScope.Client.Services;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<AgentConfig> AgentConfigs => Set<AgentConfig>();
    public DbSet<LlmConfig> LlmConfigs => Set<LlmConfig>();
    public DbSet<ChatSession> ChatSessions => Set<ChatSession>();
    public DbSet<ChatMessage> ChatMessages => Set<ChatMessage>();
    public DbSet<McpConfig> McpConfigs => Set<McpConfig>();
    public DbSet<SkillConfig> SkillConfigs => Set<SkillConfig>();

    protected override void OnModelCreating(ModelBuilder model)
    {
        model.Entity<AgentConfig>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).IsRequired().HasMaxLength(200);
        });

        model.Entity<LlmConfig>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).IsRequired().HasMaxLength(200);
        });

        model.Entity<ChatSession>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasOne<AgentConfig>().WithMany().HasForeignKey(x => x.AgentConfigId);
        });

        model.Entity<ChatMessage>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasOne<ChatSession>().WithMany().HasForeignKey(x => x.SessionId);
            e.HasIndex(x => x.SessionId);
        });

        model.Entity<McpConfig>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).IsRequired().HasMaxLength(200);
        });

        model.Entity<SkillConfig>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).IsRequired().HasMaxLength(200);
        });
    }
}
