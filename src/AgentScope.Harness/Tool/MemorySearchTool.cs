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

using AgentScope.Core.Memory;
using AgentScope.Core.Tool;

namespace AgentScope.Harness.Tool;

/// <summary>
/// 记忆搜索工具。对标 Java MemorySearchTool。
/// </summary>
public sealed class MemorySearchTool(ILongTermMemory memory) : ITool
{
    public string Name => "memory_search";
    public string Description => "搜索长期记忆中的信息";

    public async Task<ToolResult> ExecuteAsync(Dictionary<string, object> parameters)
    {
        var query = parameters.GetValueOrDefault("query")?.ToString();
        if (string.IsNullOrWhiteSpace(query))
            return ToolResult.Fail("需要 query 参数");

        var topK = 5;
        if (parameters.TryGetValue("topK", out var k) && k is int ki)
            topK = ki;

        var results = await memory.SearchAsync(query, topK);
        return ToolResult.Ok(string.Join("\n---\n", results));
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
                ["query"] = new Dictionary<string, object> { ["type"] = "string", ["description"] = "搜索关键词" },
                ["topK"] = new Dictionary<string, object> { ["type"] = "integer", ["description"] = "返回结果数量" }
            },
            ["required"] = new[] { "query" }
        }
    };
}
