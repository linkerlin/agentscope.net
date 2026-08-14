namespace AgentScope.Harness.Skill.Runtime;

/// <summary>不可变的 skill 快照，按 skillId 索引，对应 Java SkillCatalog</summary>
public sealed class SkillCatalog
{
    private readonly Dictionary<string, HarnessSkillEntry> _entries;

    private SkillCatalog(Dictionary<string, HarnessSkillEntry> entries)
    {
        _entries = entries;
    }

    public static SkillCatalog Empty => new(new());

    public static SkillCatalog Of(IEnumerable<HarnessSkillEntry> entries)
    {
        var dict = entries.ToDictionary(e => e.SkillId, e => e);
        return new SkillCatalog(dict);
    }

    public HarnessSkillEntry? Get(string skillId) =>
        _entries.TryGetValue(skillId, out var e) ? e : null;

    public IReadOnlyCollection<HarnessSkillEntry> All => _entries.Values;

    public IReadOnlyCollection<string> Ids => _entries.Keys;

    public bool IsEmpty => _entries.Count == 0;

    public int Size => _entries.Count;
}
