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

using System.Threading.Tasks;
using AgentScope.Core.Agent;

namespace AgentScope.Core.Skill;

/// <summary>
/// 动态技能中间件：在系统提示词阶段注入当前可用技能说明，并（可选）触发技能加载。
/// 对应 Java: io.agentscope.core.skill.DynamicSkillMiddleware
/// </summary>
public class DynamicSkillMiddleware : MiddlewareBase
{
    private readonly AgentSkillPromptProvider _promptProvider;
    private readonly bool _onlyActive;

    public DynamicSkillMiddleware(AgentSkillPromptProvider promptProvider, bool onlyActive = true)
    {
        _promptProvider = promptProvider ?? throw new System.ArgumentNullException(nameof(promptProvider));
        _onlyActive = onlyActive;
    }

    /// <inheritdoc />
    public override Task<string> OnSystemPromptAsync(IAgent agent, RuntimeContext ctx, string prompt)
    {
        var section = _promptProvider.BuildSkillPromptSection(_onlyActive);
        if (!string.IsNullOrEmpty(section))
        {
            prompt = string.IsNullOrEmpty(prompt) ? section : prompt + "\n" + section;
        }

        return Task.FromResult(prompt);
    }
}
