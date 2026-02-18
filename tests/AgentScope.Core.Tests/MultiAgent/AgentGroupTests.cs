// Copyright 2024-2026 the original author or authors.
// Licensed under the Apache License, Version 2.0

using AgentScope.Core.Agent;
using AgentScope.Core.Message;
using AgentScope.Core.MultiAgent;
using Xunit;

namespace AgentScope.Core.Tests.MultiAgent;

public class AgentGroupTests
{
    [Fact]
    public void Constructor_WithName_SetsName()
    {
        // Arrange & Act
        var group = new AgentGroup("TestGroup");

        // Assert
        Assert.Equal("TestGroup", group.Name);
        Assert.Equal(0, group.Count);
    }

    [Fact]
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
    public void AddAgent_WithNullAgent_ThrowsArgumentNullException()
    {
        // Arrange
        var group = new AgentGroup();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => group.AddAgent(null!));
    }

    [Fact]
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

    private class TestAgent : IAgent
    {
        private readonly string _name;

        public TestAgent(string? name = null)
        {
            _name = name ?? $"TestAgent_{GetHashCode()}";
        }

        public string Name => _name;

        public System.IObservable<Msg> Call(Msg message)
        {
            return System.Reactive.Linq.Observable.Return(Msg.Builder()
                .Role("assistant")
                .Name(_name)
                .Content($"Response from {_name}: {message.Content}")
                .Build());
        }

        public Task<Msg> CallAsync(Msg message)
        {
            return Task.FromResult(Msg.Builder()
                .Role("assistant")
                .Name(_name)
                .Content($"Response from {_name}: {message.Content}")
                .Build());
        }
    }
}
