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
using AgentScope.Core.Agent;
using AgentScope.Core.Message;

namespace AgentScope.Core.MultiAgent;

/// <summary>
/// Message distribution strategy for AgentGroup.
/// Corresponds to Java: io.agentscope.core.multiagent.DistributionStrategy
/// AgentGroup 消息分发策略。
/// 对应 Java: io.agentscope.core.multiagent.DistributionStrategy
/// </summary>
public enum DistributionStrategy
{
    /// <summary>
    /// Broadcast message to all agents in the group.
    /// 向组中所有 Agent 广播消息。
    /// </summary>
    Broadcast,
    
    /// <summary>
    /// Round-robin distribution across agents sequentially.
    /// 轮询分发，按顺序依次选择 Agent。
    /// </summary>
    RoundRobin,
    
    /// <summary>
    /// Random selection of an agent.
    /// 随机选择一个 Agent。
    /// </summary>
    Random,
    
    /// <summary>
    /// Load-based selection, picking the least busy agent.
    /// 基于负载选择，选择最不忙的 Agent。
    /// </summary>
    LoadBased,
    
    /// <summary>
    /// Select the first available agent.
    /// 选择第一个可用的 Agent。
    /// </summary>
    FirstAvailable
}

/// <summary>
/// Agent group for managing multiple agents with various distribution strategies.
/// Corresponds to Java: io.agentscope.core.multiagent.AgentGroup
/// 用于管理多个 Agent 的 Agent 组，支持多种分发策略。
/// 对应 Java: io.agentscope.core.multiagent.AgentGroup
/// </summary>
public class AgentGroup : IDisposable
{
    /// <summary>
    /// Thread-safe dictionary of registered agents (name -> agent).
    /// 已注册 Agent 的线程安全字典（名称 -> Agent）。
    /// </summary>
    private readonly ConcurrentDictionary<string, IAgent> _agents = new();

    /// <summary>
    /// Tracks the last activity time for each agent (for load-based selection).
    /// 跟踪每个 Agent 的最后活动时间（用于基于负载的选择）。
    /// </summary>
    private readonly ConcurrentDictionary<string, DateTime> _lastActivity = new();

    /// <summary>
    /// Tracks the current load count for each agent (for load-based selection).
    /// 跟踪每个 Agent 的当前负载计数（用于基于负载的选择）。
    /// </summary>
    private readonly ConcurrentDictionary<string, int> _loadCounters = new();

    /// <summary>
    /// The distribution strategy for selecting agents.
    /// 用于选择 Agent 的分发策略。
    /// </summary>
    private readonly DistributionStrategy _strategy;

    /// <summary>
    /// Optional group name.
    /// 可选的组名称。
    /// </summary>
    private readonly string? _name;

    /// <summary>
    /// Round-robin index counter, thread-safe via Interlocked.
    /// 轮询索引计数器，通过 Interlocked 实现线程安全。
    /// </summary>
    private int _roundRobinIndex = 0;

    /// <summary>
    /// Whether this group has been disposed.
    /// 此组是否已被释放。
    /// </summary>
    private bool _disposed;

    /// <summary>
    /// Gets the optional group name.
    /// 获取可选的组名称。
    /// </summary>
    public string? Name => _name;

    /// <summary>
    /// Gets the number of agents in the group.
    /// 获取组中 Agent 的数量。
    /// </summary>
    public int Count => _agents.Count;

    /// <summary>
    /// Gets all agent names in the group as a read-only collection.
    /// 获取组中所有 Agent 名称的只读集合。
    /// </summary>
    public IReadOnlyCollection<string> AgentNames => _agents.Keys.ToList().AsReadOnly();

    /// <summary>
    /// Initializes a new AgentGroup with an optional name and distribution strategy.
    /// 使用可选的名称和分发策略初始化一个新的 AgentGroup。
    /// </summary>
    /// <param name="name">Optional group name / 可选的组名称</param>
    /// <param name="strategy">Distribution strategy, defaults to RoundRobin / 分发策略，默认为轮询</param>
    public AgentGroup(string? name = null, DistributionStrategy strategy = DistributionStrategy.RoundRobin)
    {
        _name = name;
        _strategy = strategy;
    }

