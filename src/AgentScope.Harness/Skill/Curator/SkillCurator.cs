// Copyright 2024-2026 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System.Text.Json;

namespace AgentScope.Harness.Skill.Curator;

/// <summary>
/// Skill lifecycle manager: automates the Draft → Active → Stale → Archived state transitions.
/// Skill 生命周期维护器：自动 Draft→Active→Stale→Archived 状态过渡。
/// </summary>
public sealed class SkillCurator
{
    private readonly SkillCuratorConfig _config;
    private readonly SkillUsageStore _usageStore;
    private readonly SkillAuditLog _auditLog;
    private readonly string _statePath;

    /// <summary>
    /// Initializes a new instance of <see cref="SkillCurator"/>.
    /// 初始化 <see cref="SkillCurator"/> 的新实例。
    /// </summary>
    /// <param name="config">Curator configuration / 整理器配置。</param>
    /// <param name="usageStore">Skill usage store / 技能使用存储。</param>
    /// <param name="auditLog">Audit log / 审计日志。</param>
    /// <param name="statePath">Optional custom state file path / 可选的自定义状态文件路径。</param>
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

    /// <summary>
    /// Determines whether the curator should run at the specified time.
    /// 判断整理器是否应在指定时间运行。
    /// </summary>
    /// <param name="now">Current timestamp / 当前时间戳。</param>
    /// <returns>True if the curator should run / 如果应运行则返回 true。</returns>
    public bool ShouldRunNow(DateTime now)
    {
        var state = LoadState();
        if (state.Paused) return false;
        return state.LastRunAt == null || now - state.LastRunAt.Value > _config.RunInterval;
    }

    /// <summary>
    /// Executes one curation cycle: transitions skill states based on usage and timeouts.
    /// 执行一次整理周期：根据使用情况和超时时间过渡技能状态。
    /// </summary>
    /// <param name="now">Current timestamp / 当前时间戳。</param>
    /// <returns>A report of the curation run / 整理运行报告。</returns>
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
