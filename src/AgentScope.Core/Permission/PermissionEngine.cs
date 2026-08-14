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
/// 权限模式枚举
/// </summary>
public enum PermissionMode
{
    Default,
    AcceptEdits,
    Explore,
    Bypass,
    DontAsk
}

/// <summary>
/// 权限行为枚举
/// </summary>
public enum PermissionBehavior
{
    Allow,
    Deny,
    Ask,
    Passthrough
}

/// <summary>
/// 权限规则记录
/// </summary>
public record PermissionRule(string Pattern, PermissionBehavior Behavior);

/// <summary>
/// 权限决策记录
/// </summary>
public record PermissionDecision(
    PermissionBehavior Behavior,
    string Reason,
    List<string>? SuggestedRules = null,
    Dictionary<string, object>? UpdatedInput = null);

/// <summary>
/// 权限上下文状态记录
/// </summary>
public record PermissionContextState(
    PermissionMode Mode,
    string WorkingDirectory,
    List<PermissionRule> AllowRules,
    List<PermissionRule> DenyRules,
    List<PermissionRule> AskRules);

/// <summary>
/// 附加工作目录记录
/// </summary>
public record AdditionalWorkingDirectory(string Path, string Source);

/// <summary>
/// 工具调用请求类
/// </summary>
public class ToolCallRequest
{
    public string ToolName { get; init; } = "";
    public Dictionary<string, object>? Arguments { get; init; }
}

/// <summary>
/// 权限引擎接口
/// </summary>
public interface IPermissionEngine
{
    PermissionDecision Evaluate(ToolCallRequest request);
}

/// <summary>
/// 6 步优先级状态机权限引擎：
/// deny > ask > tool-specific > allow > bypass > default
/// </summary>
public class PermissionEngine : IPermissionEngine
{
    private readonly PermissionMode _mode;
    private readonly List<PermissionRule> _rules = new();

    public PermissionEngine(PermissionMode mode = PermissionMode.Default) => _mode = mode;

    /// <summary>
    /// 添加权限规则
    /// </summary>
    public PermissionEngine AddRule(string pattern, PermissionBehavior behavior)
    {
        _rules.Add(new PermissionRule(pattern, behavior));
        return this;
    }

    /// <summary>
    /// 评估工具调用请求，按 6 步优先级返回决策
    /// </summary>
    public PermissionDecision Evaluate(ToolCallRequest request)
    {
        // 第 1 步: deny 规则最高优先
        foreach (var r in _rules)
        {
            if (r.Behavior == PermissionBehavior.Deny && Regex.IsMatch(request.ToolName, Wildcard(r.Pattern)))
            {
                return new PermissionDecision(PermissionBehavior.Deny, $"deny 规则匹配: {r.Pattern}");
            }
        }

        // 第 2 步: ask 规则
        foreach (var r in _rules)
        {
            if (r.Behavior == PermissionBehavior.Ask && Regex.IsMatch(request.ToolName, Wildcard(r.Pattern)))
            {
                return new PermissionDecision(PermissionBehavior.Ask, $"ask 规则匹配: {r.Pattern}");
            }
        }

        // 第 3 步: tool-specific 默认行为（内置安全工具放行）
        if (request.ToolName == "CalculatorTool" || request.ToolName == "GetTimeTool")
        {
            return new PermissionDecision(PermissionBehavior.Allow, "内置安全工具自动放行");
        }

        // 第 4 步: allow 规则
        foreach (var r in _rules)
        {
            if (r.Behavior == PermissionBehavior.Allow && Regex.IsMatch(request.ToolName, Wildcard(r.Pattern)))
            {
                return new PermissionDecision(PermissionBehavior.Allow, $"allow 规则匹配: {r.Pattern}");
            }
        }

        // 第 5 步: bypass 模式放行
        if (_mode == PermissionMode.Bypass)
        {
            return new PermissionDecision(PermissionBehavior.Allow, "Bypass 模式: 放行");
        }

        // 第 6 步: default 回退
        if (_mode == PermissionMode.DontAsk)
        {
            return new PermissionDecision(PermissionBehavior.Allow, "DontAsk 模式: 默认放行");
        }

        return new PermissionDecision(PermissionBehavior.Ask, "无规则匹配，需要用户确认");
    }

    /// <summary>
    /// 将通配符模式转为正则表达式
    /// </summary>
    private static string Wildcard(string p) => "^" + Regex.Escape(p).Replace("\\*", ".*") + "$";
}
