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

using System.Web;

namespace AgentScope.Core.Tool;

/// <summary>
/// 模拟搜索提供者，用于测试或未配置真实 API 时的默认行为。
/// </summary>
public class SimulatedWebSearchProvider : IWebSearchProvider
{
    /// <inheritdoc />
    public Task<IReadOnlyList<WebSearchResult>> SearchAsync(
        string query,
        int maxResults,
        CancellationToken cancellationToken = default)
    {
        var results = new List<WebSearchResult>
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
        };

        return Task.FromResult<IReadOnlyList<WebSearchResult>>(
            results.Take(maxResults).ToList());
    }
}
