// Copyright 2024-2026 the original author or authors.
// Licensed under the Apache License, Version 2.0

namespace AgentScope.Core.Tool;

/// <summary>
/// Web 搜索提供者接口。无 key 或调用失败时由 WebSearchTool 优雅降级到模拟结果。
/// </summary>
public interface IWebSearchProvider
{
    /// <summary>
    /// 执行搜索。实现方应抛出异常以触发降级（如未配置 API Key、网络错误等）。
    /// </summary>
    /// <param name="query">搜索关键词</param>
    /// <param name="maxResults">最大结果数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>搜索结果列表</returns>
    Task<IReadOnlyList<WebSearchResult>> SearchAsync(
        string query,
        int maxResults,
        CancellationToken cancellationToken = default);
}
