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
/// Skill lifecycle hook providing extension points before/after skill loading, activation, deactivation, and tool invocation.
/// 技能生命周期钩子：在技能加载/激活/卸载/调用前后提供扩展点。
/// Corresponds to Java: io.agentscope.core.skill.SkillHook
/// </summary>
public interface ISkillHook
{
    /// <summary>
    /// Called before a skill is loaded. Return false to prevent loading.
    /// 技能被加载前调用，返回 false 可阻止加载。
    /// </summary>
    Task<bool> OnBeforeLoadAsync(RegisteredSkill skill);

    /// <summary>
    /// Called after a skill is loaded.
    /// 技能被加载后调用。
    /// </summary>
    Task OnAfterLoadAsync(ISkill skill);

    /// <summary>
    /// Called before a skill is activated. Return false to prevent activation.
    /// 技能激活前调用，返回 false 可阻止激活。
    /// </summary>
    Task<bool> OnBeforeActivateAsync(ISkill skill);

    /// <summary>
    /// Called after a skill is deactivated/unloaded.
    /// 技能卸载/停用后调用。
    /// </summary>
    Task OnAfterDeactivateAsync(ISkill skill);

    /// <summary>
    /// Called before a tool within a skill is invoked. Return false to prevent invocation.
    /// 技能内工具被调用前调用，返回 false 可阻止调用。
    /// </summary>
    Task<bool> OnBeforeToolInvokeAsync(ISkill skill, ITool tool, Dictionary<string, object> arguments);
}

/// <summary>
/// Default no-op implementation of ISkillHook that allows all operations.
/// Subclasses can override individual methods as needed.
/// 默认空实现（放行所有操作），便于子类按需重写单个方法。
/// </summary>
public class SkillHookBase : ISkillHook
{
    /// <summary>
    /// Called before a skill is loaded. Default allows loading.
    /// 技能被加载前调用。默认允许加载。
    /// </summary>
    public virtual Task<bool> OnBeforeLoadAsync(RegisteredSkill skill) => Task.FromResult(true);

    /// <summary>
    /// Called after a skill is loaded. Default does nothing.
    /// 技能被加载后调用。默认无操作。
    /// </summary>
    public virtual Task OnAfterLoadAsync(ISkill skill) => Task.CompletedTask;

    /// <summary>
    /// Called before a skill is activated. Default allows activation.
    /// 技能激活前调用。默认允许激活。
    /// </summary>
    public virtual Task<bool> OnBeforeActivateAsync(ISkill skill) => Task.FromResult(true);

    /// <summary>
    /// Called after a skill is deactivated. Default does nothing.
    /// 技能卸载/停用后调用。默认无操作。
    /// </summary>
    public virtual Task OnAfterDeactivateAsync(ISkill skill) => Task.CompletedTask;

    /// <summary>
    /// Called before a tool within a skill is invoked. Default allows invocation.
    /// 技能内工具被调用前调用。默认允许调用。
    /// </summary>
    public virtual Task<bool> OnBeforeToolInvokeAsync(ISkill skill, ITool tool, Dictionary<string, object> arguments)
        => Task.FromResult(true);
}
