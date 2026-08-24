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

using System.Text.RegularExpressions;
using AgentScope.Core.Agent;
using AgentScope.Core.Message;

namespace AgentScope.Core.MultiAgent;

/// <summary>
/// Routing rule for directing messages to specific agents based on conditions, keywords, or regex patterns.
/// Corresponds to Java: io.agentscope.core.multiagent.RoutingRule
/// 用于根据条件、关键词或正则表达式将消息定向到特定 Agent 的路由规则。
/// 对应 Java: io.agentscope.core.multiagent.RoutingRule
/// </summary>
public class RoutingRule
{
    /// <summary>
    /// Unique name for this routing rule.
    /// 此路由规则的唯一名称。
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Name of the target agent to route matching messages to.
    /// 匹配消息要路由到的目标 Agent 名称。
    /// </summary>
    public required string TargetAgent { get; init; }

    /// <summary>
    /// Priority of the rule (higher values are evaluated first).
    /// 规则的优先级（数值越大越先评估）。
    /// </summary>
    public int Priority { get; init; } = 0;

    /// <summary>
    /// Custom condition function for advanced matching logic.
    /// 用于高级匹配逻辑的自定义条件函数。
    /// </summary>
    public Func<Msg, bool>? Condition { get; init; }

    /// <summary>
    /// List of keywords that trigger this rule (content-based matching).
    /// 触发此规则的关键词列表（基于内容的匹配）。
    /// </summary>
    public List<string> Keywords { get; init; } = new();

    /// <summary>
    /// Regex pattern for matching message content.
    /// 用于匹配消息内容的正则表达式模式。
    /// </summary>
    public string? Pattern { get; init; }

    /// <summary>
    /// Human-readable description of the rule's purpose.
    /// 规则用途的人类可读描述。
    /// </summary>
    public string? Description { get; init; }
}

/// <summary>
/// Router for directing messages to appropriate agents based on routing rules.
/// Corresponds to Java: io.agentscope.core.multiagent.AgentRouter
/// 基于路由规则将消息定向到适当 Agent 的路由器。
/// 对应 Java: io.agentscope.core.multiagent.AgentRouter
/// </summary>
public class AgentRouter : IDisposable
{
    /// <summary>
    /// Dictionary of registered agents (name -> agent).
    /// 已注册 Agent 的字典（名称 -> Agent）。
    /// </summary>
    private readonly Dictionary<string, IAgent> _agents = new();

    /// <summary>
    /// List of routing rules, sorted by priority.
    /// 路由规则列表，按优先级排序。
    /// </summary>
    private readonly List<RoutingRule> _rules = new();

    /// <summary>
    /// Lock object for thread-safe access to agents and rules.
    /// 用于对 Agent 和规则进行线程安全访问的锁对象。
    /// </summary>
    private readonly object _lock = new();

    /// <summary>
    /// Default agent for messages that don't match any rule.
    /// 用于不匹配任何规则的消息的默认 Agent。
    /// </summary>
    private IAgent? _defaultAgent;

    /// <summary>
    /// Whether this router has been disposed.
    /// 此路由器是否已被释放。
    /// </summary>
    private bool _disposed;

    /// <summary>
    /// Gets or sets the router name.
    /// 获取或设置路由器名称。
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Gets the number of registered agents.
    /// 获取已注册 Agent 的数量。
    /// </summary>
    public int AgentCount => _agents.Count;

    /// <summary>
    /// Gets the number of routing rules (thread-safe).
    /// 获取路由规则数量（线程安全）。
    /// </summary>
    public int RuleCount 
    { 
        get 
        { 
            lock (_lock) return _rules.Count; 
        } 
    }

    /// <summary>
    /// Registers an agent with the router under the specified name.
    /// 使用指定名称向路由器注册一个 Agent。
    /// </summary>
    /// <param name="name">Unique name for the agent / Agent 的唯一名称</param>
    /// <param name="agent">The agent instance to register / 要注册的 Agent 实例</param>
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
    /// Unregisters an agent by name.
    /// 根据名称注销 Agent。
    /// </summary>
    /// <param name="name">Name of the agent to unregister / 要注销的 Agent 名称</param>
    /// <returns>True if removed successfully; false if not found / 移除成功返回 true；未找到返回 false</returns>
    public bool UnregisterAgent(string name)
    {
        lock (_lock)
        {
            return _agents.Remove(name);
        }
    }

