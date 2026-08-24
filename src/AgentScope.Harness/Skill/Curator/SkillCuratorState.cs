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

/// <summary>Curator 持久化状态，对应 Java SkillCuratorState</summary>
public sealed record SkillCuratorState
{
    public DateTime? LastRunAt { get; init; }
    public int RunCount { get; init; }
    public bool Paused { get; init; }
    public string? LastRunSummary { get; init; }
    public int LastRunDurationSeconds { get; init; }
}
