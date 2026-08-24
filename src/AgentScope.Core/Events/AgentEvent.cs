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
using AgentScope.Core.Message;

namespace AgentScope.Core.Events;

/// <summary>
/// Fine-grained AgentEvent record type hierarchy.
/// 细粒度 AgentEvent record 类型体系
/// Corresponds to: io.agentscope.core.event.AgentEvent (Java)
/// </summary>
public abstract record AgentEvent(string ReplyId);

/// <summary>
/// Event raised when an agent starts execution.
/// Agent 开始执行
/// </summary>
/// <param name="ReplyId">Reply identifier / 回复标识</param>
/// <param name="AgentName">Agent name / Agent 名称</param>
/// <param name="SessionId">Optional session identifier / 可选的会话标识</param>
public record AgentStartEvent(string ReplyId, string AgentName, string? SessionId = null) : AgentEvent(ReplyId);

/// <summary>
/// Event raised when an agent finishes execution.
/// Agent 执行结束
/// </summary>
public record AgentEndEvent(string ReplyId) : AgentEvent(ReplyId);

/// <summary>
/// Event raised when an agent returns a result.
/// Agent 返回结果
/// </summary>
public record AgentResultEvent(Msg Result, string ReplyId = "") : AgentEvent(ReplyId);

/// <summary>
/// Event raised when a text block starts (streaming).
/// 文本块开始
/// </summary>
public record TextBlockStartEvent(string ReplyId) : AgentEvent(ReplyId);

/// <summary>
/// Event raised when a text block receives a delta (streaming).
/// 文本块增量
/// </summary>
public record TextBlockDeltaEvent(string ReplyId, string Text) : AgentEvent(ReplyId);

/// <summary>
/// Event raised when a text block ends (streaming).
/// 文本块结束
/// </summary>
public record TextBlockEndEvent(string ReplyId) : AgentEvent(ReplyId);

/// <summary>
/// Event raised when a thinking block starts (streaming, e.g. extended thinking).
/// 推理块开始
/// </summary>
public record ThinkingBlockStartEvent(string ReplyId) : AgentEvent(ReplyId);

/// <summary>
/// Event raised when a thinking block receives a delta (streaming).
/// 推理块增量
/// </summary>
public record ThinkingBlockDeltaEvent(string ReplyId, string Thinking) : AgentEvent(ReplyId);

/// <summary>
/// Event raised when a thinking block ends (streaming).
/// 推理块结束
/// </summary>
public record ThinkingBlockEndEvent(string ReplyId) : AgentEvent(ReplyId);

/// <summary>
/// Event raised when a tool is called by the model.
/// 工具调用
/// </summary>
public record ToolCallEvent(string ReplyId, ToolUseBlock ToolUse) : AgentEvent(ReplyId);

/// <summary>
/// Event raised when a tool execution result is available.
/// 工具结果
/// </summary>
public record ToolResultEvent(string ReplyId, ToolResultBlock ToolResult) : AgentEvent(ReplyId);

/// <summary>
/// Event raised when user confirmation is required for a tool execution.
/// 需要用户确认
/// </summary>
public record RequireUserConfirmEvent(string ReplyId, string ToolName, Dictionary<string, object>? Arguments = null) : AgentEvent(ReplyId);

/// <summary>
/// Event raised when the maximum number of iterations has been exceeded.
/// 超最大迭代次数
/// </summary>
public record ExceedMaxItersEvent(string ReplyId, int MaxIterations) : AgentEvent(ReplyId);

/// <summary>
/// Event raised when all tools have been denied by the user.
/// 所有工具被拒绝
/// </summary>
public record AllToolsDeniedEvent(string ReplyId) : AgentEvent(ReplyId);

/// <summary>
/// Event raised when a hint block is received (non-interactive prompt information from the model).
/// 提示块事件（模型向用户显示无需响应的提示信息）
/// </summary>
public record HintBlockEvent(string ReplyId, string Hint) : AgentEvent(ReplyId);

/// <summary>
/// Event raised when a model call starts.
/// 模型调用开始
/// </summary>
public record ModelCallStartEvent(string ReplyId, string? ModelName = null) : AgentEvent(ReplyId);

/// <summary>
/// Event raised when a model call ends.
/// 模型调用结束
/// </summary>
public record ModelCallEndEvent(string ReplyId, string? ModelName = null) : AgentEvent(ReplyId);

/// <summary>
/// Custom event with extensible field name/value pairs. Corresponds to Java CustomEvent.
/// 自定义事件（扩展字段名/值对）。对标 Java CustomEvent。
/// </summary>
public record CustomAgentEvent(string ReplyId, string Name, object? Value = null) : AgentEvent(ReplyId);
