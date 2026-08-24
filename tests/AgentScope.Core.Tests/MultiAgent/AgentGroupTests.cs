// Copyright 2024-2026 the original author or authors.
// Licensed under the Apache License, Version 2.0

using AgentScope.Core.Agent;
using AgentScope.Core.Events;
using AgentScope.Core.Message;
using AgentScope.Core.MultiAgent;
using Xunit;

namespace AgentScope.Core.Tests.MultiAgent;

/// <summary>
/// Tests for <see cref="AgentGroup"/> with various distribution strategies.
/// 对 AgentGroup 在不同分发策略下的测试。
/// </summary>
public class AgentGroupTests
{
    [Fact]
    /// <summary>
    /// Tests that the constructor with a name sets the group name and initializes count to zero.
    /// 测试带名称的构造函数设置了组名称并将计数初始化为零。
    /// </summary>
    public void Constructor_WithName_SetsName()
    {
        // Arrange & Act
        var group = new AgentGroup("TestGroup");

        // Assert
        Assert.Equal("TestGroup", group.Name);
        Assert.Equal(0, group.Count);
    }

    [Fact]
    /// <summary>
    /// Tests that a valid agent is added to the group successfully.
    /// 测试有效的代理被成功添加到组中。
    /// </summary>
    public void AddAgent_WithValidAgent_AddsSuccessfully()
    {
        // Arrange
        var group = new AgentGroup();
        var agent = new TestAgent();

        // Act
        var result = group.AddAgent(agent);

        // Assert
        Assert.True(result);
        Assert.Equal(1, group.Count);
    }

