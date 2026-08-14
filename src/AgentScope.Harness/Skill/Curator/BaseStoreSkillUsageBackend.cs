// Copyright 2024-2026 the original author or authors.
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
using AgentScope.Harness.Filesystem.Remote;

namespace AgentScope.Harness.Skill.Curator;

/// <summary>
/// 基于远程 KV 存储（IBaseStore）的技能使用记录后端基类。
/// 子类只需提供按命名空间隔离的 IBaseStore 即可获得持久化能力。
/// 对应 Java: io.agentscope.harness.agent.skill.curator.BaseStoreSkillUsageBackend
/// </summary>
public abstract class BaseStoreSkillUsageBackend : ISkillUsageBackend
{
    private readonly IBaseStore _store;
    private readonly string _key;
    private readonly SemaphoreSlim _lock = new(1, 1);

    protected BaseStoreSkillUsageBackend(IBaseStore store, string key = "skill-usage")
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _key = key;
    }

    /// <inheritdoc />
    public Dictionary<string, SkillUsageRecord> LoadAll()
    {
        var json = _store.GetAsync(_key).GetAwaiter().GetResult();
        if (string.IsNullOrEmpty(json)) return new();
        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, SkillUsageRecord>>(json) ?? new();
        }
        catch
        {
            return new();
        }
    }

    /// <inheritdoc />
    public SkillUsageRecord? Get(string skillId)
    {
        return LoadAll().TryGetValue(skillId, out var r) ? r : null;
    }

    /// <inheritdoc />
    public void Mutate(string skillId, Func<SkillUsageRecord, SkillUsageRecord> mutator)
    {
        _lock.Wait();
        try
        {
            var all = LoadAll();
            all[skillId] = mutator(all.GetValueOrDefault(skillId) ?? new SkillUsageRecord { SkillId = skillId });
            _store.SetAsync(_key, JsonSerializer.Serialize(all)).GetAwaiter().GetResult();
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc />
    public void ReplaceAll(Dictionary<string, SkillUsageRecord> records)
    {
        _lock.Wait();
        try
        {
            _store.SetAsync(_key, JsonSerializer.Serialize(records)).GetAwaiter().GetResult();
        }
        finally
        {
            _lock.Release();
        }
    }
}