    /// <summary>
    /// Adds an agent to the group. Returns false if the agent is already registered.
    /// 向组中添加 Agent。如果 Agent 已注册则返回 false。
    /// </summary>
    /// <param name="agent">The agent to add / 要添加的 Agent</param>
    /// <returns>True if added successfully; false if already exists / 添加成功返回 true；已存在返回 false</returns>
    public bool AddAgent(IAgent agent)
    {
        if (agent == null)
            throw new ArgumentNullException(nameof(agent));

        var agentName = GetAgentName(agent);
        if (_agents.TryAdd(agentName, agent))
        {
            _lastActivity[agentName] = DateTime.UtcNow;
            _loadCounters[agentName] = 0;
            return true;
        }
        return false;
    }

    /// <summary>
    /// Removes an agent from the group by name.
    /// 根据名称从组中移除 Agent。
    /// </summary>
    /// <param name="agentName">Name of the agent to remove / 要移除的 Agent 名称</param>
    /// <returns>True if removed successfully; false if not found / 移除成功返回 true；未找到返回 false</returns>
    public bool RemoveAgent(string agentName)
    {
        if (_agents.TryRemove(agentName, out _))
        {
            _lastActivity.TryRemove(agentName, out _);
            _loadCounters.TryRemove(agentName, out _);
            return true;
        }
        return false;
    }

    /// <summary>
    /// Gets an agent by name.
    /// 根据名称获取 Agent。
    /// </summary>
    /// <param name="agentName">Name of the agent to retrieve / 要获取的 Agent 名称</param>
    /// <returns>The agent if found; null otherwise / 找到则返回 Agent，否则返回 null</returns>
    public IAgent? GetAgent(string agentName)
    {
        _agents.TryGetValue(agentName, out var agent);
        return agent;
    }

    /// <summary>
    /// Broadcasts a message to all agents in parallel and collects their responses.
    /// 并行向所有 Agent 广播消息并收集它们的响应。
    /// </summary>
    /// <param name="message">The message to broadcast / 要广播的消息</param>
    /// <returns>A dictionary mapping agent names to their responses / 将 Agent 名称映射到其响应的字典</returns>
    public async Task<Dictionary<string, Msg>> BroadcastAsync(Msg message)
    {
        var results = new Dictionary<string, Msg>();
        var tasks = new List<Task>();

        foreach (var (name, agent) in _agents)
        {
            tasks.Add(Task.Run(async () =>
            {
                try
                {
                    _loadCounters.AddOrUpdate(name, 1, (_, count) => count + 1);
                    var response = await agent.CallAsync(message);
                    lock (results)
                    {
                        results[name] = response;
                    }
                    _lastActivity[name] = DateTime.UtcNow;
                }
                catch (global::System.Exception ex)
                {
                    results[name] = Msg.Builder()
                        .Role("system")
                        .Content($"Error from agent {name}: {ex.Message}")
                        .Build();
                }
                finally
                {
                    _loadCounters.AddOrUpdate(name, 0, (_, count) => Math.Max(0, count - 1));
                }
            }));
        }

        await Task.WhenAll(tasks);
        return results;
    }

    /// <summary>
    /// Sends a message to one agent selected based on the distribution strategy.
    /// 根据分发策略选择一个 Agent 并发送消息。
    /// </summary>
    /// <param name="message">The message to send / 要发送的消息</param>
    /// <returns>The response from the selected agent / 所选 Agent 的响应</returns>
    public async Task<Msg> CallAsync(Msg message)
    {
        var agent = SelectAgent();
        if (agent == null)
        {
            return Msg.Builder()
                .Role("system")
                .Content("No agents available in the group")
                .Build();
        }

        var agentName = GetAgentName(agent);
        try
        {
            _loadCounters.AddOrUpdate(agentName, 1, (_, count) => count + 1);
            var response = await agent.CallAsync(message);
            _lastActivity[agentName] = DateTime.UtcNow;
            return response;
        }
        finally
        {
            _loadCounters.AddOrUpdate(agentName, 0, (_, count) => Math.Max(0, count - 1));
        }
    }

