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

namespace AgentScope.Core.State;

/// <summary>
/// 任务上下文状态：记录当前会话的待办任务清单及其完成情况。
/// 对应 Java: io.agentscope.core.state.TaskContextState
/// </summary>
public class TaskContextState : IState
{
    /// <summary>任务条目</summary>
    public List<TaskItem> Tasks { get; set; } = new();

    public TaskContextState() { }

    /// <summary>添加任务</summary>
    public TaskItem AddTask(string content, string? subject = null)
    {
        var item = new TaskItem
        {
            Id = $"task-{Tasks.Count + 1}",
            Content = content,
            Subject = subject,
            Done = false
        };
        Tasks.Add(item);
        return item;
    }

    /// <summary>标记任务完成</summary>
    public bool CompleteTask(string taskId)
    {
        var t = Tasks.Find(x => x.Id == taskId);
        if (t == null) return false;
        t.Done = true;
        return true;
    }

    /// <summary>待办数量</summary>
    public int PendingCount => Tasks.FindAll(t => !t.Done).Count;
}

/// <summary>单个任务条目</summary>
public class TaskItem
{
    public string Id { get; set; } = "";
    public string Content { get; set; } = "";
    public string? Subject { get; set; }
    public bool Done { get; set; }
}
