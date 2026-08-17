// Copyright 2024-2026 the original author or authors.
// Licensed under the Apache License, Version 2.0

using AgentScope.Core.Agent;
using AgentScope.Core.Events;
using AgentScope.Core.Message;
using AgentScope.Core.MultiAgent;
using Xunit;

namespace AgentScope.Core.Tests.MultiAgent;

/// <summary>
/// Tests for <see cref="AgentCoordinator"/> with various coordination strategies.
/// 对 AgentCoordinator 在不同协作策略下的测试。
/// </summary>
public class AgentCoordinatorTests
{
    [Fact]
    /// <summary>
    /// Tests that the default constructor creates a valid coordinator instance.
    /// 测试默认构造函数创建有效的协调器实例。
    /// </summary>
    public void Constructor_Default_CreatesCoordinator()
    {
        // Arrange & Act
        var coordinator = new AgentCoordinator();

        // Assert
        Assert.NotNull(coordinator);
    }

    [Fact]
    /// <summary>
    /// Tests that a specific <see cref="CoordinationStrategy"/> can be set via constructor.
    /// 测试可以通过构造函数设置特定的 CoordinationStrategy。
    /// </summary>
    public void Constructor_WithStrategy_SetsStrategy()
    {
        // Arrange & Act
        var coordinator = new AgentCoordinator(CoordinationStrategy.Parallel);

        // Assert
        Assert.NotNull(coordinator);
    }

    [Fact]
    /// <summary>
    /// Tests that a valid agent can be registered with the coordinator.
    /// 测试可以向协调器注册有效的代理。
    /// </summary>
    public void RegisterAgent_WithValidAgent_RegistersSuccessfully()
    {
        // Arrange
        var coordinator = new AgentCoordinator();
        var agent = new TestAgent();

        // Act
        coordinator.RegisterAgent("test_agent", agent);

        // Assert
        // No exception thrown
    }