    /// <summary>
    /// Selects an agent based on the distribution strategy.
    /// 根据分发策略选择一个 Agent。
    /// </summary>
    /// <returns>The selected agent, or null if the group is empty / 选中的 Agent，组为空时返回 null</returns>
    private IAgent? SelectAgent()
    {
        if (_agents.IsEmpty)
            return null;

        var agentsList = _agents.ToList();

        return _strategy switch
        {
            DistributionStrategy.Broadcast => null, // Not applicable for single call / 不适用于单次调用
            DistributionStrategy.RoundRobin => SelectRoundRobin(agentsList),
            DistributionStrategy.Random => SelectRandom(agentsList),
            DistributionStrategy.LoadBased => SelectLoadBased(agentsList),
            DistributionStrategy.FirstAvailable => agentsList.FirstOrDefault().Value,
            _ => SelectRoundRobin(agentsList)
        };
    }

    /// <summary>
    /// Selects an agent using round-robin strategy.
    /// 使用轮询策略选择 Agent。
    /// </summary>
    private IAgent SelectRoundRobin(List<KeyValuePair<string, IAgent>> agents)
    {
        var index = Interlocked.Increment(ref _roundRobinIndex) % agents.Count;
        return agents[Math.Abs(index)].Value;
    }

    /// <summary>
    /// Selects an agent randomly.
    /// 随机选择一个 Agent。
    /// </summary>
    private IAgent SelectRandom(List<KeyValuePair<string, IAgent>> agents)
    {
        var index = System.Random.Shared.Next(agents.Count);
        return agents[index].Value;
    }

    /// <summary>
    /// Selects the least busy agent based on load counters and last activity time.
    /// 基于负载计数器和最后活动时间选择最不忙的 Agent。
    /// </summary>
    private IAgent SelectLoadBased(List<KeyValuePair<string, IAgent>> agents)
    {
        return agents
            .OrderBy(a => _loadCounters.GetValueOrDefault(a.Key, 0))
            .ThenBy(a => _lastActivity.GetValueOrDefault(a.Key, DateTime.MinValue))
            .First()
            .Value;
    }

    /// <summary>
    /// Generates a unique name for an agent based on its type and hash code.
    /// 根据 Agent 的类型和哈希码生成唯一名称。
    /// </summary>
    private static string GetAgentName(IAgent agent)
    {
        return agent.GetType().Name + "_" + agent.GetHashCode();
    }

    /// <summary>
    /// Gets current load statistics for all agents.
    /// 获取所有 Agent 的当前负载统计信息。
    /// </summary>
    /// <returns>A dictionary mapping agent names to their load info / 将 Agent 名称映射到其负载信息的字典</returns>
    public Dictionary<string, AgentLoadInfo> GetLoadStatistics()
    {
        return _agents.ToDictionary(
            a => a.Key,
            a => new AgentLoadInfo
            {
                CurrentLoad = _loadCounters.GetValueOrDefault(a.Key, 0),
                LastActivity = _lastActivity.GetValueOrDefault(a.Key, DateTime.MinValue)
            }
        );
    }

    /// <summary>
    /// Disposes the agent group, clearing all agents and state.
    /// 释放 Agent 组，清除所有 Agent 和状态。
    /// </summary>
    public void Dispose()
    {
        if (!_disposed)
        {
            _agents.Clear();
            _lastActivity.Clear();
            _loadCounters.Clear();
            _disposed = true;
        }
    }
}

/// <summary>
/// Represents load information for a single agent.
/// Corresponds to Java: io.agentscope.core.multiagent.AgentLoadInfo
/// 表示单个 Agent 的负载信息。
/// 对应 Java: io.agentscope.core.multiagent.AgentLoadInfo
/// </summary>
public class AgentLoadInfo
{
    /// <summary>
    /// Current load count (number of concurrent tasks).
    /// 当前负载计数（并发任务数）。
    /// </summary>
    public int CurrentLoad { get; set; }

    /// <summary>
    /// Timestamp of the last activity.
    /// 最后活动时间戳。
    /// </summary>
    public DateTime LastActivity { get; set; }
}
