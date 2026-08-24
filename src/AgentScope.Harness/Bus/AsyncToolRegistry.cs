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
using AgentScope.Core.Tool;

namespace AgentScope.Harness.Bus;

/// <summary>
/// Async tool registry interface, corresponding to Java AsyncToolRegistry.<br />
/// 异步工具注册中心接口，对应 Java AsyncToolRegistry。
/// </summary>
public interface IAsyncToolRegistry
{
    /// <summary>
    /// Register a tool for the given task ID.<br />
    /// 为指定任务 ID 注册工具。
    /// </summary>
    /// <param name="taskId">The task identifier / 任务标识符</param>
    /// <param name="tool">The tool instance / 工具实例</param>
    void Register(string taskId, ITool tool);

    /// <summary>
    /// Resolve the tool registered under the given task ID.<br />
    /// 解析指定任务 ID 下注册的工具。
    /// </summary>
    /// <param name="taskId">The task identifier / 任务标识符</param>
    /// <returns>The tool if found; otherwise <c>null</c> / 找到则返回工具，否则返回 null</returns>
    ITool? Resolve(string taskId);

    /// <summary>
    /// Unregister the tool for the given task ID.<br />
    /// 注销指定任务 ID 对应的工具。
    /// </summary>
    /// <param name="taskId">The task identifier / 任务标识符</param>
    /// <returns><c>true</c> if the tool was removed; otherwise <c>false</c> / 成功移除返回 true，否则返回 false</returns>
    bool Unregister(string taskId);
}

/// <summary>
/// Default async tool registry implementation, backed by <see cref="ConcurrentDictionary{TKey,TValue}"/>.<br />
/// 默认异步工具注册中心实现，基于 <see cref="ConcurrentDictionary{TKey,TValue}"/>。
/// </summary>
public sealed class AsyncToolRegistry : IAsyncToolRegistry
{
    /// <summary>Thread-safe tool storage / 线程安全的工具存储</summary>
    private readonly ConcurrentDictionary<string, ITool> _tools = new();

    /// <inheritdoc />
    public void Register(string taskId, ITool tool) => _tools[taskId] = tool;

    /// <inheritdoc />
    public ITool? Resolve(string taskId) => _tools.TryGetValue(taskId, out var t) ? t : null;

    /// <inheritdoc />
    public bool Unregister(string taskId) => _tools.TryRemove(taskId, out _);
}
