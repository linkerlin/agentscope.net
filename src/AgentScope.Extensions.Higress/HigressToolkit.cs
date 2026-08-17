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

using System.Collections.Concurrent;
using System.Text.Json;
using AgentScope.Core.Tool;

namespace AgentScope.Extensions.Higress;

/// <summary>
/// Higress 工具集：从 Higress 网关发现工具并暴露为本地 ITool，
/// 调用时经 HigressMcpClient 远程执行。
/// 对应 Java: io.agentscope.extensions.higress.HigressToolkit
/// </summary>
public sealed class HigressToolkit
{
    private readonly HigressMcpClient _client;
    private readonly ConcurrentDictionary<string, HigressToolSearchResult> _cache = new(StringComparer.OrdinalIgnoreCase);

    public HigressToolkit(HigressMcpClient client)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
    }

    /// <summary>发现并缓存网关上的工具列表。</summary>
    public async Task<IReadOnlyList<HigressToolSearchResult>> DiscoverAsync(CancellationToken ct = default)
    {
        var names = await _client.ListToolsAsync(ct);
        var results = new List<HigressToolSearchResult>();
        foreach (var name in names)
        {
            var r = new HigressToolSearchResult { Name = name, Description = $"Higress 远程工具: {name}" };
            _cache[name] = r;
            results.Add(r);
        }

        return results;
    }

    /// <summary>按关键词搜索已发现的工具。</summary>
    public IReadOnlyList<HigressToolSearchResult> Search(string keyword)
    {
        if (string.IsNullOrEmpty(keyword)) return _cache.Values.ToArray();
        return _cache.Values
            .Where(t => t.Name.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                        (t.Description?.Contains(keyword, StringComparison.OrdinalIgnoreCase) ?? false))
            .ToArray();
    }

    /// <summary>把已发现的工具包装为本地 ITool（调用经远程网关执行）。</summary>
    public IEnumerable<ITool> AsTools() => _cache.Values.Select(t => new HigressRemoteTool(_client, t));

    private sealed class HigressRemoteTool : ITool
    {
        private readonly HigressMcpClient _client;
        private readonly HigressToolSearchResult _result;

        public HigressRemoteTool(HigressMcpClient client, HigressToolSearchResult result)
        {
            _client = client;
            _result = result;
        }

        public string Name => _result.Name;
        public string Description => _result.Description ?? $"Higress 远程工具 {_result.Name}";

        public async Task<ToolResult> ExecuteAsync(Dictionary<string, object> parameters)
        {
            try
            {
                var args = JsonSerializer.SerializeToElement(parameters);
                var resp = await _client.CallToolAsync(_result.Name, args);
                return ToolResult.Ok(resp);
            }
            catch (System.Exception ex)
            {
                return ToolResult.Fail(ex.Message);
            }
        }

        public Dictionary<string, object> GetSchema() => new()
        {
            ["name"] = Name,
            ["description"] = Description,
            ["parameters"] = new Dictionary<string, object>
            {
                ["type"] = "object",
                ["properties"] = new Dictionary<string, object>(),
                ["required"] = Array.Empty<string>()
            }
        };
    }
}
