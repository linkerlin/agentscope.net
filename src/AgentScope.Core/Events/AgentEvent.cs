// Copyright 2024-2026 the original author or authors.
// Licensed under the Apache License, Version 2.0

using System.Collections.Generic;
using AgentScope.Core.Message;

namespace AgentScope.Core.Events;

/// <summary>
/// 细粒度 AgentEvent record 类型体系
/// 对应 Java: io.agentscope.core.event.AgentEvent
/// </summary>
public abstract record AgentEvent(string ReplyId);

/// <summary>Agent 开始执行</summary>
public record AgentStartEvent(string ReplyId, string AgentName, string? SessionId = null) : AgentEvent(ReplyId);

/// <summary>Agent 执行结束</summary>
public record AgentEndEvent(string ReplyId) : AgentEvent(ReplyId);

/// <summary>Agent 返回结果</summary>
public record AgentResultEvent(Msg Result, string ReplyId = "") : AgentEvent(ReplyId);

/// <summary>文本块开始</summary>
public record TextBlockStartEvent(string ReplyId) : AgentEvent(ReplyId);

/// <summary>文本块增量</summary>
public record TextBlockDeltaEvent(string ReplyId, string Text) : AgentEvent(ReplyId);

/// <summary>文本块结束</summary>
public record TextBlockEndEvent(string ReplyId) : AgentEvent(ReplyId);

/// <summary>推理块开始</summary>
public record ThinkingBlockStartEvent(string ReplyId) : AgentEvent(ReplyId);

/// <summary>推理块增量</summary>
public record ThinkingBlockDeltaEvent(string ReplyId, string Thinking) : AgentEvent(ReplyId);

/// <summary>推理块结束</summary>
public record ThinkingBlockEndEvent(string ReplyId) : AgentEvent(ReplyId);

/// <summary>工具调用</summary>
public record ToolCallEvent(string ReplyId, ToolUseBlock ToolUse) : AgentEvent(ReplyId);

/// <summary>工具结果</summary>
public record ToolResultEvent(string ReplyId, ToolResultBlock ToolResult) : AgentEvent(ReplyId);

/// <summary>需要用户确认</summary>
public record RequireUserConfirmEvent(string ReplyId, string ToolName, Dictionary<string, object>? Arguments = null) : AgentEvent(ReplyId);

/// <summary>超最大迭代次数</summary>
public record ExceedMaxItersEvent(string ReplyId, int MaxIterations) : AgentEvent(ReplyId);

/// <summary>所有工具被拒绝</summary>
public record AllToolsDeniedEvent(string ReplyId) : AgentEvent(ReplyId);

/// <summary>提示块事件（模型向用户显示无需响应的提示信息）</summary>
public record HintBlockEvent(string ReplyId, string Hint) : AgentEvent(ReplyId);

/// <summary>模型调用开始</summary>
public record ModelCallStartEvent(string ReplyId, string? ModelName = null) : AgentEvent(ReplyId);

/// <summary>模型调用结束</summary>
public record ModelCallEndEvent(string ReplyId, string? ModelName = null) : AgentEvent(ReplyId);

/// <summary>自定义事件（扩展字段名/值对）。对标 Java CustomEvent。</summary>
public record CustomAgentEvent(string ReplyId, string Name, object? Value = null) : AgentEvent(ReplyId);
