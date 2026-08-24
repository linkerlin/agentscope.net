using Microsoft.EntityFrameworkCore;
using AgentScope.Client.Models;

namespace AgentScope.Client.Services;

public class McpConfigService
{
    private readonly IDbContextFactory<AppDbContext> _factory;

    public McpConfigService(IDbContextFactory<AppDbContext> factory)
    {
        _factory = factory;
    }

    public async Task<List<McpConfig>> GetAllAsync()
    {
        await using var db = await _factory.CreateDbContextAsync();
        return await db.McpConfigs.OrderBy(m => m.Name).ToListAsync();
    }

    public async Task<McpConfig?> GetAsync(Guid id)
    {
        await using var db = await _factory.CreateDbContextAsync();
        return await db.McpConfigs.FindAsync(id);
    }

    public async Task SaveAsync(McpConfig config)
    {
        await using var db = await _factory.CreateDbContextAsync();
        var existing = await db.McpConfigs.FindAsync(config.Id);
        if (existing != null)
        {
            db.Entry(existing).CurrentValues.SetValues(config);
        }
        else
        {
            db.McpConfigs.Add(config);
        }
        await db.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        await using var db = await _factory.CreateDbContextAsync();
        var item = await db.McpConfigs.FindAsync(id);
        if (item != null)
        {
            db.McpConfigs.Remove(item);
            await db.SaveChangesAsync();
        }
    }
}
