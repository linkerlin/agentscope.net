namespace AgentScope.Harness.Skill.Curator;

/// <summary>Skill 使用遥测存储，封装 ISkillUsageBackend，对应 Java SkillUsageStore</summary>
public sealed class SkillUsageStore
{
    private readonly ISkillUsageBackend _backend;

    public SkillUsageStore(ISkillUsageBackend backend) => _backend = backend;

    public Dictionary<string, SkillUsageRecord> Load() => _backend.LoadAll();
    public SkillUsageRecord? Get(string skillId) => _backend.Get(skillId);

    public void BumpView(string skillId)
        => _backend.Mutate(skillId, r => r with
        { ViewCount = r.ViewCount + 1, LastUsedAt = DateTime.UtcNow });

    public void BumpUse(string skillId)
        => _backend.Mutate(skillId, r => r with
        { UseCount = r.UseCount + 1, LastUsedAt = DateTime.UtcNow });

    public void SetState(string skillId, SkillState state)
        => _backend.Mutate(skillId, r => r with { State = state });

    public void MarkAgentDraft(string skillId, string sessionId)
        => _backend.Mutate(skillId, r => r with
        {
            State = SkillState.Draft,
            IsAgentCreated = true,
            CreatedBySessionId = sessionId
        });
}
