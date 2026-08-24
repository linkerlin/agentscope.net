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

/// <summary>Skill 使用记录遥测，对应 Java SkillUsageRecord</summary>
public sealed record SkillUsageRecord
{
    public string SkillId { get; init; } = "";
    public SkillState State { get; init; } = SkillState.Active;
    public int ViewCount { get; init; }
    public int UseCount { get; init; }
    public int PatchCount { get; init; }
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public DateTime? LastUsedAt { get; init; }
    public bool IsAgentCreated { get; init; }
    public string? CreatedBySessionId { get; init; }

    public DateTime LatestActivityAt => LastUsedAt ?? CreatedAt;
    public int ActivityCount => ViewCount + UseCount + PatchCount;
}

public enum SkillState
{
    Draft,
    Active,
    Stale,
    Archived
}
