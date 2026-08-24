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

namespace AgentScope.Harness.Subagent.Protocol;

/// <summary>
/// Remote event types for streaming remote agent execution.
/// 远程流式事件类型，用于远程 Agent 执行过程。
/// </summary>
public enum RemoteEventType
{
    /// <summary>Run has started / 运行开始</summary>
    RunStarted,
    /// <summary>Run has finished / 运行结束</summary>
    RunFinished,
    /// <summary>Run encountered an error / 运行出错</summary>
    RunError,
    /// <summary>Text content delta / 文本内容增量</summary>
    TextDelta,
    /// <summary>Thinking/reasoning content delta / 思考/推理内容增量</summary>
    ThinkingDelta,
    /// <summary>Tool call has started / 工具调用开始</summary>
    ToolCallStart,
    /// <summary>Tool call has ended / 工具调用结束</summary>
    ToolCallEnd,
    /// <summary>Tool result available / 工具结果可用</summary>
    ToolResult,
    /// <summary>Requires user confirmation / 需要用户确认</summary>
    RequireConfirm,
    /// <summary>Status update / 状态更新</summary>
    Status,
    /// <summary>Generic agent event / 通用 Agent 事件</summary>
    AgentEvent
}
