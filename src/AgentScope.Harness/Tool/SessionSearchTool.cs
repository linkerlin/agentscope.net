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

using AgentScope.Core.Tool;
using AgentScope.Harness.Transcript;

namespace AgentScope.Harness.Tool;

/// <summary>
/// 会话搜索工具：在会话转录（Transcript）中按关键词模糊检索历史段落。
/// 对应 Java: io.agentscope.harness.agent.tool.SessionSearchTool
/// </summary>
public sealed class SessionSearchTool : ITool
{
    private readonly ITranscriptStore _store;

    public SessionSearchTool(ITranscriptStore store)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
    }

    public string Name => "session_search";
    public string Description => "在会话转录中按关键词检索历史段落";

    public async Task<ToolResult> ExecuteAsync(Dictionary<string, object> parameters)
    {
        var sessionId = parameters.GetValueOrDefault("session_id")?.ToString();
        var query = parameters.GetValueOrDefault("query")?.ToString();
        var limitObj = parameters.GetValueOrDefault("limit");
        var limit = limitObj != null && int.TryParse(limitObj.ToString(), out var l) ? l : 5;

        if (string.IsNullOrWhiteSpace(sessionId) || string.IsNullOrWhiteSpace(query))
        {
            return ToolResult.Fail("需要 session_id 与 query 参数");
        }

        var matches = new List<(double Score, long SeqStart, string Snippet)>();
        await foreach (var segment in _store.ListSegmentsAsync(sessionId!))
        {
            var score = FuzzyTextMatcher.Score(query!, segment.Content);
            if (score >= 0.3)
            {
                var snippet = segment.Content.Length > 200
                    ? segment.Content[..200] + "..."
                    : segment.Content;
                matches.Add((score, segment.SequenceStart, snippet));
            }
        }

        if (matches.Count == 0)
        {
            return ToolResult.Ok("（未找到匹配段落）");
        }

        var result = matches
            .OrderByDescending(x => x.Score)
            .Take(limit)
            .Select(x => $"[seq={x.SeqStart}, score={x.Score:F2}] {x.Snippet}");
        return ToolResult.Ok(string.Join("\n---\n", result));
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
                ["session_id"] = new Dictionary<string, object> { ["type"] = "string", ["description"] = "会话ID" },
                ["query"] = new Dictionary<string, object> { ["type"] = "string", ["description"] = "检索关键词" },
                ["limit"] = new Dictionary<string, object> { ["type"] = "integer", ["description"] = "最大返回数（默认5）" }
            },
            ["required"] = new[] { "session_id", "query" }
        }
    };
}