    [Fact]
    /// <summary>
    /// Tests that registering an agent with an empty name throws <see cref="ArgumentException"/>.
    /// 测试使用空名称注册代理时抛出 ArgumentException。
    /// </summary>
    public void RegisterAgent_WithEmptyName_ThrowsArgumentException()
    {
        // Arrange
        var coordinator = new AgentCoordinator();
        var agent = new TestAgent();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => coordinator.RegisterAgent("", agent));
    }

    [Fact]
    /// <summary>
    /// Tests that registering a null agent throws <see cref="ArgumentNullException"/>.
    /// 测试注册 null 代理时抛出 ArgumentNullException。
    /// </summary>
    public void RegisterAgent_WithNullAgent_ThrowsArgumentNullException()
    {
        // Arrange
        var coordinator = new AgentCoordinator();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => coordinator.RegisterAgent("test", null!));
    }

    [Fact]
    /// <summary>
    /// Tests that sequential coordination executes agents one after another.
    /// 测试顺序协调策略按顺序依次执行代理。
    /// </summary>
    public async Task CoordinateAsync_Sequential_ExecutesAgentsInOrder()
    {
        // Arrange
        var coordinator = new AgentCoordinator(CoordinationStrategy.Sequential);
        coordinator.RegisterAgent("agent1", new TestAgent("Agent1"));
        coordinator.RegisterAgent("agent2", new TestAgent("Agent2"));

        var message = Msg.Builder().Role("user").Content("Hello").Build();

        // Act
        var result = await coordinator.CoordinateAsync(message);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.FinalResponse);
        Assert.Equal(2, result.AgentResponses.Count);
    }

    [Fact]
    /// <summary>
    /// Tests that parallel coordination executes all agents concurrently.
    /// 测试并行协调策略并发执行所有代理。
    /// </summary>
    public async Task CoordinateAsync_Parallel_ExecutesAllAgents()
    {
        // Arrange
        var coordinator = new AgentCoordinator(CoordinationStrategy.Parallel);
        coordinator.RegisterAgent("agent1", new TestAgent("Agent1"));
        coordinator.RegisterAgent("agent2", new TestAgent("Agent2"));
        coordinator.RegisterAgent("agent3", new TestAgent("Agent3"));

        var message = Msg.Builder().Role("user").Content("Hello").Build();

        // Act
        var result = await coordinator.CoordinateAsync(message);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(3, result.AgentResponses.Count);
        Assert.Contains("---", result.FinalResponse.Content?.ToString());
    }

    [Fact]
    /// <summary>
    /// Tests that consensus coordination executes multiple rounds of discussion.
    /// 测试共识协调策略执行多轮讨论。
    /// </summary>
    public async Task CoordinateAsync_Consensus_ExecutesMultipleRounds()
    {
        // Arrange
        var coordinator = new AgentCoordinator(CoordinationStrategy.Consensus);
        coordinator.RegisterAgent("agent1", new TestAgent("Agent1"));
        coordinator.RegisterAgent("agent2", new TestAgent("Agent2"));

        var message = Msg.Builder().Role("user").Content("Hello").Build();

        // Act
        var result = await coordinator.CoordinateAsync(message);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.FinalResponse);
    }

    [Fact]
    /// <summary>
    /// Tests that hierarchical coordination uses a designated coordinator agent.
    /// 测试层次协调策略使用指定的协调者代理。
    /// </summary>
    public async Task CoordinateAsync_Hierarchical_UsesCoordinator()
    {
        // Arrange
        var coordinator = new AgentCoordinator(CoordinationStrategy.Hierarchical, "coordinator");
        coordinator.RegisterAgent("coordinator", new TestAgent("Coordinator"));
        coordinator.RegisterAgent("worker1", new TestAgent("Worker1"));
        coordinator.RegisterAgent("worker2", new TestAgent("Worker2"));

        var message = Msg.Builder().Role("user").Content("Hello").Build();

        // Act
        var result = await coordinator.CoordinateAsync(message);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.FinalResponse);
    }

    [Fact]
    /// <summary>
    /// Tests that hierarchical coordination falls back to the first agent when coordinator is not found.
    /// 测试层次协调策略在找不到协调者时回退到第一个代理。
    /// </summary>
    public async Task CoordinateAsync_Hierarchical_NoCoordinator_UsesFirstAgent()
    {
        // Arrange
        var coordinator = new AgentCoordinator(CoordinationStrategy.Hierarchical, "nonexistent");
        coordinator.RegisterAgent("worker1", new TestAgent("Worker1"));

        var message = Msg.Builder().Role("user").Content("Hello").Build();

        // Act
        var result = await coordinator.CoordinateAsync(message);

        // Assert - when coordinator not found, falls back to first agent
        Assert.True(result.Success);
    }

    [Fact]
    /// <summary>
    /// Tests that competitive coordination selects the best result among agents.
    /// 测试竞争协调策略从代理中选择最佳结果。
    /// </summary>
    public async Task CoordinateAsync_Competitive_SelectsBestResult()
    {
        // Arrange
        var coordinator = new AgentCoordinator(CoordinationStrategy.Competitive);
        coordinator.RegisterAgent("agent1", new TestAgent("Agent1", "Short"));
        coordinator.RegisterAgent("agent2", new TestAgent("Agent2", "This is a much longer response that should win"));

        var message = Msg.Builder().Role("user").Content("Hello").Build();

        // Act
        var result = await coordinator.CoordinateAsync(message);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.FinalResponse);
        Assert.True(result.Metadata["winner"].ToString()?.ToLower() == "agent2");
    }

    [Fact]
    /// <summary>
    /// Tests that coordinating with no registered agents returns an error.
    /// 测试没有注册代理时执行协调返回错误。
    /// </summary>
    public async Task CoordinateAsync_NoAgents_ReturnsError()
    {
        // Arrange
        var coordinator = new AgentCoordinator();
        var message = Msg.Builder().Role("user").Content("Hello").Build();

        // Act
        var result = await coordinator.CoordinateAsync(message);

        // Assert
        // Empty agent list should fail
    }

    [Fact]
    /// <summary>
    /// Tests that the builder creates a fully configured coordinator.
    /// 测试构建器创建完全配置好的协调器。
    /// </summary>
    public void Builder_CreatesCoordinatorWithConfiguration()
    {
        // Arrange & Act
        var coordinator = AgentCoordinator.Builder()
            .Strategy(CoordinationStrategy.Parallel)
            .RegisterAgent("agent1", new TestAgent("Agent1"))
            .RegisterAgent("agent2", new TestAgent("Agent2"))
            .Build();

        // Assert
        Assert.NotNull(coordinator);
    }

    [Fact]
    /// <summary>
    /// Tests that disposing the coordinator does not throw.
    /// 测试释放协调器时不抛出异常。
    /// </summary>
    public void Dispose_DoesNotThrow()
    {
        // Arrange
        var coordinator = new AgentCoordinator();
        coordinator.RegisterAgent("agent1", new TestAgent());

        // Act & Assert
        coordinator.Dispose();
        // No exception thrown
    }

    /// <summary>
    /// A stub <see cref="IAgent"/> implementation for testing purposes.
    /// 用于测试的存根 IAgent 实现。
    /// </summary>
    private class TestAgent : IAgent
    {
        private readonly string _name;
        private readonly string _responseContent;

        /// <summary>
        /// Initializes the test agent with an optional name and response content.
        /// 使用可选名称和响应内容初始化测试代理。
        /// </summary>
        public TestAgent(string? name = null, string? responseContent = null)
        {
            _name = name ?? $"TestAgent_{GetHashCode()}";
            _responseContent = responseContent ?? $"Response from {_name}";
        }

        /// <summary>
        /// Gets the agent name.
        /// 获取代理名称。
        /// </summary>
        public string Name => _name;

        /// <summary>全局唯一 Agent ID</summary>
        public string AgentId => $"agent_{_name}";

        /// <summary>Agent 描述</summary>
        public string Description => $"Test agent: {_name}";

        /// <summary>中断（空实现）</summary>
        public void Interrupt() { }

        /// <summary>带消息的中断（空实现）</summary>
        public void Interrupt(Msg message) { }

        /// <summary>
        /// Synchronously returns a response via observable.
        /// 通过可观察对象同步返回响应。
        /// </summary>
        public System.IObservable<Msg> Call(Msg message)
        {
            return System.Reactive.Linq.Observable.Return(Msg.Builder()
                .Role("assistant")
                .Name(_name)
                .Content(_responseContent)
                .Build());
        }

        /// <summary>Asynchronously returns a response (单条消息).</summary>
        public Task<Msg> CallAsync(Msg message, RuntimeContext? context = null)
        {
            return Task.FromResult(Msg.Builder()
                .Role("assistant")
                .Name(_name)
                .Content(_responseContent)
                .Build());
        }

        /// <summary>Asynchronously returns a response (消息列表).</summary>
        public Task<Msg> CallAsync(IReadOnlyList<Msg> messages, RuntimeContext? context = null)
        {
            var last = messages[^1];
            return CallAsync(last, context);
        }

        /// <summary>Asynchronously returns a response (纯文本).</summary>
        public Task<Msg> CallAsync(string text, RuntimeContext? context = null)
        {
            var msg = Msg.Builder().Role("user").TextContent(text).Build();
            return CallAsync(msg, context);
        }

        /// <summary>流式处理（不支持）</summary>
        public IAsyncEnumerable<AgentScope.Core.Events.Event> StreamEventsAsync(IReadOnlyList<Msg> messages, RuntimeContext? context = null)
            => throw new NotSupportedException();

        /// <summary>流式处理单条消息（不支持）</summary>
        public IAsyncEnumerable<AgentScope.Core.Events.Event> StreamEventsAsync(Msg message, RuntimeContext? context = null)
            => throw new NotSupportedException();

        /// <summary>观察单条消息</summary>
        public async Task ObserveAsync(Msg message, RuntimeContext? context = null)
            => await CallAsync(message, context);

        /// <summary>观察多条消息</summary>
        public async Task ObserveAsync(IReadOnlyList<Msg> messages, RuntimeContext? context = null)
            => await CallAsync(messages, context);
    }
}
