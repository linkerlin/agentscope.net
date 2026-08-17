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

using AgentScope.Core.Agent;
using AgentScope.Core.Message;

namespace AgentScope.Core.MultiAgent;

/// <summary>
/// Coordination strategy for multi-agent collaboration
/// 多Agent协作的协调策略
/// </summary>
public enum CoordinationStrategy
{
    /// <summary>
    /// Sequential execution (one after another)
    /// 顺序执行（一个接一个）
    /// </summary>
    Sequential,

    /// <summary>
    /// Parallel execution (all at once)
    /// 并行执行（同时进行）
    /// </summary>
    Parallel,

    /// <summary>
    /// Consensus-based (agents vote/discuss)
    /// 基于共识（Agent投票/讨论）
    /// </summary>
    Consensus,

    /// <summary>
    /// Hierarchical (leader delegates to workers)
    /// 层级式（领导者委派给工作者）
    /// </summary>
    Hierarchical,

    /// <summary>
    /// Competitive (agents compete, best result wins)
    /// 竞争式（Agent竞争，最佳结果获胜）
    /// </summary>
    Competitive
}

/// <summary>
/// Result from coordinated agent execution.
/// 协调Agent执行的结果。
/// </summary>
public class CoordinatedResult
{
    /// <summary>
    /// Whether the coordination was successful.
    /// 协调是否成功。
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// Final aggregated response from all agents.
    /// 所有Agent的最终聚合响应。
    /// </summary>
    public required Msg FinalResponse { get; init; }

    /// <summary>
    /// Individual responses from each agent, keyed by agent name.
    /// 每个Agent的单独响应，以Agent名称为键。
    /// </summary>
    public Dictionary<string, Msg> AgentResponses { get; init; } = new();

    /// <summary>
    /// Coordination metadata (rounds, strategy, etc.).
    /// 协调元数据（轮次、策略等）。
    /// </summary>
    public Dictionary<string, object> Metadata { get; init; } = new();

    /// <summary>
    /// Total execution time of the coordination.
    /// 协调的总执行时间。
    /// </summary>
    public TimeSpan ExecutionTime { get; init; }
}

/// <summary>
/// Coordinates multiple agents to work together on a task.
/// 协调多个Agent协同工作。
/// 
/// Reference: agentscope-java multi-agent coordination concept
/// 参考: agentscope-java 的多Agent协调概念
/// </summary>
public class AgentCoordinator : IDisposable
{
    // Registered agents, keyed by name
    // 已注册的Agent，以名称为键
    private readonly Dictionary<string, IAgent> _agents = new();
    private readonly CoordinationStrategy _strategy;
    private readonly string? _coordinatorAgentName;
    private bool _disposed;

    /// <summary>
    /// Creates a new agent coordinator with the specified strategy.
    /// 使用指定的策略创建新的Agent协调器。
    /// </summary>
    /// <param name="strategy">Coordination strategy / 协调策略</param>
    /// <param name="coordinatorAgentName">Optional name of the coordinator agent for hierarchical strategy / 可选，层级策略中协调者Agent的名称</param>
    public AgentCoordinator(
        CoordinationStrategy strategy = CoordinationStrategy.Sequential,
        string? coordinatorAgentName = null)
    {
        _strategy = strategy;
        _coordinatorAgentName = coordinatorAgentName;
    }

    /// <summary>
    /// Registers an agent for coordination.
    /// 注册Agent用于协调。
    /// </summary>
    /// <param name="name">Unique agent name / 唯一的Agent名称</param>
    /// <param name="agent">The agent instance / Agent实例</param>
    /// <exception cref="ArgumentException">Thrown when name is empty / 名称为空时抛出</exception>
    /// <exception cref="ArgumentNullException">Thrown when agent is null / Agent为null时抛出</exception>
    public void RegisterAgent(string name, IAgent agent)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Agent name cannot be empty", nameof(name));
        if (agent == null)
            throw new ArgumentNullException(nameof(agent));

