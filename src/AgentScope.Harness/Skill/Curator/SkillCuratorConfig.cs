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

/// <summary>SkillCurator 配置，对应 Java SkillCuratorConfig</summary>
public sealed record SkillCuratorConfig
{
    public UmbrellaPassMode UmbrellaMode { get; init; } = UmbrellaPassMode.Disabled;
    public TimeSpan RunInterval { get; init; } = TimeSpan.FromHours(6);
    public TimeSpan DraftTimeout { get; init; } = TimeSpan.FromDays(7);
    public TimeSpan StaleTimeout { get; init; } = TimeSpan.FromDays(30);

    public enum UmbrellaPassMode
    {
        Disabled,
        DryRunOnly
    }
}
