// Copyright 2024-2026 the original author or authors.
// Licensed under the Apache License, Version 2.0

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
/// 参考: agentscope-java 的工具概念
/// </summary>
public class WebSearchTool : ToolBase
{
    private readonly HttpClient _httpClient;
    private readonly string? _searchEngineUrl;

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
    /// Creates a new web search tool
    /// 创建新网络搜索工具
    /// </summary>
    public WebSearchTool(HttpClient? httpClient = null, string? searchEngineUrl = null) 
        : base("web_search", "Search the web for information. Input should be a search query string.")
    {
        _httpClient = httpClient ?? new HttpClient();
        _searchEngineUrl = searchEngineUrl;
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
    /// Search the web
    /// 搜索网络
    /// </summary>
    public virtual async Task<IReadOnlyList<WebSearchResult>> SearchAsync(string query)
    {
        var results = new List<WebSearchResult>();

        if (!string.IsNullOrEmpty(_searchEngineUrl))
        {
            // Use custom search engine
            results.AddRange(await SearchWithEngineAsync(query));
        }
        else
        {
            // Simulate search results (in production, use real search API)
            results.AddRange(SimulateSearchResults(query));
        }

        return results.Take(MaxResults).ToList();
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
    /// Simulate search results (for testing/demo)
    /// 模拟搜索结果（用于测试/演示）
    /// </summary>
    protected virtual IReadOnlyList<WebSearchResult> SimulateSearchResults(string query)
    {
        // In a real implementation, this would call an actual search API
        // This is a placeholder that returns simulated results
        return new List<WebSearchResult>
        {
            new()
            {
                Title = $"Search results for: {query}",
                Url = $"https://example.com/search?q={HttpUtility.UrlEncode(query)}",
                Snippet = "This is a simulated search result. In production, integrate with a real search API like Google Custom Search, Bing API, or DuckDuckGo.",
                Source = "example.com",
                Score = 0.95
            },
            new()
            {
                Title = "How to integrate web search in your application",
                Url = "https://example.com/guide",
                Snippet = "To integrate real web search, you can use APIs like: Google Custom Search JSON API, Microsoft Bing Web Search API, SerpAPI, or DuckDuckGo Instant Answer API.",
                Source = "example.com",
                Score = 0.85
            },
            new()
            {
                Title = "Best practices for web search tools",
                Url = "https://example.com/best-practices",
                Snippet = "1. Cache results to reduce API calls. 2. Respect rate limits. 3. Handle errors gracefully. 4. Provide relevant snippets.",
                Source = "example.com",
                Score = 0.75
            }
        }.AsReadOnly();
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
