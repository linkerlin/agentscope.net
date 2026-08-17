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
/// Input model for agent middleware, containing input messages and context.
/// Agent 中间件的输入模型，包含输入消息和上下文。
/// </summary>
public class AgentInput
{
    /// <summary>Input messages / 输入消息列表</summary>
    public IReadOnlyList<Msg> Messages { get; init; } = [];

    /// <summary>Runtime context / 运行时上下文</summary>
    public RuntimeContext? Context { get; init; }

    /// <summary>The agent being invoked / 被调用的 Agent</summary>
    public IAgent? Agent { get; init; }

    /// <summary>Additional metadata / 附加元数据</summary>
    public Dictionary<string, object> Metadata { get; init; } = new();
}

/// <summary>
/// Middleware input for the reasoning stage.
/// 推理阶段的中间件输入。
/// </summary>
public class ReasoningInput
{
    /// <summary>Input messages for reasoning / 推理阶段的输入消息</summary>
    public IReadOnlyList<Msg> Messages { get; init; } = [];

    /// <summary>Runtime context / 运行时上下文</summary>
    public RuntimeContext? Context { get; init; }
}

/// <summary>
/// Middleware input for the acting stage.
/// 行动阶段的中间件输入。
/// </summary>
public class ActingInput
{
    /// <summary>Tool calls to execute / 待执行的工具调用</summary>
    public List<ToolUseBlock> ToolCalls { get; init; } = [];

    /// <summary>Runtime context / 运行时上下文</summary>
    public RuntimeContext? Context { get; init; }
}

/// <summary>
/// Middleware input for the model call stage.
/// 模型调用阶段的中间件输入。
/// </summary>
public class ModelCallInput
{
    /// <summary>Messages to send to the model / 发送给模型的消息</summary>
    public IReadOnlyList<Msg> Messages { get; init; } = [];

    /// <summary>Optional model call options / 可选的模型调用选项</summary>
    public Dictionary<string, object>? Options { get; init; }

    /// <summary>Runtime context / 运行时上下文</summary>
    public RuntimeContext? Context { get; init; }
}
