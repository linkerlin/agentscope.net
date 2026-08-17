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
