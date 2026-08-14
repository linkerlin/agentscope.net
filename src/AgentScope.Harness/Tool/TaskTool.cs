// Copyright 2024-2026 the original author or authors.
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

using AgentScope.Core.State;
using AgentScope.Core.Tool;

namespace AgentScope.Harness.Tool;

/// <summary>
/// 任务管理工具：添加/完成/列出当前会话的任务（基于 TaskContextState）。
/// 对应 Java: io.agentscope.harness.agent.tool.TaskTool
/// </summary>
public sealed class TaskTool : ITool
{
    private readonly TaskContextState _state;

    public TaskTool(TaskContextState state)
    {
        _state = state ?? throw new ArgumentNullException(nameof(state));
    }

    public string Name => "task";
    public string Description => "管理会话任务：add(添加) / complete(完成) / list(列出)";

    public Task<ToolResult> ExecuteAsync(Dictionary<string, object> parameters)
    {
        var action = (parameters.GetValueOrDefault("action")?.ToString() ?? "list").ToLowerInvariant();
        switch (action)
        {
            case "add":
            {
                var content = parameters.GetValueOrDefault("content")?.ToString();
                if (string.IsNullOrWhiteSpace(content))
                    return Task.FromResult(ToolResult.Fail("需要 content 参数"));
                var subject = parameters.GetValueOrDefault("subject")?.ToString();
                var item = _state.AddTask(content!, subject);
                return Task.FromResult(ToolResult.Ok($"已添加任务 {item.Id}: {item.Content}"));
            }
            case "complete":
            {
                var id = parameters.GetValueOrDefault("id")?.ToString();
                if (string.IsNullOrWhiteSpace(id))
                    return Task.FromResult(ToolResult.Fail("需要 id 参数"));
                return Task.FromResult(_state.CompleteTask(id!)
                    ? ToolResult.Ok($"任务 {id} 已完成")
                    : ToolResult.Fail($"未找到任务 {id}"));
            }
            case "list":
            {
                var lines = _state.Tasks.Count == 0
                    ? "（暂无任务）"
                    : string.Join("\n", _state.Tasks.Select(t => $"[{(t.Done ? "x" : " ")}] {t.Id}: {t.Content}"));
                return Task.FromResult(ToolResult.Ok($"待办 {_state.PendingCount}/{_state.Tasks.Count}\n{lines}"));
            }
            default:
                return Task.FromResult(ToolResult.Fail($"未知 action: {action}（支持 add/complete/list）"));
        }
    }

    public Dictionary<string, object> GetSchema() => new()
    {
        ["name"] = Name,
        ["description"] = Description,
        ["parameters"] = new Dictionary<string, object>
        {
            ["type"] = "object",
            ["properties"] = new Dictionary<string, object>
            {
                ["action"] = new Dictionary<string, object>
                {
                    ["type"] = "string",
                    ["enum"] = new[] { "add", "complete", "list" },
                    ["description"] = "操作类型"
                },
                ["content"] = new Dictionary<string, object> { ["type"] = "string", ["description"] = "任务内容（add 时必填）" },
                ["subject"] = new Dictionary<string, object> { ["type"] = "string", ["description"] = "任务主题（可选）" },
                ["id"] = new Dictionary<string, object> { ["type"] = "string", ["description"] = "任务ID（complete 时必填）" }
            },
            ["required"] = new[] { "action" }
        }
    };
}
