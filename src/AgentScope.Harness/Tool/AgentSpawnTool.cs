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

using AgentScope.Core.Agent;
using AgentScope.Core.Message;
using AgentScope.Core.Tool;
using AgentScope.Harness.Subagent;

namespace AgentScope.Harness.Tool;

/// <summary>
/// Agent spawn tool. Corresponds to Java AgentGenerateTool/AgentSpawnTool.
/// Creates child agents at runtime and delegates independent tasks to them.
/// Agent 生成工具。在运行中创建新的子 Agent 并执行独立任务。
/// </summary>
public sealed class AgentSpawnTool(ISubagentManager subagentManager) : ITool
{
    /// <inheritdoc />
    public string Name => "spawn_agent";

    /// <inheritdoc />
    public string Description => "生成一个子 Agent 执行独立任务";

    /// <inheritdoc />
    public async Task<ToolResult> ExecuteAsync(Dictionary<string, object> parameters)
    {
        var name = parameters.GetValueOrDefault("name")?.ToString();
        if (string.IsNullOrWhiteSpace(name))
            return ToolResult.Fail("需要 name 参数");

        var task = parameters.GetValueOrDefault("task")?.ToString();
        var subagent = subagentManager.GetOrCreate(name);

        // 未指定 task 时仅创建/返回子 Agent（保持原语义）
        // Return the subagent without a task if task is not specified (preserve original semantics)
        if (string.IsNullOrWhiteSpace(task))
            return ToolResult.Ok($"子 Agent '{name}' 已就绪");

        // 真正把任务委派给子 Agent 执行，并回传结果
        // Delegate the task to the child agent and relay the result
        var input = Msg.Builder().Role("user").TextContent(task).Build();
        var result = await subagent.CallAsync(input);
        var text = result.GetTextContent() ?? $"子 Agent '{name}' 已完成任务";
        return ToolResult.Ok(text);
    }

    /// <inheritdoc />
    public Dictionary<string, object> GetSchema() => new()
    {
        ["name"] = Name,
        ["description"] = Description,
        ["parameters"] = new Dictionary<string, object>
        {
            ["type"] = "object",
            ["properties"] = new Dictionary<string, object>
            {
                ["name"] = new Dictionary<string, object> { ["type"] = "string", ["description"] = "子 Agent 名称" },
                ["task"] = new Dictionary<string, object> { ["type"] = "string", ["description"] = "任务描述" }
            },
            ["required"] = new[] { "name" }
        }
    };
}
