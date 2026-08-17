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
/// 技能过滤器：在加载/激活前按名称、ID、标签、来源等条件筛选已注册技能。
/// 对应 Java: io.agentscope.core.skill.SkillFilter
/// </summary>
public class SkillFilter
{
    /// <summary>包含的技能名（为空表示不按名过滤）。</summary>
    public ISet<string>? IncludeNames { get; set; }

    /// <summary>排除的技能名。</summary>
    public ISet<string>? ExcludeNames { get; set; }

    /// <summary>包含的技能 ID。</summary>
    public ISet<string>? IncludeIds { get; set; }

    /// <summary>来源路径前缀（如指定目录下）。</summary>
    public string? SourcePathPrefix { get; set; }

    /// <summary>是否仅保留默认激活的技能。</summary>
    public bool? ActiveByDefault { get; set; }

    /// <summary>应用过滤器。</summary>
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
