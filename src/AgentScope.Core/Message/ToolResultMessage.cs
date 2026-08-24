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

namespace AgentScope.Core.Message;

/// <summary>
/// Tool result message with the role fixed to "tool".
/// Represents messages returned by tool/function calls.
/// Corresponds to Java: io.agentscope.core.message.ToolResultMessage
/// 工具结果消息，role 固定为 "tool"。
/// 表示由工具/函数调用返回的消息。
/// 对应 Java: io.agentscope.core.message.ToolResultMessage
/// </summary>
public class ToolResultMessage : Msg
{
    /// <summary>
    /// Initializes a new instance of <see cref="ToolResultMessage"/> with the tool role.
    /// 使用工具角色初始化 <see cref="ToolResultMessage"/> 新实例。
    /// </summary>
    public ToolResultMessage()
    {
        Role = "tool";
    }

    /// <summary>
    /// Initializes a new instance of <see cref="ToolResultMessage"/> with the specified name and content.
    /// 使用指定的名称和内容初始化 <see cref="ToolResultMessage"/> 新实例。
    /// </summary>
    /// <param name="name">Optional sender name / 可选的发送者名称。</param>
    /// <param name="content">Message content / 消息内容。</param>
    public ToolResultMessage(string? name, object? content)
        : base(name, content, "tool")
    {
    }
}