    /// <summary>
    /// Sets the default agent for messages that don't match any routing rule.
    /// 为不匹配任何路由规则的消息设置默认 Agent。
    /// </summary>
    /// <param name="agent">The default agent / 默认 Agent</param>
    public void SetDefaultAgent(IAgent agent)
    {
        _defaultAgent = agent ?? throw new ArgumentNullException(nameof(agent));
    }

    /// <summary>
    /// Adds a routing rule. Rules are automatically sorted by priority (descending).
    /// 添加路由规则。规则会自动按优先级排序（降序）。
    /// </summary>
    /// <param name="rule">The routing rule to add / 要添加的路由规则</param>
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
    /// Removes a routing rule by its name.
    /// 根据名称移除路由规则。
    /// </summary>
    /// <param name="ruleName">Name of the rule to remove / 要移除的规则名称</param>
    /// <returns>True if removed successfully; false if not found / 移除成功返回 true；未找到返回 false</returns>
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
    /// Routes a message to the appropriate agent based on routing rules.
    /// 根据路由规则将消息路由到适当的 Agent。
    /// </summary>
    /// <param name="message">The message to route / 要路由的消息</param>
    /// <returns>The response from the matched agent / 匹配的 Agent 的响应</returns>
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
    /// Routes a message and returns both the agent name and response.
    /// 路由消息并返回 Agent 名称和响应。
    /// </summary>
    /// <param name="message">The message to route / 要路由的消息</param>
    /// <returns>A tuple containing the agent name and response / 包含 Agent 名称和响应的元组</returns>
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
    /// Selects the appropriate agent for a message based on routing rules.
    /// 根据路由规则为消息选择适当的 Agent。
    /// </summary>
    /// <param name="message">The message to route / 要路由的消息</param>
    /// <returns>The selected agent, or null if no match / 选中的 Agent，无匹配时返回 null</returns>
    private IAgent? SelectAgent(Msg message)
    {
        var (_, agent) = SelectAgentWithName(message);
        return agent;
    }

    /// <summary>
    /// Selects the appropriate agent and returns both name and instance.
    /// 选择适当的 Agent 并返回名称和实例。
    /// </summary>
    /// <param name="message">The message to route / 要路由的消息</param>
    /// <returns>A tuple with agent name and instance / 包含 Agent 名称和实例的元组</returns>
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

        // Fall back to default agent / 回退到默认 Agent
        if (_defaultAgent != null)
        {
            return ("default", _defaultAgent);
        }

