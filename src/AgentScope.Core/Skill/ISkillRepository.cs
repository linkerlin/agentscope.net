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
/// Skill repository: scans to obtain a list of RegisteredSkill entries and loads them as ISkill on demand.
/// Skill 仓库：扫描得到 RegisteredSkill 列表，并按需加载为 ISkill。
/// Corresponds to Java: io.agentscope.core.skill.ISkillRepository
/// </summary>
public interface ISkillRepository
{
    IEnumerable<RegisteredSkill> Scan();
    ISkill Load(RegisteredSkill registered);
}