        lock (_agents)
        {
            _agents[name] = agent;
        }
    }

    /// <summary>
    /// Coordinates agents to process a message using the configured strategy.
    /// 使用配置的策略协调Agent处理消息。
    /// </summary>
    /// <param name="message">The message to process / 要处理的消息</param>
    /// <returns>Coordination result containing responses / 包含响应的协调结果</returns>
    public async Task<CoordinatedResult> CoordinateAsync(Msg message)
    {
        var startTime = DateTime.UtcNow;

        try
        {
            return _strategy switch
            {
                CoordinationStrategy.Sequential => await ExecuteSequentialAsync(message),
                CoordinationStrategy.Parallel => await ExecuteParallelAsync(message),
                CoordinationStrategy.Consensus => await ExecuteConsensusAsync(message),
                CoordinationStrategy.Hierarchical => await ExecuteHierarchicalAsync(message),
                CoordinationStrategy.Competitive => await ExecuteCompetitiveAsync(message),
                _ => await ExecuteSequentialAsync(message)
            };
        }
        catch (global::System.Exception ex)
        {
            return new CoordinatedResult
            {
                Success = false,
                FinalResponse = Msg.Builder()
                    .Role("system")
                    .Content($"Coordination failed: {ex.Message}")
                    .Build(),
                ExecutionTime = DateTime.UtcNow - startTime
            };
        }
    }

        /// <summary>
        /// Sequential execution: each agent processes the output of the previous one in chain.
        /// 顺序执行：每个Agent依次处理前一个Agent的输出。
        /// </summary>
        private async Task<CoordinatedResult> ExecuteSequentialAsync(Msg message)
    {
        var responses = new Dictionary<string, Msg>();
        var currentMessage = message;

        var agentsList = _agents.ToList();
        foreach (var kvp in agentsList)
        {
            var response = await kvp.Value.CallAsync(currentMessage);
            responses[kvp.Key] = response;
            currentMessage = Msg.Builder()
                .Role("user")
                .Content($"Previous agent ({kvp.Key}) responded: {response.Content}\n\nContinue the task.")
                .Build();
        }

        var lastResponse = responses.LastOrDefault();
        return new CoordinatedResult
        {
            Success = true,
            FinalResponse = lastResponse.Value ?? message,
            AgentResponses = responses
        };
    }

        /// <summary>
        /// Parallel execution: all agents process the same message simultaneously.
        /// 并行执行：所有Agent同时处理相同消息。
        /// </summary>
        private async Task<CoordinatedResult> ExecuteParallelAsync(Msg message)
    {
        var responses = new Dictionary<string, Msg>();
        var tasks = _agents.Select(async kvp =>
        {
            try
            {
                var response = await kvp.Value.CallAsync(message);
                lock (responses)
                {
                    responses[kvp.Key] = response;
                }
            }
            catch (global::System.Exception ex)
            {
                lock (responses)
                {
                    responses[kvp.Key] = Msg.Builder()
                        .Role("system")
                        .Content($"Error: {ex.Message}")
                        .Build();
                }
            }
        });

        await Task.WhenAll(tasks);

        // Aggregate responses
        var aggregatedContent = string.Join("\n\n---\n\n", 
            responses.Select(r => $"[{r.Key}]: {r.Value.Content}"));

        return new CoordinatedResult
        {
            Success = true,
            FinalResponse = Msg.Builder()
                .Role("assistant")
                .Content(aggregatedContent)
                .Build(),
            AgentResponses = responses
        };
    }

        /// <summary>
        /// Consensus execution: agents discuss iteratively until reaching agreement or max rounds.
        /// 共识执行：Agent迭代讨论直到达成一致或达到最大轮次。
        /// </summary>
        private async Task<CoordinatedResult> ExecuteConsensusAsync(Msg message)
    {
        var responses = new Dictionary<string, Msg>();
        var round = 0;
        var maxRounds = 3;

        // Initial round - all agents respond
        List<KeyValuePair<string, IAgent>> agents;
        lock (_agents)
        {
            agents = _agents.ToList();
        }

        var currentMessage = message;

        while (round < maxRounds)
        {
            round++;
            var roundResponses = new Dictionary<string, Msg>();

            foreach (var agent in agents)
            {
                var response = await agent.Value.CallAsync(currentMessage);
                roundResponses[agent.Key] = response;
                responses[$"{agent.Key}_round{round}"] = response;
            }

            // Check for consensus (simplified - in practice would use LLM to evaluate)
            if (roundResponses.Values.Select(r => r.Content).Distinct().Count() == 1)
            {
                return new CoordinatedResult
                {
                    Success = true,
                    FinalResponse = roundResponses.First().Value,
                    AgentResponses = responses,
                    Metadata = new Dictionary<string, object> { ["rounds"] = round }
                };
            }

            // Prepare message for next round with all responses
            var discussion = string.Join("\n", roundResponses.Select(r => $"{r.Key}: {r.Value.Content}"));
            currentMessage = Msg.Builder()
                .Role("user")
                .Content($"Round {round} responses:\n{discussion}\n\nPlease refine your answer based on others' responses.")
                .Build();
        }

        // Return the most common response or coordinator's synthesis
        var mostCommon = responses.Values
            .GroupBy(r => r.Content)
            .OrderByDescending(g => g.Count())
            .First()
            .First();

        return new CoordinatedResult
        {
            Success = true,
            FinalResponse = mostCommon,
            AgentResponses = responses,
            Metadata = new Dictionary<string, object> { ["rounds"] = round, ["consensus"] = false }
        };
    }

        /// <summary>
        /// Hierarchical execution: a coordinator agent delegates work to worker agents and synthesizes results.
        /// 层级执行：协调者Agent委派给工作者Agent并综合结果。
        /// </summary>
        private async Task<CoordinatedResult> ExecuteHierarchicalAsync(Msg message)
    {
        // If there's a coordinator agent, use it to delegate
        IAgent? coordinator = null;
        
        lock (_agents)
        {
            if (!string.IsNullOrEmpty(_coordinatorAgentName))
            {
                _agents.TryGetValue(_coordinatorAgentName, out coordinator);
            }
            // Otherwise use first agent as coordinator
            coordinator ??= _agents.FirstOrDefault().Value;
        }

        if (coordinator == null)
        {
            return new CoordinatedResult
            {
                Success = false,
                FinalResponse = Msg.Builder()
                    .Role("system")
                    .Content("No coordinator agent available")
                    .Build()
            };
        }

        // Workers execute in parallel
        var workerResponses = new Dictionary<string, Msg>();
        
        var workerTasks = _agents
            .Where(a => a.Value != coordinator)
            .Select(async kvp =>
        {
            try
            {
                var response = await kvp.Value.CallAsync(message);
                lock (workerResponses)
                {
                    workerResponses[kvp.Key] = response;
                }
            }
            catch (global::System.Exception ex)
            {
                lock (workerResponses)
                {
                    workerResponses[kvp.Key] = Msg.Builder()
                        .Role("system")
                        .Content($"Error: {ex.Message}")
                        .Build();
                }
            }
        });

        await Task.WhenAll(workerTasks);

        // Coordinator synthesizes results
        var synthesisPrompt = Msg.Builder()
            .Role("user")
            .Content($"Original task: {message.Content}\n\nWorker responses:\n" +
                string.Join("\n", workerResponses.Select(r => $"{r.Key}: {r.Value.Content}")) +
                "\n\nPlease synthesize these responses into a final answer.")
            .Build();

        var finalResponse = await coordinator.CallAsync(synthesisPrompt);

        return new CoordinatedResult
        {
            Success = true,
            FinalResponse = finalResponse,
            AgentResponses = workerResponses,
            Metadata = new Dictionary<string, object> { ["coordinator"] = _coordinatorAgentName ?? "default" }
        };
    }

        /// <summary>
        /// Competitive execution: all agents respond, then the best result is selected.
        /// 竞争执行：所有Agent响应，然后选择最佳结果。
        /// </summary>
        private async Task<CoordinatedResult> ExecuteCompetitiveAsync(Msg message)
    {
        // Execute all agents in parallel
        var results = await ExecuteParallelAsync(message);

        // In a real implementation, would use an evaluator to select best
        // For now, select the longest response as a simple heuristic
        var bestResponse = results.AgentResponses
            .OrderByDescending(r => (r.Value.Content?.ToString()?.Length ?? 0))
            .FirstOrDefault();

        return new CoordinatedResult
        {
            Success = true,
            FinalResponse = bestResponse.Value ?? results.FinalResponse,
            AgentResponses = results.AgentResponses,
            Metadata = new Dictionary<string, object> 
            { 
                ["winner"] = bestResponse.Key,
                ["strategy"] = "length_heuristic"
            }
        };
    }

        /// <summary>
        /// Creates a builder for fluent configuration of AgentCoordinator.
        /// 创建用于流畅配置 AgentCoordinator 的构建器。
        /// </summary>
        /// <returns>A new AgentCoordinatorBuilder / 新的 AgentCoordinatorBuilder</returns>
        public static AgentCoordinatorBuilder Builder()
    {
        return new AgentCoordinatorBuilder();
    }

    /// <summary>
    /// Releases all resources used by the coordinator.
    /// 释放协调器使用的所有资源。
    /// </summary>
    public void Dispose()
    {
        if (!_disposed)
        {
            lock (_agents)
            {
                _agents.Clear();
            }
            _disposed = true;
        }
    }
}

/// <summary>
/// Builder for AgentCoordinator
/// AgentCoordinator 的构建器
/// </summary>
public class AgentCoordinatorBuilder
{
    private AgentCoordinator _coordinator;

    public AgentCoordinatorBuilder()
    {
        _coordinator = new AgentCoordinator();
    }

    public AgentCoordinatorBuilder Strategy(CoordinationStrategy strategy)
    {
        _coordinator = new AgentCoordinator(strategy);
        return this;
    }

    public AgentCoordinatorBuilder CoordinatorAgent(string name)
    {
        _coordinator = new AgentCoordinator(
            CoordinationStrategy.Hierarchical, 
            name);
        return this;
    }

    public AgentCoordinatorBuilder RegisterAgent(string name, IAgent agent)
    {
        _coordinator.RegisterAgent(name, agent);
        return this;
    }

    public AgentCoordinator Build()
    {
        return _coordinator;
    }
}
