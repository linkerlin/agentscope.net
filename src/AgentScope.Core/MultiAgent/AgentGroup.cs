// Copyright 2024-2026 the original author or authors.
// Licensed under the Apache License, Version 2.0

using System.Collections.Concurrent;
using AgentScope.Core.Agent;
using AgentScope.Core.Message;

namespace AgentScope.Core.MultiAgent;

/// <summary>
/// Message distribution strategy for AgentGroup
/// AgentGroup 消息分发策略
/// </summary>
public enum DistributionStrategy
{
    /// <summary>
    /// Broadcast to all agents
    /// 广播给所有Agent
    /// </summary>
    Broadcast,
    
    /// <summary>
    /// Round-robin distribution
    /// 轮询分发
    /// </summary>
    RoundRobin,
    
    /// <summary>
    /// Random selection
    /// 随机选择
    /// </summary>
    Random,
    
    /// <summary>
    /// Load-based selection (least busy)
    /// 基于负载选择（选择最不忙的）
    /// </summary>
    LoadBased,
    
    /// <summary>
    /// First available
    /// 第一个可用的
    /// </summary>
    FirstAvailable
}

/// <summary>
/// Agent group for managing multiple agents
/// 用于管理多个Agent的Agent组
/// 
/// 参考: agentscope-java 的 AgentGroup 概念
/// </summary>
public class AgentGroup : IDisposable
{
    private readonly ConcurrentDictionary<string, IAgent> _agents = new();
    private readonly ConcurrentDictionary<string, DateTime> _lastActivity = new();
    private readonly ConcurrentDictionary<string, int> _loadCounters = new();
    private readonly DistributionStrategy _strategy;
    private readonly string? _name;
    private int _roundRobinIndex = 0;
    private bool _disposed;

    /// <summary>
    /// Group name
    /// 组名称
    /// </summary>
    public string? Name => _name;

    /// <summary>
    /// Number of agents in the group
    /// 组中Agent数量
    /// </summary>
    public int Count => _agents.Count;

    /// <summary>
    /// All agent names in the group
    /// 组中所有Agent名称
    /// </summary>
    public IReadOnlyCollection<string> AgentNames => _agents.Keys.ToList().AsReadOnly();

    /// <summary>
    /// Creates a new agent group
    /// 创建新的Agent组
    /// </summary>
    public AgentGroup(string? name = null, DistributionStrategy strategy = DistributionStrategy.RoundRobin)
    {
        _name = name;
        _strategy = strategy;
    }

    /// <summary>
    /// Adds an agent to the group
    /// 向组中添加Agent
    /// </summary>
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
    /// Removes an agent from the group
    /// 从组中移除Agent
    /// </summary>
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
    /// Gets an agent by name
    /// 根据名称获取Agent
    /// </summary>
    public IAgent? GetAgent(string agentName)
    {
        _agents.TryGetValue(agentName, out var agent);
        return agent;
    }

    /// <summary>
    /// Broadcasts a message to all agents
    /// 向所有Agent广播消息
    /// </summary>
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
    /// Sends a message to one agent based on distribution strategy
    /// 根据分发策略向一个Agent发送消息
    /// </summary>
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
    /// Selects an agent based on the distribution strategy
    /// 根据分发策略选择Agent
    /// </summary>
    private IAgent? SelectAgent()
    {
        if (_agents.IsEmpty)
            return null;

        var agentsList = _agents.ToList();

        return _strategy switch
        {
            DistributionStrategy.Broadcast => null, // Not applicable for single call
            DistributionStrategy.RoundRobin => SelectRoundRobin(agentsList),
            DistributionStrategy.Random => SelectRandom(agentsList),
            DistributionStrategy.LoadBased => SelectLoadBased(agentsList),
            DistributionStrategy.FirstAvailable => agentsList.FirstOrDefault().Value,
            _ => SelectRoundRobin(agentsList)
        };
    }

    private IAgent SelectRoundRobin(List<KeyValuePair<string, IAgent>> agents)
    {
        var index = Interlocked.Increment(ref _roundRobinIndex) % agents.Count;
        return agents[Math.Abs(index)].Value;
    }

    private IAgent SelectRandom(List<KeyValuePair<string, IAgent>> agents)
    {
        var index = System.Random.Shared.Next(agents.Count);
        return agents[index].Value;
    }

    private IAgent SelectLoadBased(List<KeyValuePair<string, IAgent>> agents)
    {
        return agents
            .OrderBy(a => _loadCounters.GetValueOrDefault(a.Key, 0))
            .ThenBy(a => _lastActivity.GetValueOrDefault(a.Key, DateTime.MinValue))
            .First()
            .Value;
    }

    private static string GetAgentName(IAgent agent)
    {
        return agent.GetType().Name + "_" + agent.GetHashCode();
    }

    /// <summary>
    /// Gets current load statistics for all agents
    /// 获取所有Agent的当前负载统计
    /// </summary>
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
/// Agent load information
/// Agent负载信息
/// </summary>
public class AgentLoadInfo
{
    public int CurrentLoad { get; set; }
    public DateTime LastActivity { get; set; }
}
