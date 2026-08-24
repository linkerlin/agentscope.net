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

using System;
using System.Threading.Tasks;

namespace AgentScope.Core.Agent.User;

/// <summary>
/// Console-based user input implementation, reads user input from the standard console.
/// 控制台用户输入实现，从标准控制台读取用户输入。
/// </summary>
public class ConsoleUserInput : IUserInput
{
    /// <summary>
    /// Asynchronously prompts the user via console and returns their input.
    /// 通过控制台提示用户并返回其输入的文本。
    /// </summary>
    /// <param name="prompt">The prompt text displayed to the user / 显示给用户的提示文本</param>
    /// <returns>A task that resolves to the user's input string / 用户输入的字符串</returns>
    public Task<string> RequestAsync(string prompt)
    {
        // 输出提示文本到控制台
        // Write the prompt text to console
        Console.Write(prompt);

        // 读取用户输入，若为 null 则返回空字符串
        // Read user input, return empty string if null
        return Task.FromResult(Console.ReadLine() ?? "");
    }
}
