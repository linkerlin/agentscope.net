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

namespace AgentScope.Core.Tool;

/// <summary>
/// 工具挂起异常：表示工具需要等待外部条件（人工确认/异步结果）而挂起执行。
/// 对应 Java: io.agentscope.core.tool.ToolSuspendException
/// </summary>
public class ToolSuspendException : System.Exception
{
    /// <summary>挂起标识，用于后续恢复时关联。</summary>
    public string? SuspendId { get; }

    public ToolSuspendException(string message, string? suspendId = null) : base(message)
    {
        SuspendId = suspendId;
    }

    public ToolSuspendException(string message, System.Exception inner, string? suspendId = null)
        : base(message, inner)
    {
        SuspendId = suspendId;
    }
}
