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

using System.Threading.Tasks;
using AgentScope.Core.Tool;

namespace AgentScope.Core.Skill;

/// <summary>
/// 技能生命周期钩子：在技能加载/激活/卸载/调用前后提供扩展点。
/// 对应 Java: io.agentscope.core.skill.SkillHook
/// </summary>
public interface ISkillHook
{
    /// <summary>技能被加载前调用，返回 false 可阻止加载。</summary>
    Task<bool> OnBeforeLoadAsync(RegisteredSkill skill);

    /// <summary>技能被加载后调用。</summary>
    Task OnAfterLoadAsync(ISkill skill);

    /// <summary>技能激活前调用，返回 false 可阻止激活。</summary>
    Task<bool> OnBeforeActivateAsync(ISkill skill);

    /// <summary>技能卸载/停用后调用。</summary>
    Task OnAfterDeactivateAsync(ISkill skill);

    /// <summary>技能内工具被调用前调用，返回 false 可阻止调用。</summary>
    Task<bool> OnBeforeToolInvokeAsync(ISkill skill, ITool tool, Dictionary<string, object> arguments);
}

/// <summary>
/// 默认空实现（放行所有操作），便于子类按需重写单个方法。
/// </summary>
public class SkillHookBase : ISkillHook
{
    public virtual Task<bool> OnBeforeLoadAsync(RegisteredSkill skill) => Task.FromResult(true);
    public virtual Task OnAfterLoadAsync(ISkill skill) => Task.CompletedTask;
    public virtual Task<bool> OnBeforeActivateAsync(ISkill skill) => Task.FromResult(true);
    public virtual Task OnAfterDeactivateAsync(ISkill skill) => Task.CompletedTask;
    public virtual Task<bool> OnBeforeToolInvokeAsync(ISkill skill, ITool tool, Dictionary<string, object> arguments)
        => Task.FromResult(true);
}
