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

using AgentScope.Core.Tool;
using AgentScope.Harness.Team;

namespace AgentScope.Harness.Tool;

public sealed class TeamTool(ITeamClient teamClient) : ITool
{
    public string Name => "team";
    public string Description => "团队协作：创建任务、分配任务、发送消息";

    public async Task<ToolResult> ExecuteAsync(Dictionary<string, object> parameters)
    {
        var action = parameters.GetValueOrDefault("action")?.ToString();
        return action switch
        {
            "create_task" => await CreateTaskAsync(parameters),
            "list_tasks" => await ListTasksAsync(),
            "send_message" => await SendMessageAsync(parameters),
            _ => ToolResult.Fail($"未知操作: {action}")
        };
    }

    private async Task<ToolResult> CreateTaskAsync(Dictionary<string, object> p)
    {
        var desc = p.GetValueOrDefault("description")?.ToString() ?? "";
        var id = await teamClient.CreateTaskAsync(new TeamTask(Guid.NewGuid().ToString(), desc));
        return ToolResult.Ok($"任务已创建: {id}");
    }

    private async Task<ToolResult> ListTasksAsync()
    {
        var tasks = await teamClient.ListTasksAsync();
        return ToolResult.Ok(string.Join("\n", tasks.Select(t => $"[{t.Status}] {t.Id}: {t.Description}")));
    }

    private async Task<ToolResult> SendMessageAsync(Dictionary<string, object> p)
    {
        var to = p.GetValueOrDefault("to")?.ToString() ?? "";
        var content = p.GetValueOrDefault("content")?.ToString() ?? "";
        await teamClient.SendMessageAsync(to, new TeamMessage("agent", to, content, DateTime.UtcNow));
        return ToolResult.Ok("消息已发送");
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
                ["action"] = new Dictionary<string, object> { ["type"] = "string", ["description"] = "操作: create_task/list_tasks/send_message" },
                ["description"] = new Dictionary<string, object> { ["type"] = "string", ["description"] = "任务描述" },
                ["to"] = new Dictionary<string, object> { ["type"] = "string", ["description"] = "消息接收者" },
                ["content"] = new Dictionary<string, object> { ["type"] = "string", ["description"] = "消息内容" }
            },
            ["required"] = new[] { "action" }
        }
    };
}
