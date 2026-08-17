using Microsoft.EntityFrameworkCore;
using AgentScope.Client.Models;

namespace AgentScope.Client.Services;

public class SkillConfigService
{
    private readonly IDbContextFactory<AppDbContext> _factory;

    public SkillConfigService(IDbContextFactory<AppDbContext> factory)
    {
        _factory = factory;
    }

    public async Task<List<SkillConfig>> GetAllAsync()
    {
        await using var db = await _factory.CreateDbContextAsync();
        return await db.SkillConfigs.OrderBy(s => s.Name).ToListAsync();
    }

    public async Task<SkillConfig?> GetAsync(Guid id)
    {
        await using var db = await _factory.CreateDbContextAsync();
        return await db.SkillConfigs.FindAsync(id);
    }

    public async Task SaveAsync(SkillConfig config)
    {
        await using var db = await _factory.CreateDbContextAsync();
        var existing = await db.SkillConfigs.FindAsync(config.Id);
        if (existing != null)
        {
            db.Entry(existing).CurrentValues.SetValues(config);
        }
        else
        {
            db.SkillConfigs.Add(config);
        }
        await db.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        await using var db = await _factory.CreateDbContextAsync();
        var item = await db.SkillConfigs.FindAsync(id);
        if (item != null)
        {
            db.SkillConfigs.Remove(item);
            await db.SaveChangesAsync();
        }
    }
}
