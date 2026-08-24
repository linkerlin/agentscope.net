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

using System;
using System.Collections.Generic;
using System.Linq;

namespace AgentScope.Core.Skill;

/// <summary>
/// Filter for selecting skills before loading/activation, based on name, ID, tags, source path, etc.
/// 技能过滤器：在加载/激活前按名称、ID、标签、来源等条件筛选已注册技能。
/// Corresponds to Java: io.agentscope.core.skill.SkillFilter
/// </summary>
public class SkillFilter
{
    /// <summary>
    /// Set of skill names to include (empty means no name-based filtering).
    /// 包含的技能名集合（为空表示不按名称过滤）。
    /// </summary>
    public ISet<string>? IncludeNames { get; set; }

    /// <summary>
    /// Set of skill names to exclude.
    /// 排除的技能名集合。
    /// </summary>
    public ISet<string>? ExcludeNames { get; set; }

    /// <summary>
    /// Set of skill IDs to include.
    /// 包含的技能 ID 集合。
    /// </summary>
    public ISet<string>? IncludeIds { get; set; }

    /// <summary>
    /// Source path prefix filter (e.g., skills under a specific directory).
    /// 来源路径前缀过滤器（如指定目录下的技能）。
    /// </summary>
    public string? SourcePathPrefix { get; set; }

    /// <summary>
    /// Whether to only keep skills that are active by default.
    /// 是否仅保留默认激活的技能。
    /// </summary>
    public bool? ActiveByDefault { get; set; }

    /// <summary>
    /// Applies the filter to a sequence of registered skills.
    /// 对已注册技能序列应用过滤器。
    /// </summary>
    /// <param name="skills">The input skills to filter. / 要过滤的输入技能。</param>
    /// <returns>The filtered skills matching all criteria. / 符合所有条件的过滤后技能。</returns>
    public IEnumerable<RegisteredSkill> Apply(IEnumerable<RegisteredSkill> skills)
    {
        var query = skills ?? System.Array.Empty<RegisteredSkill>();

        if (IncludeNames != null && IncludeNames.Count > 0)
        {
            query = query.Where(s => IncludeNames.Contains(s.Name));
        }

        if (ExcludeNames != null && ExcludeNames.Count > 0)
        {
            query = query.Where(s => !ExcludeNames.Contains(s.Name));
        }

        if (IncludeIds != null && IncludeIds.Count > 0)
        {
            query = query.Where(s => IncludeIds.Contains(s.Id));
        }

        if (!string.IsNullOrEmpty(SourcePathPrefix) && SourcePathPrefix != null)
        {
            query = query.Where(s => s.SourcePath != null &&
                s.SourcePath.StartsWith(SourcePathPrefix, StringComparison.OrdinalIgnoreCase));
        }

        if (ActiveByDefault.HasValue)
        {
            query = query.Where(s => s.IsActiveByDefault == ActiveByDefault.Value);
        }

        return query.ToList();
    }
}
