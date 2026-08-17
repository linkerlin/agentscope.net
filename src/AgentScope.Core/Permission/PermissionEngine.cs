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
using System.Text.RegularExpressions;

namespace AgentScope.Core.Permission;

/// <summary>
/// Permission mode enumeration controlling the overall permission behavior.
/// 权限模式枚举，控制整体权限行为。
/// Corresponds to Java: io.agentscope.core.permission.PermissionMode
/// 对应 Java: io.agentscope.core.permission.PermissionMode
/// </summary>
public enum PermissionMode
{
    /// <summary>
    /// Default mode - ask for user confirmation when no rules match.
    /// 默认模式 - 无规则匹配时询问用户确认。
    /// </summary>
    Default,

    /// <summary>
    /// Accept edits mode - more permissive for file modification tools.
    /// 接受编辑模式 - 对文件修改工具更宽松。
    /// </summary>
    AcceptEdits,

    /// <summary>
    /// Explore mode - allow read-only operations without confirmation.
    /// 探索模式 - 允许只读操作无需确认。
    /// </summary>
    Explore,

    /// <summary>
    /// Bypass mode - bypass all permission checks (full trust).
    /// 绕过模式 - 绕过所有权限检查（完全信任）。
    /// </summary>
    Bypass,

    /// <summary>
    /// Don't ask mode - automatically allow all operations.
    /// 不询问模式 - 自动允许所有操作。
    /// </summary>
    DontAsk
}

/// <summary>
/// Permission behavior enumeration defining the action to take for a matching rule.
/// 权限行为枚举，定义匹配规则时要采取的操作。
/// Corresponds to Java: io.agentscope.core.permission.PermissionBehavior
/// 对应 Java: io.agentscope.core.permission.PermissionBehavior
/// </summary>
public enum PermissionBehavior
{
    /// <summary>
    /// Allow the operation. 允许操作。
    /// </summary>
    Allow,

    /// <summary>
    /// Deny the operation. 拒绝操作。
    /// </summary>
    Deny,

    /// <summary>
    /// Ask the user for confirmation. 询问用户确认。
    /// </summary>
    Ask,

    /// <summary>
    /// Pass through without decision (let next rule decide).
    /// 透传，不做决策（让下一条规则决定）。
    /// </summary>
    Passthrough
}

/// <summary>
/// A permission rule that maps a wildcard pattern to a behavior.
/// 将通配符模式映射到行为的权限规则。
/// </summary>
/// <param name="Pattern">Wildcard pattern for matching tool names. 用于匹配工具名称的通配符模式。</param>
/// <param name="Behavior">The permission behavior to apply. 要应用的权限行为。</param>
public record PermissionRule(string Pattern, PermissionBehavior Behavior);

/// <summary>
/// The result of a permission evaluation.
/// 权限评估的结果。
/// </summary>
/// <param name="Behavior">The decided permission behavior. 决定的权限行为。</param>
/// <param name="Reason">Human-readable reason for the decision. 决策的人类可读原因。</param>
/// <param name="SuggestedRules">Optional suggested rules for the user to add. 可选的建议用户添加的规则。</param>
/// <param name="UpdatedInput">Optional modified input data. 可选的修改后的输入数据。</param>
public record PermissionDecision(
    PermissionBehavior Behavior,
    string Reason,
    List<string>? SuggestedRules = null,
    Dictionary<string, object>? UpdatedInput = null);

/// <summary>
/// Snapshot of the permission engine's current state for serialization or inspection.
/// 权限引擎当前状态的快照，用于序列化或检查。
/// </summary>
/// <param name="Mode">The current permission mode. 当前权限模式。</param>
/// <param name="WorkingDirectory">The working directory. 工作目录。</param>
/// <param name="AllowRules">List of allow rules. 允许规则列表。</param>
/// <param name="DenyRules">List of deny rules. 拒绝规则列表。</param>
/// <param name="AskRules">List of ask rules. 询问规则列表。</param>
public record PermissionContextState(
    PermissionMode Mode,
    string WorkingDirectory,
    List<PermissionRule> AllowRules,
    List<PermissionRule> DenyRules,
    List<PermissionRule> AskRules);

/// <summary>
/// Represents an additional working directory with its source.
/// 表示一个附加的工作目录及其来源。
/// </summary>
/// <param name="Path">The directory path. 目录路径。</param>
/// <param name="Source">The source of this directory (e.g., config file). 此目录的来源（例如配置文件）。</param>
public record AdditionalWorkingDirectory(string Path, string Source);

/// <summary>
/// Represents a tool call request to be evaluated by the permission engine.
/// 表示要由权限引擎评估的工具调用请求。
/// </summary>
public class ToolCallRequest
{
    /// <summary>
    /// The name of the tool being called.
    /// 正在调用的工具名称。
    /// </summary>
    public string ToolName { get; init; } = "";

    /// <summary>
    /// Optional arguments for the tool call.
    /// 工具调用的可选参数。
    /// </summary>
    public Dictionary<string, object>? Arguments { get; init; }
}

/// <summary>
/// Interface for permission evaluation engines.
/// 权限评估引擎的接口。
/// Corresponds to Java: io.agentscope.core.permission.IPermissionEngine
/// 对应 Java: io.agentscope.core.permission.IPermissionEngine
/// </summary>
public interface IPermissionEngine
{
    /// <summary>
    /// Evaluates a tool call request and returns a permission decision.
    /// 评估工具调用请求并返回权限决策。
    /// </summary>
    /// <param name="request">The tool call request to evaluate. 要评估的工具调用请求。</param>
    /// <returns>The permission decision. 权限决策。</returns>
    PermissionDecision Evaluate(ToolCallRequest request);
}

