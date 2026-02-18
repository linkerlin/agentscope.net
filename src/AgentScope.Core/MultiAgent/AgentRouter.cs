// Copyright 2024-2026 the original author or authors.
// Licensed under the Apache License, Version 2.0

using System.Text.RegularExpressions;
using AgentScope.Core.Agent;
using AgentScope.Core.Message;

namespace AgentScope.Core.MultiAgent;

/// <summary>
/// Routing rule for directing messages to specific agents
/// 将消息定向到特定Agent的路由规则
/// </summary>
public class RoutingRule
{
    /// <summary>
    /// Rule name
    /// 规则名称
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Target agent name
    /// 目标Agent名称
    /// </summary>
    public required string TargetAgent { get; init; }

    /// <summary>
    /// Priority (higher = evaluated first)
    /// 优先级（数值越大越先评估）
    /// </summary>
    public int Priority { get; init; } = 0;

    /// <summary>
    /// Condition function
    /// 条件函数
    /// </summary>
    public Func<Msg, bool>? Condition { get; init; }

    /// <summary>
    /// Keywords that trigger this rule
    /// 触发此规则的关键词
    /// </summary>
    public List<string> Keywords { get; init; } = new();

    /// <summary>
    /// Regex pattern for matching
    /// 用于匹配的正则表达式
    /// </summary>
    public string? Pattern { get; init; }

    /// <summary>
    /// Description of the rule
    /// 规则描述
    /// </summary>
    public string? Description { get; init; }
}

/// <summary>
/// Router for directing messages to appropriate agents
/// 将消息路由到适当Agent的路由器
/// 
/// 参考: agentscope-java 的路由概念
/// </summary>
public class AgentRouter : IDisposable
{
    private readonly Dictionary<string, IAgent> _agents = new();
    private readonly List<RoutingRule> _rules = new();
    private readonly object _lock = new();
    private IAgent? _defaultAgent;
    private bool _disposed;

    /// <summary>
    /// Router name
    /// 路由器名称
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Number of registered agents
    /// 已注册Agent数量
    /// </summary>
    public int AgentCount => _agents.Count;

    /// <summary>
    /// Number of routing rules
    /// 路由规则数量
    /// </summary>
    public int RuleCount 
    { 
        get 
        { 
            lock (_lock) return _rules.Count; 
        } 
    }

    /// <summary>
    /// Registers an agent with the router
    /// 向路由器注册Agent
    /// </summary>
    public void RegisterAgent(string name, IAgent agent)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Agent name cannot be empty", nameof(name));
        if (agent == null)
            throw new ArgumentNullException(nameof(agent));

