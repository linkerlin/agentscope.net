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

/// <summary>
/// Configuration for <see cref="SkillCurator"/>.
/// <see cref="SkillCurator"/> 的配置。
/// </summary>
public sealed record SkillCuratorConfig
{
    /// <summary>
    /// Umbrella pass mode (bypass for testing).
    /// 伞形通过模式（用于测试绕过）。
    /// </summary>
    public UmbrellaPassMode UmbrellaMode { get; init; } = UmbrellaPassMode.Disabled;

    /// <summary>
    /// Interval between curation runs.
    /// 整理运行间隔。
    /// </summary>
    public TimeSpan RunInterval { get; init; } = TimeSpan.FromHours(6);

    /// <summary>
    /// Time before a Draft skill auto-transitions to Active.
    /// Draft 技能自动转为 Active 的超时时间。
    /// </summary>
    public TimeSpan DraftTimeout { get; init; } = TimeSpan.FromDays(7);

    /// <summary>
    /// Time of inactivity before an Active skill becomes Stale.
    /// Active 技能转为 Stale 的非活跃超时时间。
    /// </summary>
    public TimeSpan StaleTimeout { get; init; } = TimeSpan.FromDays(30);

    /// <summary>
    /// Umbrella pass mode for testing purposes.
    /// 用于测试目的的伞形通过模式。
    /// </summary>
    public enum UmbrellaPassMode
    {
        /// <summary>Disabled / 禁用。</summary>
        Disabled,
        /// <summary>Dry run only, no actual transitions / 仅试运行，不实际过渡。</summary>
        DryRunOnly
    }
}
