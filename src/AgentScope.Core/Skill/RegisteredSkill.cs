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

namespace AgentScope.Core.Skill;

/// <summary>
/// Skill registration metadata: an entry obtained from repository scanning, not yet loaded as an ISkill.
/// Skill 注册元数据：仓库扫描得到的条目，尚未加载为 ISkill。
/// Corresponds to Java: io.agentscope.core.skill.RegisteredSkill
/// </summary>
public class RegisteredSkill
{
    /// <summary>
    /// Unique identifier for the skill.
    /// 技能的唯一标识符。
    /// </summary>
    public string Id { get; set; } = "";

    /// <summary>
    /// Display name of the skill.
    /// 技能的显示名称。
    /// </summary>
    public string Name { get; set; } = "";

    /// <summary>
    /// Description of the skill's purpose and functionality.
    /// 技能用途和功能的描述。
    /// </summary>
    public string Description { get; set; } = "";

    /// <summary>
    /// Names of the tools associated with this skill.
    /// 与此技能关联的工具名称列表。
    /// </summary>
    public List<string> ToolNames { get; set; } = new();

    /// <summary>
    /// Whether this skill is active by default after loading.
    /// 加载后是否默认激活此技能。
    /// </summary>
    public bool IsActiveByDefault { get; set; } = true;

    /// <summary>
    /// Source file path from which this skill was loaded (optional).
    /// 加载此技能的源文件路径（可选）。
    /// </summary>
    public string? SourcePath { get; set; }

    /// <summary>
    /// Raw content of the skill definition (e.g., Markdown content).
    /// 技能定义的原始内容（例如 Markdown 内容）。
    /// </summary>
    public string? RawContent { get; set; }
}
