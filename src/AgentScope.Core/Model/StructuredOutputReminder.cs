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

namespace AgentScope.Core.Model;

/// <summary>
/// Structured output reminder type, used to remind the model to use tools or system prompts during generation.
/// These reminders are injected into the model request to guide structured output behavior.
/// Corresponds to Java: io.agentscope.core.model.StructuredOutputReminder
/// 结构化输出提示类型，用于在生成时提醒模型使用工具或系统提示等。
/// 这些提示会被注入到模型请求中，以引导结构化输出行为。
/// 对应 Java: io.agentscope.core.model.StructuredOutputReminder
/// </summary>
public class StructuredOutputReminder
{
    /// <summary>
    /// Gets the kind/type of the structured output reminder.
    /// 获取结构化输出提示的类型。
    /// </summary>
    public string Kind { get; }

    private StructuredOutputReminder(string kind)
    {
        Kind = kind;
    }

    /// <summary>
    /// Reminder for tool choice - tells the model to use a specific tool.
    /// 工具选择提示 - 告诉模型使用特定工具。
    /// </summary>
    public static StructuredOutputReminder ToolChoice { get; } = new("tool_choice");

    /// <summary>
    /// Reminder for system prompt - tells the model to follow system prompt instructions.
    /// 系统提示提示 - 告诉模型遵循系统提示指令。
    /// </summary>
    public static StructuredOutputReminder SystemPrompt { get; } = new("system_prompt");
}
