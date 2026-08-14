// Copyright 2024-2026 the original author or authors.
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

using AgentScope.Core.Agent;
using AgentScope.Core.Skill;

namespace AgentScope.Harness.Skill;

/// <summary>
/// 运行时上下文技能仓库：根据 RuntimeContext（如工作区/用户）选择底层仓库委托。
/// 对应 Java: io.agentscope.harness.agent.skill.RuntimeContextSkillRepository
/// </summary>
public class RuntimeContextSkillRepository : ISkillRepository
{
    private readonly Func<RuntimeContext?, ISkillRepository> _selector;

    public RuntimeContextSkillRepository(Func<RuntimeContext?, ISkillRepository> selector)
    {
        _selector = selector ?? throw new ArgumentNullException(nameof(selector));
    }

    /// <inheritdoc />
    public IEnumerable<RegisteredSkill> Scan()
    {
        return _selector(RuntimeContext.Current).Scan();
    }

    /// <inheritdoc />
    public ISkill Load(RegisteredSkill registered)
    {
        return _selector(RuntimeContext.Current).Load(registered);
    }
}
