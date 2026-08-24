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

namespace AgentScope.Core.Agent;

/// <summary>
/// Input model for the agent middleware pipeline, containing input messages,
/// runtime context, the target agent, and additional metadata.
/// This is the primary data transfer object passed through the middleware chain
/// during agent invocation.
/// Agent 中间件管道的输入模型，包含输入消息、运行时上下文、目标 Agent 和附加元数据。
/// 这是在 Agent 调用过程中通过中间件链传递的主要数据传输对象。
/// </summary>
public class AgentInput
{
    /// <summary>
    /// Gets the list of input messages to be processed by the agent.
    /// 获取待 Agent 处理的输入消息列表。
    /// </summary>
    public IReadOnlyList<Msg> Messages { get; init; } = [];

    /// <summary>
    /// Gets the optional runtime context for the current invocation.
    /// 获取当前调用的可选运行时上下文。
    /// </summary>
    public RuntimeContext? Context { get; init; }

    /// <summary>
    /// Gets the agent instance being invoked.
    /// 获取被调用的 Agent 实例。
    /// </summary>
    public IAgent? Agent { get; init; }

    /// <summary>
    /// Gets additional metadata key-value pairs for extensibility.
    /// 获取用于扩展的附加元数据键值对。
    /// </summary>
    public Dictionary<string, object> Metadata { get; init; } = new();
}

/// <summary>
/// Middleware input for the reasoning stage of agent execution.
/// Contains the messages and context needed for the agent to perform reasoning.
/// 中间件在 Agent 执行推理阶段的输入模型。
/// 包含 Agent 执行推理所需的消息和上下文。
/// </summary>
public class ReasoningInput
{
    /// <summary>
    /// Gets the input messages for the reasoning stage.
    /// 获取推理阶段的输入消息。
    /// </summary>
    public IReadOnlyList<Msg> Messages { get; init; } = [];

    /// <summary>
    /// Gets the runtime context for the reasoning stage.
    /// 获取推理阶段的运行时上下文。
    /// </summary>
    public RuntimeContext? Context { get; init; }
}

/// <summary>
/// Middleware input for the acting (tool execution) stage of agent execution.
/// Contains the tool calls to be executed and the runtime context.
/// 中间件在 Agent 执行行动（工具调用）阶段的输入模型。
    /// 包含待执行的工具调用和运行时上下文。
/// </summary>
public class ActingInput
{
    /// <summary>
    /// Gets the list of tool calls to be executed during the acting stage.
    /// 获取行动阶段待执行的工具调用列表。
    /// </summary>
    public List<ToolUseBlock> ToolCalls { get; init; } = [];

    /// <summary>
    /// Gets the runtime context for the acting stage.
    /// 获取行动阶段的运行时上下文。
    /// </summary>
    public RuntimeContext? Context { get; init; }
}

/// <summary>
/// Middleware input for the model call stage of agent execution.
/// Contains the messages to be sent to the language model and optional call options.
/// 中间件在 Agent 执行模型调用阶段的输入模型。
/// 包含待发送给语言模型的消息和可选的调用选项。
/// </summary>
public class ModelCallInput
{
    /// <summary>
    /// Gets the messages to be sent to the language model.
    /// 获取待发送给语言模型的消息。
    /// </summary>
    public IReadOnlyList<Msg> Messages { get; init; } = [];

    /// <summary>
    /// Gets optional model call options (e.g., temperature, max_tokens).
    /// 获取可选的模型调用选项（如 temperature、max_tokens 等）。
    /// </summary>
    public Dictionary<string, object>? Options { get; init; }

    /// <summary>
    /// Gets the runtime context for the model call stage.
    /// 获取模型调用阶段的运行时上下文。
    /// </summary>
    public RuntimeContext? Context { get; init; }
}
