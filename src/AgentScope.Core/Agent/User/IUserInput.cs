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

namespace AgentScope.Core.Agent.User;

/// <summary>
/// User input interface, abstracts how user input is obtained.
/// 用户输入接口，抽象了获取用户输入的方式。
/// Corresponds to Java: io.agentscope.core.agent.user.IUserInput
/// </summary>
public interface IUserInput
{
    /// <summary>
    /// Asynchronously requests user input with the given prompt text.
    /// 使用给定的提示文本异步请求用户输入。
    /// </summary>
    /// <param name="prompt">The prompt text displayed to the user / 显示给用户的提示文本</param>
    /// <returns>A task that resolves to the user's input string / 用户输入的字符串</returns>
    Task<string> RequestAsync(string prompt);
}
