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

using System.Collections.Generic;

namespace AgentScope.Core.State;

/// <summary>
/// 工具执行上下文状态：记录最近一次工具调用与异步工具挂起记录。
/// 对应 Java: io.agentscope.core.state.ToolContextState
/// </summary>
public class ToolContextState : IState
{
    /// <summary>最近执行的工具名</summary>
    public string? LastToolName { get; set; }

    /// <summary>最近执行的工具调用参数（只读快照）</summary>
    public Dictionary<string, object>? LastArguments { get; set; }

    /// <summary>挂起的异步工具调用 ID 集合</summary>
    public HashSet<string> PendingAsyncToolIds { get; } = new();

    /// <summary>本会话累计工具调用次数</summary>
    public int ToolCallCount { get; set; }

    /// <summary>记录一次工具调用</summary>
    public void RecordCall(string toolName, Dictionary<string, object>? arguments)
    {
        LastToolName = toolName;
        LastArguments = arguments == null ? null : new Dictionary<string, object>(arguments);
        ToolCallCount++;
    }

    /// <summary>登记一个挂起的异步工具调用</summary>
    public void RegisterPending(string toolCallId) => PendingAsyncToolIds.Add(toolCallId);

    /// <summary>完成并移除一个挂起的异步工具调用</summary>
    public bool CompletePending(string toolCallId) => PendingAsyncToolIds.Remove(toolCallId);
}
