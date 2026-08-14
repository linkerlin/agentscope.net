using System.Text.Json;

namespace AgentScope.Harness.Skill.Curator;

/// <summary>Skill 生命周期维护器：自动 Draft→Active→Stale→Archived 过渡，对应 Java SkillCurator</summary>
public sealed class SkillCurator
{
    private readonly SkillCuratorConfig _config;
    private readonly SkillUsageStore _usageStore;
    private readonly SkillAuditLog _auditLog;
    private readonly string _statePath;

    public SkillCurator(
        SkillCuratorConfig config,
        SkillUsageStore usageStore,
        SkillAuditLog auditLog,
        string? statePath = null)
    {
        _config = config;
        _usageStore = usageStore;
        _auditLog = auditLog;
        _statePath = statePath ?? Path.Combine(
            Environment.CurrentDirectory, ".skills", ".curator_state.json");
    }

    public bool ShouldRunNow(DateTime now)
    {
        var state = LoadState();
        if (state.Paused) return false;
        return state.LastRunAt == null || now - state.LastRunAt.Value > _config.RunInterval;
    }

    public CuratorRunReport RunOnce(DateTime now)
    {
        var startTime = DateTime.UtcNow;
        var counters = new TransitionCounts();
        var usage = _usageStore.Load();

        foreach (var (skillId, record) in usage)
        {
            var newState = ComputeNextState(record, now);
            if (newState != record.State)
            {
                _usageStore.SetState(skillId, newState);
                switch (newState)
                {
                    case SkillState.Stale: counters.StaleCount++; break;
                    case SkillState.Archived: counters.ArchivedCount++; break;
                }
            }
        }

        var duration = (int)(DateTime.UtcNow - startTime).TotalSeconds;
        var state = new SkillCuratorState
        {
            LastRunAt = now,
            RunCount = (LoadState().RunCount) + 1,
            LastRunSummary = $"Draft→Active: {counters.ActiveCount}, "
                + $"Active→Stale: {counters.StaleCount}, "
                + $"Stale→Archived: {counters.ArchivedCount}",
            LastRunDurationSeconds = duration
        };
        SaveState(state);

        return new CuratorRunReport(counters, duration, state);
    }

    private SkillState ComputeNextState(SkillUsageRecord record, DateTime now)
    {
        if (record.State == SkillState.Draft && record.CreatedAt.Add(_config.DraftTimeout) < now)
            return SkillState.Active;
        if (record.State == SkillState.Active && record.LatestActivityAt.Add(_config.StaleTimeout) < now)
            return SkillState.Stale;
        if (record.State == SkillState.Stale && record.LatestActivityAt.Add(_config.StaleTimeout * 2) < now)
            return SkillState.Archived;
        return record.State;
    }

    private SkillCuratorState LoadState()
    {
        if (!File.Exists(_statePath)) return new();
        try
        {
            var json = File.ReadAllText(_statePath);
            return JsonSerializer.Deserialize<SkillCuratorState>(json) ?? new();
        }
        catch { return new(); }
    }

    private void SaveState(SkillCuratorState state)
    {
        var dir = Path.GetDirectoryName(_statePath);
        if (dir != null) Directory.CreateDirectory(dir);
        File.WriteAllText(_statePath, JsonSerializer.Serialize(state));
    }
}

public sealed record CuratorRunReport(
    TransitionCounts Counters,
    int DurationSeconds,
    SkillCuratorState State);

public sealed record TransitionCounts
{
    public int ActiveCount { get; set; }
    public int StaleCount { get; set; }
    public int ArchivedCount { get; set; }
}
