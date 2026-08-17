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

using System.Text.Json;
using System.Web;

namespace AgentScope.Core.Tool;

/// <summary>
/// Web search result
/// 网络搜索结果
/// </summary>
public class WebSearchResult
{
    /// <summary>
    /// Result title
    /// 结果标题
    /// </summary>
    public required string Title { get; init; }

    /// <summary>
    /// Result URL
    /// </summary>
    public required string Url { get; init; }

    /// <summary>
    /// Result snippet/description
    /// 结果摘要/描述
    /// </summary>
    public string? Snippet { get; init; }

    /// <summary>
    /// Source domain
    /// 来源域名
    /// </summary>
    public string? Source { get; init; }

    /// <summary>
    /// Result relevance score (0-1)
    /// 结果相关性分数 (0-1)
    /// </summary>
    public double? Score { get; init; }
}

/// <summary>
/// Web search tool for searching the internet
/// 网络搜索工具，用于搜索互联网
/// 
/// 支持可配置 Provider：无 key 或调用失败时优雅降级为模拟结果；
/// 可通过 UseSimulatedSearchOnly 强制仅使用模拟结果（测试或禁用 API 时）。
/// 参考: agentscope-java 的工具概念
/// </summary>
public class WebSearchTool : ToolBase
{
    private readonly HttpClient _httpClient;
    private readonly string? _searchEngineUrl;
    private readonly IWebSearchProvider? _provider;
    private static readonly IWebSearchProvider DefaultSimulatedProvider = new SimulatedWebSearchProvider();

    /// <summary>
    /// Maximum number of results to return
    /// 返回的最大结果数
    /// </summary>
    public int MaxResults { get; set; } = 10;

    /// <summary>
    /// Whether to include snippets in results
    /// 是否在结果中包含摘要
    /// </summary>
    public bool IncludeSnippets { get; set; } = true;

    /// <summary>
    /// Timeout for search requests
    /// 搜索请求超时
    /// </summary>
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// 当为 true 时，始终使用模拟结果，不调用任何外部 API 或 Provider。
    /// 用于测试或明确禁用真实搜索时。
    /// </summary>
    public bool UseSimulatedSearchOnly { get; set; }

    /// <summary>
    /// Creates a new web search tool（无 Provider 时使用自定义 URL 或模拟结果）
    /// 创建新网络搜索工具
    /// </summary>
    public WebSearchTool(HttpClient? httpClient = null, string? searchEngineUrl = null)
        : this(httpClient, searchEngineUrl, null)
    {
    }

    /// <summary>
    /// Creates a new web search tool with optional search provider.
    /// 使用可选搜索提供者创建。Provider 异常（如未配置 API Key）时会优雅降级为模拟结果。
    /// </summary>
    public WebSearchTool(HttpClient? httpClient, string? searchEngineUrl, IWebSearchProvider? provider)
        : base("web_search", "Search the web for information. Input should be a search query string.")
    {
        _httpClient = httpClient ?? new HttpClient();
        _searchEngineUrl = searchEngineUrl;
        _provider = provider;
    }

    /// <summary>
    /// Execute web search
    /// 执行网络搜索
    /// </summary>
    public override async Task<ToolResult> ExecuteAsync(Dictionary<string, object> parameters)
    {
        try
        {
            if (!parameters.TryGetValue("query", out var queryObj) || queryObj is not string query)
            {
                return ToolResult.Fail("Missing required parameter: query");
            }

            var results = await SearchAsync(query);

            var formatted = FormatResults(results);
            return new ToolResult
            {
                Success = true,
                Result = formatted
            };
        }
        catch (global::System.Exception ex)
        {
            return ToolResult.Fail($"Search failed: {ex.Message}");
        }
    }

    /// <summary>
    /// 上次搜索是否因降级使用了模拟结果（Provider/API 未配置或失败时为 true）
    /// </summary>
    public bool LastSearchWasFallback { get; private set; }

