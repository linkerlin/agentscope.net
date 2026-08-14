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
using System.Threading;
using AgentScope.Core.Agent;
using AgentScope.Core.Message;
using AgentScope.Core.Permission;

namespace AgentScope.Core.Tool;

/// <summary>
/// 工具执行上下文，携带执行所需的环境信息和生命周期控制。
/// </summary>
public class ToolExecutionContext
{
    /// <summary>正在执行的工具调用块</summary>
    public ToolUseBlock ToolUse { get; }

    /// <summary>调用该工具的 Agent</summary>
    public IAgent? Agent { get; }

    /// <summary>当前运行时上下文</summary>
    public RuntimeContext? RuntimeContext { get; }

    /// <summary>权限引擎</summary>
    public IPermissionEngine? PermissionEngine { get; }

    /// <summary>取消令牌</summary>
    public CancellationToken CancellationToken { get; }

    /// <summary>扩展元数据</summary>
    public Dictionary<string, object> Metadata { get; } = new();

    public ToolExecutionContext(
        ToolUseBlock toolUse,
        IAgent? agent = null,
        RuntimeContext? runtimeContext = null,
        IPermissionEngine? permissionEngine = null,
        CancellationToken cancellationToken = default)
    {
        ToolUse = toolUse ?? throw new ArgumentNullException(nameof(toolUse));
        Agent = agent;
        RuntimeContext = runtimeContext;
        PermissionEngine = permissionEngine;
        CancellationToken = cancellationToken;
    }

    /// <summary>
    /// 评估权限决策
    /// </summary>
    public PermissionDecision? EvaluatePermission()
    {
        if (PermissionEngine == null) return null;

        var request = new ToolCallRequest
        {
            ToolName = ToolUse.Name,
            Arguments = ToolUse.Input
        };

        return PermissionEngine.Evaluate(request);
    }

    /// <summary>
    /// 获取工具参数，不存在时返回默认值
    /// </summary>
    public T? GetParameter<T>(string key)
    {
        if (ToolUse.Input == null) return default;

        if (ToolUse.Input.TryGetValue(key, out var val))
        {
            try
            {
                return (T?)Convert.ChangeType(val, typeof(T));
            }
            catch
            {
                return default;
            }
        }

        return default;
    }
}