/// <summary>
/// 6-step priority state machine permission engine:
/// 6 步优先级状态机权限引擎：
/// deny > ask > tool-specific > allow > bypass > default
/// 
/// The engine evaluates tool call requests against a set of rules with the following priority:
/// 引擎按以下优先级评估工具调用请求：
/// 1. Deny rules (highest priority) / 拒绝规则（最高优先级）
/// 2. Ask rules / 询问规则
/// 3. Tool-specific built-in rules / 工具特定的内置规则
/// 4. Allow rules / 允许规则
/// 5. Bypass mode / 绕过模式
/// 6. Default fallback / 默认回退
/// 
/// Corresponds to Java: io.agentscope.core.permission.PermissionEngine
/// 对应 Java: io.agentscope.core.permission.PermissionEngine
/// </summary>
public class PermissionEngine : IPermissionEngine
{
    /// <summary>
    /// The current permission mode.
    /// 当前权限模式。
    /// </summary>
    private readonly PermissionMode _mode;

    /// <summary>
    /// The list of permission rules to evaluate against.
    /// 要评估的权限规则列表。
    /// </summary>
    private readonly List<PermissionRule> _rules = new();

    /// <summary>
    /// Initializes a new instance of PermissionEngine with the specified mode.
    /// 使用指定的模式初始化 PermissionEngine 的新实例。
    /// </summary>
    /// <param name="mode">The permission mode. Defaults to Default. 权限模式。默认为 Default。</param>
    public PermissionEngine(PermissionMode mode = PermissionMode.Default) => _mode = mode;

    /// <summary>
    /// Adds a permission rule to the engine.
    /// 向引擎添加权限规则。
    /// </summary>
    /// <param name="pattern">Wildcard pattern for matching tool names (e.g., "File*", "Read*").
    /// 用于匹配工具名称的通配符模式（例如 "File*", "Read*"）。</param>
    /// <param name="behavior">The permission behavior for matching tools. 匹配工具的权限行为。</param>
    /// <returns>This PermissionEngine instance for fluent chaining. 此 PermissionEngine 实例，支持链式调用。</returns>
    public PermissionEngine AddRule(string pattern, PermissionBehavior behavior)
    {
        _rules.Add(new PermissionRule(pattern, behavior));
        return this;
    }

    /// <summary>
    /// Evaluates a tool call request using the 6-step priority state machine.
    /// 使用 6 步优先级状态机评估工具调用请求。
    /// </summary>
    /// <param name="request">The tool call request to evaluate. 要评估的工具调用请求。</param>
    /// <returns>A PermissionDecision with the evaluation result. 包含评估结果的 PermissionDecision。</returns>
    public PermissionDecision Evaluate(ToolCallRequest request)
    {
        // Step 1: Deny rules have the highest priority
        // 第 1 步: deny 规则最高优先
        foreach (var r in _rules)
        {
            if (r.Behavior == PermissionBehavior.Deny && Regex.IsMatch(request.ToolName, Wildcard(r.Pattern)))
            {
                return new PermissionDecision(PermissionBehavior.Deny, $"deny 规则匹配: {r.Pattern}");
            }
        }

        // Step 2: Ask rules
        // 第 2 步: ask 规则
        foreach (var r in _rules)
        {
            if (r.Behavior == PermissionBehavior.Ask && Regex.IsMatch(request.ToolName, Wildcard(r.Pattern)))
            {
                return new PermissionDecision(PermissionBehavior.Ask, $"ask 规则匹配: {r.Pattern}");
            }
        }

        // Step 3: Tool-specific default behavior (built-in safe tools are auto-allowed)
        // 第 3 步: tool-specific 默认行为（内置安全工具自动放行）
        if (request.ToolName == "CalculatorTool" || request.ToolName == "GetTimeTool")
        {
            return new PermissionDecision(PermissionBehavior.Allow, "内置安全工具自动放行");
        }

        // Step 4: Allow rules
        // 第 4 步: allow 规则
        foreach (var r in _rules)
        {
            if (r.Behavior == PermissionBehavior.Allow && Regex.IsMatch(request.ToolName, Wildcard(r.Pattern)))
            {
                return new PermissionDecision(PermissionBehavior.Allow, $"allow 规则匹配: {r.Pattern}");
            }
        }

        // Step 5: Bypass mode allows all
        // 第 5 步: bypass 模式放行
        if (_mode == PermissionMode.Bypass)
        {
            return new PermissionDecision(PermissionBehavior.Allow, "Bypass 模式: 放行");
        }

        // Step 6: Default fallback
        // 第 6 步: default 回退
        if (_mode == PermissionMode.DontAsk)
        {
            return new PermissionDecision(PermissionBehavior.Allow, "DontAsk 模式: 默认放行");
        }

        return new PermissionDecision(PermissionBehavior.Ask, "无规则匹配，需要用户确认");
    }

    /// <summary>
    /// Converts a wildcard pattern to a regular expression.
    /// 将通配符模式转换为正则表达式。
    /// '*' matches any sequence of characters.
    /// '*' 匹配任意字符序列。
    /// </summary>
    /// <param name="p">The wildcard pattern. 通配符模式。</param>
    /// <returns>The equivalent regular expression pattern. 等效的正则表达式模式。</returns>
    private static string Wildcard(string p) => "^" + Regex.Escape(p).Replace("\\*", ".*") + "$";
}
