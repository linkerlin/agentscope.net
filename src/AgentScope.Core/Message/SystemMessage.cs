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
/// System message, representing a message with the system role. Role is fixed to "system".
/// 系统消息，表示系统角色的消息，role 固定为 "system"。
/// </summary>
public class SystemMessage : Msg
{
    /// <summary>
    /// Initializes a new instance of <see cref="SystemMessage"/> with the system role.
    /// 使用系统角色初始化 <see cref="SystemMessage"/> 新实例。
    /// </summary>
    public SystemMessage()
    {
        Role = "system";
    }

    /// <summary>
    /// Initializes a new instance of <see cref="SystemMessage"/> with the specified name and content.
    /// 使用指定的名称和内容初始化 <see cref="SystemMessage"/> 新实例。
    /// </summary>
    /// <param name="name">Optional sender name / 可选的发送者名称。</param>
    /// <param name="content">Message content / 消息内容。</param>
    public SystemMessage(string? name, object? content)
        : base(name, content, "system")
    {
    }
}
