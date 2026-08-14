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
/// Agent 中间件的输入模型，包含输入消息和上下文
/// </summary>
public class AgentInput
{
    public IReadOnlyList<Msg> Messages { get; init; } = [];
    public RuntimeContext? Context { get; init; }
    public IAgent? Agent { get; init; }
    public Dictionary<string, object> Metadata { get; init; } = new();
}

/// <summary>
/// 推理阶段的中间件输入
/// </summary>
public class ReasoningInput
{
    public IReadOnlyList<Msg> Messages { get; init; } = [];
    public RuntimeContext? Context { get; init; }
}

/// <summary>
/// 行动阶段的中间件输入
/// </summary>
public class ActingInput
{
    public List<ToolUseBlock> ToolCalls { get; init; } = [];
    public RuntimeContext? Context { get; init; }
}

/// <summary>
/// 模型调用阶段的中间件输入
/// </summary>
public class ModelCallInput
{
    public IReadOnlyList<Msg> Messages { get; init; } = [];
    public Dictionary<string, object>? Options { get; init; }
    public RuntimeContext? Context { get; init; }
}
