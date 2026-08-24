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

using AgentScope.Core.Tool;

namespace AgentScope.Core.Skill;

/// <summary>
/// A skill implementation loaded from a Markdown file, containing metadata and associated tools.
/// 从 Markdown 文件加载的技能实现，包含元数据和关联的工具。
/// Corresponds to Java: io.agentscope.core.skill.MarkdownSkill
/// </summary>
public class MarkdownSkill : ISkill
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MarkdownSkill"/> class.
    /// 初始化 <see cref="MarkdownSkill"/> 类的新实例。
    /// </summary>
    /// <param name="registeredSkill">The registration metadata. / 注册元数据。</param>
    /// <param name="tools">The tools associated with this skill. / 与此技能关联的工具。</param>
    /// <param name="isActive">Whether the skill is active by default. / 是否默认激活。</param>
    /// <exception cref="ArgumentNullException">Thrown when registeredSkill is null. / 当 registeredSkill 为 null 时抛出。</exception>
    public MarkdownSkill(RegisteredSkill registeredSkill, IReadOnlyList<ITool>? tools = null, bool isActive = true)
    {
        if (registeredSkill == null)
            throw new ArgumentNullException(nameof(registeredSkill));

        Id = registeredSkill.Id;
        Name = registeredSkill.Name;
        Description = registeredSkill.Description;
        SourcePath = registeredSkill.SourcePath;
        RawContent = registeredSkill.RawContent ?? string.Empty;
        ToolNames = registeredSkill.ToolNames.AsReadOnly();
        Tools = tools ?? Array.Empty<ITool>();
        IsActive = isActive;
    }

    /// <summary>
    /// Unique identifier for the skill.
    /// 技能的唯一标识符。
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// Display name of the skill.
    /// 技能的显示名称。
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Description of the skill's purpose and functionality.
    /// 技能用途和功能的描述。
    /// </summary>
    public string Description { get; }

    /// <summary>
    /// Names of the tools referenced by this skill (read-only).
    /// 此技能引用的工具名称列表（只读）。
    /// </summary>
    public IReadOnlyList<string> ToolNames { get; }

    /// <summary>
    /// The tools associated with this skill.
    /// 与此技能关联的工具实例。
    /// </summary>
    public IReadOnlyList<ITool> Tools { get; }

    /// <summary>
    /// Whether this skill is currently active.
    /// 此技能当前是否处于激活状态。
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Source file path from which this skill was loaded.
    /// 加载此技能的源文件路径。
    /// </summary>
    public string? SourcePath { get; }

    /// <summary>
    /// Raw Markdown content of the skill definition.
    /// 技能定义的原始 Markdown 内容。
    /// </summary>
    public string RawContent { get; }
}
