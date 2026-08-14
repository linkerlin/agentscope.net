using System.Collections.Concurrent;
using System.Text.Json;

namespace AgentScope.Harness.Skill.Curator;

/// <summary>Skill 使用记录持久化后端 SPI，对应 Java SkillUsageBackend</summary>
public interface ISkillUsageBackend
{
    Dictionary<string, SkillUsageRecord> LoadAll();
    SkillUsageRecord? Get(string skillId);
    void Mutate(string skillId, Func<SkillUsageRecord, SkillUsageRecord> mutator);
    void ReplaceAll(Dictionary<string, SkillUsageRecord> records);
}

/// <summary>基于文件系统的 JSON 后端</summary>
public sealed class FilesystemSkillUsageBackend : ISkillUsageBackend
{
    private readonly string _filePath;
    private readonly object _lock = new();

    public FilesystemSkillUsageBackend(string filePath)
    {
        _filePath = filePath;
    }

    public Dictionary<string, SkillUsageRecord> LoadAll()
    {
        if (!File.Exists(_filePath)) return new();
        lock (_lock)
        {
            var json = File.ReadAllText(_filePath);
            return JsonSerializer.Deserialize<Dictionary<string, SkillUsageRecord>>(json)
                ?? new();
        }
    }

    public SkillUsageRecord? Get(string skillId)
    {
        var all = LoadAll();
        return all.TryGetValue(skillId, out var r) ? r : null;
    }

    public void Mutate(string skillId, Func<SkillUsageRecord, SkillUsageRecord> mutator)
    {
        lock (_lock)
        {
            var all = LoadAll();
            all[skillId] = mutator(all.GetValueOrDefault(skillId)
                ?? new SkillUsageRecord { SkillId = skillId });
            File.WriteAllText(_filePath, JsonSerializer.Serialize(all));
        }
    }

    public void ReplaceAll(Dictionary<string, SkillUsageRecord> records)
    {
        lock (_lock)
        {
            File.WriteAllText(_filePath, JsonSerializer.Serialize(records));
        }
    }
}

/// <summary>基于内存的并发后端</summary>
public sealed class InMemorySkillUsageBackend : ISkillUsageBackend
{
    private readonly ConcurrentDictionary<string, SkillUsageRecord> _store = new();

    public Dictionary<string, SkillUsageRecord> LoadAll() => _store.ToDictionary(k => k.Key, v => v.Value);

    public SkillUsageRecord? Get(string skillId) =>
        _store.TryGetValue(skillId, out var r) ? r : null;

    public void Mutate(string skillId, Func<SkillUsageRecord, SkillUsageRecord> mutator)
    {
        _store.AddOrUpdate(skillId,
            _ => mutator(new SkillUsageRecord { SkillId = skillId }),
            (_, existing) => mutator(existing));
    }

    public void ReplaceAll(Dictionary<string, SkillUsageRecord> records)
    {
        _store.Clear();
        foreach (var (k, v) in records) _store[k] = v;
    }
}