        lock (_lock)
        {
            _agents[name] = agent;
        }
    }

    /// <summary>
    /// Unregisters an agent
    /// 注销Agent
    /// </summary>
    public bool UnregisterAgent(string name)
    {
        lock (_lock)
        {
            return _agents.Remove(name);
        }
    }

    /// <summary>
    /// Sets the default agent for messages that don't match any rule
    /// 为不匹配任何规则的消息设置默认Agent
    /// </summary>
    public void SetDefaultAgent(IAgent agent)
    {
        _defaultAgent = agent ?? throw new ArgumentNullException(nameof(agent));
    }

    /// <summary>
    /// Adds a routing rule
    /// 添加路由规则
    /// </summary>
    public void AddRule(RoutingRule rule)
    {
        if (rule == null)
            throw new ArgumentNullException(nameof(rule));

        lock (_lock)
        {
            _rules.Add(rule);
            _rules.Sort((a, b) => b.Priority.CompareTo(a.Priority));
        }
    }

    /// <summary>
    /// Removes a routing rule by name
    /// 根据名称移除路由规则
    /// </summary>
    public bool RemoveRule(string ruleName)
    {
        lock (_lock)
        {
            var rule = _rules.FirstOrDefault(r => r.Name == ruleName);
            if (rule != null)
            {
                return _rules.Remove(rule);
            }
            return false;
        }
    }

    /// <summary>
    /// Routes a message to the appropriate agent
    /// 将消息路由到适当的Agent
    /// </summary>
    public async Task<Msg> RouteAsync(Msg message)
    {
        var agent = SelectAgent(message);
        if (agent == null)
        {
            return Msg.Builder()
                .Role("system")
                .Content("No suitable agent found for this message")
                .Build();
        }

        return await agent.CallAsync(message);
    }

    /// <summary>
    /// Routes a message and returns the agent name and response
    /// 路由消息并返回Agent名称和响应
    /// </summary>
    public async Task<(string? AgentName, Msg Response)> RouteWithInfoAsync(Msg message)
    {
        var (agentName, agent) = SelectAgentWithName(message);
        if (agent == null)
        {
            return (null, Msg.Builder()
                .Role("system")
                .Content("No suitable agent found for this message")
                .Build());
        }

        var response = await agent.CallAsync(message);
        return (agentName, response);
    }

    /// <summary>
    /// Selects the appropriate agent for a message
    /// 为消息选择适当的Agent
    /// </summary>
    private IAgent? SelectAgent(Msg message)
    {
        var (_, agent) = SelectAgentWithName(message);
        return agent;
    }

    private (string? Name, IAgent? Agent) SelectAgentWithName(Msg message)
    {
        lock (_lock)
        {
            foreach (var rule in _rules)
            {
                if (MatchesRule(message, rule))
                {
                    if (_agents.TryGetValue(rule.TargetAgent, out var agent))
                    {
                        return (rule.TargetAgent, agent);
                    }
                }
            }
        }

        // Fall back to default agent
        if (_defaultAgent != null)
        {
            return ("default", _defaultAgent);
        }

        return (null, null);
    }

    private bool MatchesRule(Msg message, RoutingRule rule)
    {
        // Check condition function
        if (rule.Condition != null)
        {
            return rule.Condition(message);
        }

        // Check keywords
        if (rule.Keywords.Count > 0)
        {
            var content = message.Content?.ToString()?.ToLower() ?? string.Empty;
            foreach (var k in rule.Keywords)
            {
                var keyword = k?.ToLower() ?? string.Empty;
                if (!string.IsNullOrEmpty(keyword) && content.Contains(keyword))
                    return true;
            }
            return false;
        }

        // Check regex pattern
        if (!string.IsNullOrEmpty(rule.Pattern))
        {
            var content = message.Content?.ToString() ?? string.Empty;
            return System.Text.RegularExpressions.Regex.IsMatch(content, rule.Pattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        }

        return false;
    }

    /// <summary>
    /// Creates a rule-based builder for fluent configuration
    /// 创建基于规则的构建器用于流畅配置
    /// </summary>
    public static AgentRouterBuilder Builder()
    {
        return new AgentRouterBuilder();
    }

    /// <summary>
    /// Gets all registered agent names
    /// 获取所有已注册的Agent名称
    /// </summary>
    public IReadOnlyList<string> GetRegisteredAgentNames()
    {
        lock (_lock)
        {
            return _agents.Keys.ToList().AsReadOnly();
        }
    }

    /// <summary>
    /// Gets all routing rules
    /// 获取所有路由规则
    /// </summary>
    public IReadOnlyList<RoutingRule> GetRules()
    {
        lock (_lock)
        {
            return _rules.ToList().AsReadOnly();
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            lock (_lock)
            {
                _agents.Clear();
                _rules.Clear();
            }
            _disposed = true;
        }
    }
}

/// <summary>
/// Builder for configuring AgentRouter
/// AgentRouter 的配置构建器
/// </summary>
public class AgentRouterBuilder
{
    private readonly AgentRouter _router = new();
    private readonly List<RoutingRule> _rules = new();

    public AgentRouterBuilder Name(string name)
    {
        _router.Name = name;
        return this;
    }

    public AgentRouterBuilder RegisterAgent(string name, IAgent agent)
    {
        _router.RegisterAgent(name, agent);
        return this;
    }

    public AgentRouterBuilder SetDefaultAgent(IAgent agent)
    {
        _router.SetDefaultAgent(agent);
        return this;
    }

    public AgentRouterBuilder AddRule(RoutingRule rule)
    {
        _rules.Add(rule);
        return this;
    }

    public AgentRouterBuilder AddRule(string name, string targetAgent, params string[] keywords)
    {
        _rules.Add(new RoutingRule
        {
            Name = name,
            TargetAgent = targetAgent,
            Keywords = keywords.ToList()
        });
        return this;
    }

    public AgentRouterBuilder AddRule(string name, string targetAgent, Func<Msg, bool> condition, int priority = 0)
    {
        _rules.Add(new RoutingRule
        {
            Name = name,
            TargetAgent = targetAgent,
            Condition = condition,
            Priority = priority
        });
        return this;
    }

    public AgentRouter Build()
    {
        foreach (var rule in _rules.OrderByDescending(r => r.Priority))
        {
            _router.AddRule(rule);
        }
        return _router;
    }
}
