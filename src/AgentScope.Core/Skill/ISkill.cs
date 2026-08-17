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
/// Skill interface: a composite functional unit that can contain multiple Tools and supports dynamic activation.
/// Skill 接口：复合功能单元，可包含多个 Tool，支持动态激活。
/// Corresponds to Java: io.agentscope.core.skill.ISkill
/// </summary>
public interface ISkill
{
    string Id { get; }
    string Name { get; }
    string Description { get; }
    IReadOnlyList<ITool> Tools { get; }
    bool IsActive { get; set; }
}
