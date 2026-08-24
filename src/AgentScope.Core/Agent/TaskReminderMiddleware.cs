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

namespace AgentScope.Core.Agent;

/// <summary>
/// Middleware that ensures the model does not forget the key task context
/// by appending a reminder to the system prompt.
/// Corresponds to Java: io.agentscope.core.agent.middleware.TaskReminderMiddleware
/// 任务提醒中间件：通过在系统提示词后追加提醒文本，确保模型不会忘记关键任务上下文。
/// 对应 Java: io.agentscope.core.agent.middleware.TaskReminderMiddleware
/// </summary>
public sealed class TaskReminderMiddleware : MiddlewareBase
{
    private readonly string _reminder;

    /// <summary>
    /// Initializes a new instance of the TaskReminderMiddleware.
    /// 初始化 TaskReminderMiddleware 的新实例。
    /// </summary>
    /// <param name="reminder">The reminder text to append to the system prompt / 要追加到系统提示词的提醒文本</param>
    public TaskReminderMiddleware(string reminder = "请记住当前任务目标 / Please remember the current task objective")
    {
        _reminder = reminder;
    }

    /// <summary>
    /// Appends the task reminder to the system prompt.
    /// 将任务提醒追加到系统提示词末尾。
    /// </summary>
    public override Task<string> OnSystemPromptAsync(IAgent agent, RuntimeContext ctx, string prompt)
    {
        return Task.FromResult($"{prompt}\n\n[系统提醒 / System Reminder]: {_reminder}");
    }
}