    [Fact]
    /// <summary>
    /// Tests that adding a null agent throws <see cref="ArgumentNullException"/>.
    /// 测试添加 null 代理时抛出 ArgumentNullException。
    /// </summary>
    public void AddAgent_WithNullAgent_ThrowsArgumentNullException()
    {
        // Arrange
        var group = new AgentGroup();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => group.AddAgent(null!));
    }

    [Fact]
    /// <summary>
    /// Tests that adding a duplicate agent returns false.
    /// 测试添加重复代理时返回 false。
    /// </summary>
    public void AddAgent_DuplicateAgent_ReturnsFalse()
    {
        // Arrange
        var group = new AgentGroup();
        var agent = new TestAgent();
        group.AddAgent(agent);

        // Act
        var result = group.AddAgent(agent);

        // Assert
        Assert.False(result);
    }

    [Fact]
    /// <summary>
    /// Tests that an existing agent is removed successfully.
    /// 测试已存在的代理被成功移除。
    /// </summary>
    public void RemoveAgent_ExistingAgent_RemovesSuccessfully()
    {
        // Arrange
        var group = new AgentGroup();
        var agent = new TestAgent();
        group.AddAgent(agent);

        // Act
        var result = group.RemoveAgent(agent.GetType().Name + "_" + agent.GetHashCode());

        // Assert
        Assert.True(result);
        Assert.Equal(0, group.Count);
    }

    [Fact]
    /// <summary>
    /// Tests that removing a non-existent agent returns false.
    /// 测试移除不存在的代理时返回 false。
    /// </summary>
    public void RemoveAgent_NonExistingAgent_ReturnsFalse()
    {
        // Arrange
        var group = new AgentGroup();

        // Act
        var result = group.RemoveAgent("nonexistent");

        // Assert
        Assert.False(result);
    }

    [Fact]
    /// <summary>
    /// Tests that broadcasting a message to multiple agents returns all responses.
    /// 测试向多个代理广播消息时返回所有响应。
    /// </summary>
    public async Task BroadcastAsync_MultipleAgents_ReturnsAllResponses()
    {
        // Arrange
        var group = new AgentGroup();
        group.AddAgent(new TestAgent("Agent1"));
        group.AddAgent(new TestAgent("Agent2"));

        var message = Msg.Builder().Role("user").Content("Hello").Build();

        // Act
        var results = await group.BroadcastAsync(message);

        // Assert
        Assert.Equal(2, results.Count);
        Assert.All(results.Values, r => Assert.Equal("assistant", r.Role));
        Assert.All(results.Values, r => Assert.NotNull(r.Content));
    }

    [Fact]
    /// <summary>
    /// Tests that round-robin distribution cycles through agents sequentially.
    /// 测试轮询分发策略按顺序循环选择代理。
    /// </summary>
    public async Task CallAsync_RoundRobin_CyclesThroughAgents()
    {
        // Arrange
        var group = new AgentGroup(strategy: DistributionStrategy.RoundRobin);
        group.AddAgent(new TestAgent("Agent1"));
        group.AddAgent(new TestAgent("Agent2"));

        var message = Msg.Builder().Role("user").Content("Hello").Build();

        // Act
        var response1 = await group.CallAsync(message);
        var response2 = await group.CallAsync(message);

        // Assert
        Assert.NotNull(response1);
        Assert.NotNull(response2);
    }

    [Fact]
    /// <summary>
    /// Tests that random distribution selects an agent and returns a response.
    /// 测试随机分发策略选择一个代理并返回响应。
    /// </summary>
    public async Task CallAsync_Random_ReturnsResponse()
    {
        // Arrange
        var group = new AgentGroup(strategy: DistributionStrategy.Random);
        group.AddAgent(new TestAgent("Agent1"));
        group.AddAgent(new TestAgent("Agent2"));

        var message = Msg.Builder().Role("user").Content("Hello").Build();

        // Act
        var response = await group.CallAsync(message);

        // Assert
        Assert.NotNull(response);
        Assert.Equal("assistant", response.Role);
    }

    [Fact]
    /// <summary>
    /// Tests that load-based distribution selects the least busy agent.
    /// 测试基于负载的分发策略选择最不繁忙的代理。
    /// </summary>
    public async Task CallAsync_LoadBased_SelectsLeastBusy()
    {
        // Arrange
        var group = new AgentGroup(strategy: DistributionStrategy.LoadBased);
        group.AddAgent(new TestAgent("Agent1"));
        group.AddAgent(new TestAgent("Agent2"));

        var message = Msg.Builder().Role("user").Content("Hello").Build();

        // Act
        var response = await group.CallAsync(message);

        // Assert
        Assert.NotNull(response);
    }

    [Fact]
    /// <summary>
    /// Tests that calling an empty group returns an error message.
    /// 测试调用空组时返回错误消息。
    /// </summary>
    public async Task CallAsync_EmptyGroup_ReturnsErrorMessage()
    {
        // Arrange
        var group = new AgentGroup();
        var message = Msg.Builder().Role("user").Content("Hello").Build();

        // Act
        var response = await group.CallAsync(message);

        // Assert
        Assert.Contains("No agents available", response.Content?.ToString());
    }

    [Fact]
    /// <summary>
    /// Tests that load statistics are returned correctly for registered agents.
    /// 测试已注册代理的负载统计信息被正确返回。
    /// </summary>
    public void GetLoadStatistics_WithAgents_ReturnsStats()
    {
        // Arrange
        var group = new AgentGroup();
        group.AddAgent(new TestAgent("Agent1"));

        // Act
        var stats = group.GetLoadStatistics();

        // Assert
        Assert.Single(stats);
        Assert.Equal(0, stats.First().Value.CurrentLoad);
    }

    [Fact]
    /// <summary>
    /// Tests that <see cref="AgentGroup.AgentNames"/> returns all registered agent names.
    /// 测试 AgentNames 属性返回所有已注册的代理名称。
    /// </summary>
    public void AgentNames_WithMultipleAgents_ReturnsAllNames()
    {
        // Arrange
        var group = new AgentGroup();
        group.AddAgent(new TestAgent("Agent1"));
        group.AddAgent(new TestAgent("Agent2"));

        // Act
        var names = group.AgentNames;

        // Assert
        Assert.Equal(2, names.Count);
    }

    [Fact]
    /// <summary>
    /// Tests that disposing the group clears all agents.
    /// 测试释放组时清除所有代理。
    /// </summary>
    public void Dispose_ClearsAllAgents()
    {
        // Arrange
        var group = new AgentGroup();
        group.AddAgent(new TestAgent());
        Assert.Equal(1, group.Count);

        // Act
        group.Dispose();

        // Assert
        Assert.Equal(0, group.Count);
    }

    /// <summary>
    /// A stub <see cref="IAgent"/> implementation for testing purposes.
    /// 用于测试的存根 IAgent 实现。
    /// </summary>
    private class TestAgent : IAgent
    {
        private readonly string _name;

        /// <summary>
        /// Initializes the test agent with an optional name.
        /// 使用可选名称初始化测试代理。
        /// </summary>
        public TestAgent(string? name = null)
        {
            _name = name ?? $"TestAgent_{GetHashCode()}";
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
                .Content($"Response from {_name}: {message.Content}")
                .Build());
        }

        /// <summary>
        /// Asynchronously returns a response (单条消息).
        /// </summary>
        public Task<Msg> CallAsync(Msg message, RuntimeContext? context = null)
        {
            return Task.FromResult(Msg.Builder()
                .Role("assistant")
                .Name(_name)
                .Content($"Response from {_name}: {message.Content}")
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
