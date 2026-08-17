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

using System.Threading.Tasks;
using AgentScope.Core.Message;

namespace AgentScope.Core.Agent.User;

/// <summary>
/// User interactive Agent: requests user input each round and wraps it as a UserMessage.
/// 用户交互 Agent：每轮向用户索取输入，再包装为 UserMessage。
/// Corresponds to Java: io.agentscope.core.agent.user.UserAgent
/// </summary>
public class UserAgent
{
    /// <summary>
    /// The underlying user input implementation used to obtain input.
    /// 底层用户输入实现，用于获取输入。
    /// </summary>
    private readonly IUserInput _input;

    /// <summary>
    /// The name of this user agent.
    /// 此用户 Agent 的名称。
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="UserAgent"/> class.
    /// 初始化 <see cref="UserAgent"/> 类的新实例。
    /// </summary>
    /// <param name="name">Agent name / Agent 名称</param>
    /// <param name="input">The user input provider / 用户输入提供程序</param>
    public UserAgent(string name, IUserInput input)
    {
        Name = name;
        _input = input;
    }

    /// <summary>
    /// Asynchronously prompts the user and builds a <see cref="Msg"/> with their response.
    /// 异步提示用户并用其响应构建一个 <see cref="Msg"/>。
    /// </summary>
    /// <param name="prompt">The prompt text displayed to the user, default "You> " / 显示给用户的提示文本，默认为 "You> "</param>
    /// <returns>A task that resolves to the user message / 用户消息</returns>
    public async Task<Msg> ReplyAsync(string prompt = "You> ")
    {
        // 通过输入实现获取用户文本
        // Obtain user text via the input implementation
        var text = await _input.RequestAsync(prompt);

        // 构建并返回用户消息
        // Build and return the user message
        return Msg.Builder().Name(Name).Role("user").TextContent(text).Build();
    }
}
