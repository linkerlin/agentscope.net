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

namespace AgentScope.Core.Tool
{
    /// <summary>
    /// Skill tool group: groups tools logically by skill, supporting dynamic activation/deactivation.
    /// 技能工具组：按技能领域对工具进行逻辑分组，支持动态激活/禁用。
    /// </summary>
    public class SkillToolGroup
    {
        /// <summary>
        /// Group name, used as the unique identifier for this skill group.
        /// 组名称，用作此技能组的唯一标识。
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Whether this group is currently active. Inactive groups' tools will not be available.
        /// 当前组是否激活。非激活组的工具将不可用。
        /// </summary>
        public bool IsActive { get; set; }

        /// <summary>
        /// List of tools belonging to this skill group.
        /// 属于此技能组的工具列表。
        /// </summary>
        public List<ITool> Tools { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="SkillToolGroup"/> class.
        /// 初始化 <see cref="SkillToolGroup"/> 类的新实例。
        /// </summary>
        /// <param name="name">Group name / 组名称</param>
        /// <param name="tools">Initial tool collection / 初始工具集合</param>
        /// <param name="isActive">Whether the group is active on creation / 创建时是否激活</param>
        /// <exception cref="ArgumentNullException">Thrown when name is null / 名称为 null 时抛出</exception>
        public SkillToolGroup(string name, IEnumerable<ITool> tools, bool isActive = true)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Tools = tools?.ToList() ?? new List<ITool>();
            IsActive = isActive;
        }
    }
}