    /// <summary>
    /// Search the web. 无 key 或 Provider 失败时优雅降级为模拟结果。
    /// 搜索网络
    /// </summary>
    public virtual async Task<IReadOnlyList<WebSearchResult>> SearchAsync(string query)
    {
        LastSearchWasFallback = false;
        var results = new List<WebSearchResult>();

        if (UseSimulatedSearchOnly)
        {
            results.AddRange(await DefaultSimulatedProvider.SearchAsync(query, MaxResults));
            LastSearchWasFallback = true;
            return results;
        }

        if (_provider != null)
        {
            try
            {
                var providerResults = await _provider.SearchAsync(query, MaxResults, CancellationToken.None);
                return providerResults.Take(MaxResults).ToList();
            }
            catch (System.Exception)
            {
                // 优雅降级：无 key、网络错误等时使用模拟结果，便于错误定位
                LastSearchWasFallback = true;
                results.AddRange(await DefaultSimulatedProvider.SearchAsync(query, MaxResults));
                return results;
            }
        }

        if (!string.IsNullOrEmpty(_searchEngineUrl))
        {
            try
            {
                results.AddRange(await SearchWithEngineAsync(query));
                return results.Take(MaxResults).ToList();
            }
            catch (System.Exception)
            {
                LastSearchWasFallback = true;
                results.AddRange(await DefaultSimulatedProvider.SearchAsync(query, MaxResults));
                return results;
            }
        }

        // 默认：模拟结果
        LastSearchWasFallback = true;
        results.AddRange(await DefaultSimulatedProvider.SearchAsync(query, MaxResults));
        return results;
    }

    /// <summary>
    /// Search using a search engine API
    /// 使用搜索引擎 API 搜索
    /// </summary>
    protected virtual async Task<IReadOnlyList<WebSearchResult>> SearchWithEngineAsync(string query)
    {
        var results = new List<WebSearchResult>();
        var encodedQuery = HttpUtility.UrlEncode(query);
        var url = $"{_searchEngineUrl}?q={encodedQuery}&num={MaxResults}";

        using var cts = new CancellationTokenSource(Timeout);
        var response = await _httpClient.GetAsync(url, cts.Token);
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync();
        
        // Parse results - this would be customized based on the search engine API
        // For now, return empty list
        return results;
    }

    /// <summary>
    /// Format search results as string
    /// 将搜索结果格式化为字符串
    /// </summary>
    protected virtual string FormatResults(IReadOnlyList<WebSearchResult> results)
    {
        if (results.Count == 0)
        {
            return "No results found.";
        }

        var lines = new List<string>();
        if (LastSearchWasFallback)
        {
            lines.Add("[Simulated results - no search API key or provider error. Configure IWebSearchProvider or set UseSimulatedSearchOnly=false with a provider.]\n");
        }
        lines.Add($"Found {results.Count} results:\n");

        for (int i = 0; i < results.Count; i++)
        {
            var result = results[i];
            lines.Add($"[{i + 1}] {result.Title}");
            lines.Add($"    URL: {result.Url}");
            
            if (IncludeSnippets && !string.IsNullOrEmpty(result.Snippet))
            {
                lines.Add($"    {result.Snippet}");
            }
            
            lines.Add("");
        }

        return string.Join("\n", lines);
    }

    /// <summary>
    /// Get tool schema for LLM
    /// 获取 LLM 工具模式
    /// </summary>
    public override Dictionary<string, object> GetSchema()
    {
        return new Dictionary<string, object>
        {
            ["name"] = Name,
            ["description"] = Description,
            ["parameters"] = new Dictionary<string, object>
            {
                ["query"] = new Dictionary<string, object>
                {
                    ["type"] = "string",
                    ["description"] = "The search query string",
                    ["required"] = true
                },
                ["max_results"] = new Dictionary<string, object>
                {
                    ["type"] = "integer",
                    ["description"] = "Maximum number of results to return (default: 10)",
                    ["required"] = false
                }
            }
        };
    }
}

/// <summary>
/// Mock web search tool for testing
/// 用于测试的模拟网络搜索工具
/// </summary>
public class MockWebSearchTool : WebSearchTool
{
    private readonly List<WebSearchResult> _mockResults;

    public MockWebSearchTool(List<WebSearchResult>? mockResults = null)
    {
        _mockResults = mockResults ?? new List<WebSearchResult>();
    }

    public override Task<IReadOnlyList<WebSearchResult>> SearchAsync(string query)
    {
        var results = _mockResults.Any() 
            ? _mockResults 
            : new List<WebSearchResult>
            {
                new()
                {
                    Title = $"Mock result for: {query}",
                    Url = "https://mock.example.com",
                    Snippet = "This is a mock search result for testing.",
                    Source = "mock.example.com"
                }
            };

        return Task.FromResult<IReadOnlyList<WebSearchResult>>(results.Take(MaxResults).ToList());
    }
}
