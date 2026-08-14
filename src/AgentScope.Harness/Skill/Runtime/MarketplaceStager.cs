namespace AgentScope.Harness.Skill.Runtime;

/// <summary>将非 workspace 的 skill 资源物化到缓存目录，对应 Java MarketplaceStager</summary>
public sealed class MarketplaceStager
{
    private readonly string _cacheDir;

    public MarketplaceStager(string? cacheDir = null)
    {
        _cacheDir = cacheDir ?? Path.Combine(
            Environment.CurrentDirectory, ".skills-cache");
    }

    public async Task<Dictionary<string, StageResult>> StageAsync(
        IEnumerable<HarnessSkillEntry> entries)
    {
        var results = new Dictionary<string, StageResult>();
        foreach (var entry in entries)
        {
            if (entry.Resources?.Files == null)
            {
                results[entry.SkillId] = StageResult.None;
                continue;
            }

            var skillDir = Path.Combine(_cacheDir, entry.SkillId);
            Directory.CreateDirectory(skillDir);

            foreach (var (path, content) in entry.Resources.Files)
            {
                var fullPath = Path.Combine(skillDir, path);
                Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
                await File.WriteAllTextAsync(fullPath, content);
            }

            results[entry.SkillId] = StageResult.Cached(entry.SkillId);
        }

        return results;
    }

    public void InvalidateAll()
    {
        if (Directory.Exists(_cacheDir))
            Directory.Delete(_cacheDir, recursive: true);
    }

    public abstract record StageResult
    {
        public static StageResult None => new NoneResult();
        public static StageResult Cached(string skillId) => new CachedResult(skillId);
    }
    private sealed record NoneResult : StageResult;
    private sealed record CachedResult(string SkillId) : StageResult;
}
