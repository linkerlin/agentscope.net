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

using AgentScope.Core.Memory;
using AgentScope.Core.Tool;

namespace AgentScope.Harness.Tool;

/// <summary>
/// 记忆获取工具：按条数/关键词从记忆中检索消息。
/// 对应 Java: io.agentscope.harness.agent.tool.MemoryGetTool
/// </summary>
public sealed class MemoryGetTool : ITool
{
    private readonly IMemory _memory;

    public MemoryGetTool(IMemory memory)
    {
        _memory = memory ?? throw new ArgumentNullException(nameof(memory));
    }

    public string Name => "memory_get";
    public string Description => "从 Agent 记忆中检索最近的消息（可按关键词过滤）";

    public Task<ToolResult> ExecuteAsync(Dictionary<string, object> parameters)
    {
        var countObj = parameters.GetValueOrDefault("count");
        var count = countObj != null && int.TryParse(countObj.ToString(), out var c) ? c : 10;
        if (count <= 0) count = 10;

        var keyword = parameters.GetValueOrDefault("keyword")?.ToString();

        var recent = _memory.GetRecent(count);
        if (!string.IsNullOrEmpty(keyword))
        {
            recent = recent.Where(m =>
            {
                var t = m.GetTextContent();
                return t != null && t.Contains(keyword!, StringComparison.OrdinalIgnoreCase);
            }).ToList();
        }

        var summary = recent.Count == 0
            ? "（无匹配记忆）"
            : string.Join("\n---\n", recent.Select(m => $"[{m.Role}] {m.GetTextContent()}"));

        return Task.FromResult(ToolResult.Ok(summary));
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
                ["count"] = new Dictionary<string, object> { ["type"] = "integer", ["description"] = "检索条数（默认 10）" },
                ["keyword"] = new Dictionary<string, object> { ["type"] = "string", ["description"] = "可选关键词过滤" }
            },
            ["required"] = Array.Empty<string>()
        }
    };
}
