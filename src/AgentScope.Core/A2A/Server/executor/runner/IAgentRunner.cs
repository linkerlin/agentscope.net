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
using System.Runtime.CompilerServices;
using AgentScope.Core.Agent;
using AgentScope.Core.Events;
using AgentScope.Core.Message;

namespace AgentScope.Core.A2A.Server.Executor.Runner;

/// <summary>
/// Agent 运行抽象。对标 Java AgentRunner。
/// 为每个任务创建 Agent 实例并返回事件流。
/// </summary>
public interface IAgentRunner
{
    string AgentName { get; }
    string AgentDescription { get; }
    IAsyncEnumerable<Event> StreamAsync(IReadOnlyList<Msg> messages, AgentRequestOptions options, CancellationToken ct = default);
    Task StopAsync(string taskId, CancellationToken ct = default);
}

/// <summary>
/// 每个请求的可选参数。对标 Java AgentRequestOptions。
/// </summary>
public sealed record AgentRequestOptions(
    string? TaskId = null,
    string? SessionId = null,
    string? UserId = null);

/// <summary>
/// ReAct Agent Runner 抽象基类。对标 Java BaseReActAgentRunner。
/// 为每个 taskId 创建并缓存 Agent 实例，以便在收到取消请求时中断。
/// </summary>
public abstract class BaseReActAgentRunner : IAgentRunner
{
    private readonly ConcurrentDictionary<string, IAgent> _agentCache = new();

    public abstract string AgentName { get; }
    public abstract string AgentDescription { get; }

    /// <summary>构建新的 Agent 实例，由子类实现。</summary>
    protected abstract IAgent BuildAgent();

    /// <summary>
    /// 为请求流式执行。对标 Java BaseReActAgentRunner.stream。
    /// 每个 taskId 同一时刻只允许一个 Agent；执行结束后从缓存移除。
    /// </summary>
    public async IAsyncEnumerable<Event> StreamAsync(
        IReadOnlyList<Msg> messages, AgentRequestOptions options,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        var taskId = options.TaskId ?? Guid.NewGuid().ToString("N");
        if (_agentCache.ContainsKey(taskId))
            throw new InvalidOperationException($"Agent already exists for taskId: {taskId}");

        var agent = BuildAgent();
        _agentCache[taskId] = agent;
        try
        {
            await foreach (var evt in agent.StreamEventsAsync(messages, context: null).WithCancellation(ct))
                yield return evt;
        }
        finally
        {
            _agentCache.TryRemove(taskId, out _);
        }
    }

    /// <summary>
    /// 按 taskId 停止（中断）对应 Agent。对标 Java BaseReActAgentRunner.stop。
    /// </summary>
    public Task StopAsync(string taskId, CancellationToken ct = default)
    {
        if (_agentCache.TryRemove(taskId, out var agent))
            agent.Interrupt();
        return Task.CompletedTask;
    }
}
