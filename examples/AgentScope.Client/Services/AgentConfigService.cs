using Microsoft.EntityFrameworkCore;
using AgentScope.Client.Models;

namespace AgentScope.Client.Services;

public class AgentConfigService
{
    private readonly IDbContextFactory<AppDbContext> _factory;

    public AgentConfigService(IDbContextFactory<AppDbContext> factory)
    {
        _factory = factory;
    }

    public async Task<List<AgentConfig>> GetAllAgentsAsync()
    {
        await using var db = await _factory.CreateDbContextAsync();
        return await db.AgentConfigs.OrderBy(a => a.Name).ToListAsync();
    }

    public async Task<AgentConfig?> GetAgentAsync(Guid id)
    {
        await using var db = await _factory.CreateDbContextAsync();
        return await db.AgentConfigs.FindAsync(id);
    }

    public async Task SaveAgentAsync(AgentConfig agent)
    {
        await using var db = await _factory.CreateDbContextAsync();
        var existing = await db.AgentConfigs.FindAsync(agent.Id);
        if (existing != null)
        {
            db.Entry(existing).CurrentValues.SetValues(agent);
        }
        else
        {
            db.AgentConfigs.Add(agent);
        }
        await db.SaveChangesAsync();
    }

    public async Task DeleteAgentAsync(Guid id)
    {
        await using var db = await _factory.CreateDbContextAsync();
        var agent = await db.AgentConfigs.FindAsync(id);
        if (agent != null)
        {
            db.AgentConfigs.Remove(agent);
            await db.SaveChangesAsync();
        }
    }

    public async Task<List<LlmConfig>> GetAllLlmConfigsAsync()
    {
        await using var db = await _factory.CreateDbContextAsync();
        return await db.LlmConfigs.OrderBy(l => l.Name).ToListAsync();
    }

    public async Task<LlmConfig?> GetDefaultLlmAsync()
    {
        await using var db = await _factory.CreateDbContextAsync();
        return await db.LlmConfigs.FirstOrDefaultAsync(l => l.IsDefault)
               ?? await db.LlmConfigs.FirstOrDefaultAsync();
    }

    public async Task SaveLlmAsync(LlmConfig config)
    {
        await using var db = await _factory.CreateDbContextAsync();
        if (config.IsDefault)
        {
            var defaults = await db.LlmConfigs.Where(l => l.IsDefault && l.Id != config.Id).ToListAsync();
            foreach (var d in defaults) d.IsDefault = false;
        }
        var existing = await db.LlmConfigs.FindAsync(config.Id);
        if (existing != null)
        {
            db.Entry(existing).CurrentValues.SetValues(config);
        }
        else
        {
            db.LlmConfigs.Add(config);
        }
        await db.SaveChangesAsync();
    }

    public async Task DeleteLlmAsync(Guid id)
    {
        await using var db = await _factory.CreateDbContextAsync();
        var llm = await db.LlmConfigs.FindAsync(id);
        if (llm != null)
        {
            db.LlmConfigs.Remove(llm);
            await db.SaveChangesAsync();
        }
    }
}