        return (null, null);
    }

    /// <summary>
    /// Checks whether a message matches a given routing rule.
    /// 检查消息是否匹配给定的路由规则。
    /// </summary>
    /// <param name="message">The message to check / 要检查的消息</param>
    /// <param name="rule">The routing rule / 路由规则</param>
    /// <returns>True if the message matches the rule / 消息匹配规则时返回 true</returns>
    private bool MatchesRule(Msg message, RoutingRule rule)
    {
        // Check condition function first / 首先检查条件函数
        if (rule.Condition != null)
        {
            return rule.Condition(message);
        }

        // Check keywords in message content / 检查消息内容中的关键词
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

        // Check regex pattern against message content / 检查消息内容的正则表达式模式
        if (!string.IsNullOrEmpty(rule.Pattern))
        {
            var content = message.Content?.ToString() ?? string.Empty;
            return System.Text.RegularExpressions.Regex.IsMatch(content, rule.Pattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        }

        return false;
    }

    /// <summary>
    /// Creates a new AgentRouterBuilder for fluent configuration.
    /// 创建一个新的 AgentRouterBuilder 用于流畅配置。
    /// </summary>
    /// <returns>A new AgentRouterBuilder instance / 一个新的 AgentRouterBuilder 实例</returns>
    public static AgentRouterBuilder Builder()
    {
        return new AgentRouterBuilder();
    }

    /// <summary>
    /// Gets all registered agent names as a read-only list.
    /// 获取所有已注册的 Agent 名称的只读列表。
    /// </summary>
    /// <returns>Read-only list of agent names / Agent 名称的只读列表</returns>
    public IReadOnlyList<string> GetRegisteredAgentNames()
    {
        lock (_lock)
        {
            return _agents.Keys.ToList().AsReadOnly();
        }
    }

    /// <summary>
    /// Gets all routing rules as a read-only list.
    /// 获取所有路由规则的只读列表。
    /// </summary>
    /// <returns>Read-only list of routing rules / 路由规则的只读列表</returns>
    public IReadOnlyList<RoutingRule> GetRules()
    {
        lock (_lock)
        {
            return _rules.ToList().AsReadOnly();
        }
    }

    /// <summary>
    /// Disposes the router, clearing all agents and rules.
    /// 释放路由器，清除所有 Agent 和规则。
    /// </summary>
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
/// Fluent builder for configuring and creating an AgentRouter.
/// Corresponds to Java: io.agentscope.core.multiagent.AgentRouterBuilder
/// 用于流畅配置和创建 AgentRouter 的构建器。
/// 对应 Java: io.agentscope.core.multiagent.AgentRouterBuilder
/// </summary>
public class AgentRouterBuilder
{
    /// <summary>
    /// The router being built.
    /// 正在构建的路由器。
    /// </summary>
    private readonly AgentRouter _router = new();

    /// <summary>
    /// Temporary list of rules to be added during build.
    /// 在构建过程中要添加的临时规则列表。
    /// </summary>
    private readonly List<RoutingRule> _rules = new();

    /// <summary>
    /// Sets the router name.
    /// 设置路由器名称。
    /// </summary>
    /// <param name="name">The router name / 路由器名称</param>
    /// <returns>This builder instance for chaining / 此构建器实例，用于链式调用</returns>
    public AgentRouterBuilder Name(string name)
    {
        _router.Name = name;
        return this;
    }

    /// <summary>
    /// Registers an agent with the router.
    /// 向路由器注册一个 Agent。
    /// </summary>
    /// <param name="name">Unique agent name / Agent 的唯一名称</param>
    /// <param name="agent">The agent instance / Agent 实例</param>
    /// <returns>This builder instance for chaining / 此构建器实例，用于链式调用</returns>
    public AgentRouterBuilder RegisterAgent(string name, IAgent agent)
    {
        _router.RegisterAgent(name, agent);
        return this;
    }

    /// <summary>
    /// Sets the default agent for unmatched messages.
    /// 为不匹配的消息设置默认 Agent。
    /// </summary>
    /// <param name="agent">The default agent / 默认 Agent</param>
    /// <returns>This builder instance for chaining / 此构建器实例，用于链式调用</returns>
    public AgentRouterBuilder SetDefaultAgent(IAgent agent)
    {
        _router.SetDefaultAgent(agent);
        return this;
    }

    /// <summary>
    /// Adds a routing rule.
    /// 添加一条路由规则。
    /// </summary>
    /// <param name="rule">The routing rule to add / 要添加的路由规则</param>
    /// <returns>This builder instance for chaining / 此构建器实例，用于链式调用</returns>
    public AgentRouterBuilder AddRule(RoutingRule rule)
    {
        _rules.Add(rule);
        return this;
    }

    /// <summary>
    /// Adds a keyword-based routing rule.
    /// 添加一条基于关键词的路由规则。
    /// </summary>
    /// <param name="name">Rule name / 规则名称</param>
    /// <param name="targetAgent">Target agent name / 目标 Agent 名称</param>
    /// <param name="keywords">Keywords that trigger this rule / 触发此规则的关键词</param>
    /// <returns>This builder instance for chaining / 此构建器实例，用于链式调用</returns>
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

    /// <summary>
    /// Adds a condition-based routing rule with optional priority.
    /// 添加一条基于条件的路由规则，可指定优先级。
    /// </summary>
    /// <param name="name">Rule name / 规则名称</param>
    /// <param name="targetAgent">Target agent name / 目标 Agent 名称</param>
    /// <param name="condition">Condition function / 条件函数</param>
    /// <param name="priority">Rule priority (higher = evaluated first) / 规则优先级（数值越大越先评估）</param>
    /// <returns>This builder instance for chaining / 此构建器实例，用于链式调用</returns>
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

    /// <summary>
    /// Builds and returns the configured AgentRouter with all rules applied.
    /// 构建并返回配置好的 AgentRouter，应用所有规则。
    /// </summary>
    /// <returns>The configured AgentRouter / 配置好的 AgentRouter</returns>
    public AgentRouter Build()
    {
        foreach (var rule in _rules.OrderByDescending(r => r.Priority))
        {
            _router.AddRule(rule);
        }
        return _router;
    }
}
