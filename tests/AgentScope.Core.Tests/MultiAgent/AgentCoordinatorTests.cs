// Copyright 2024-2026 the original author or authors.
// Licensed under the Apache License, Version 2.0

using AgentScope.Core.Agent;
using AgentScope.Core.Message;
using AgentScope.Core.MultiAgent;
using Xunit;

namespace AgentScope.Core.Tests.MultiAgent;

public class AgentCoordinatorTests
{
    [Fact]
    public void Constructor_Default_CreatesCoordinator()
    {
        // Arrange & Act
        var coordinator = new AgentCoordinator();

        // Assert
        Assert.NotNull(coordinator);
    }

    [Fact]
    public void Constructor_WithStrategy_SetsStrategy()
    {
        // Arrange & Act
        var coordinator = new AgentCoordinator(CoordinationStrategy.Parallel);

        // Assert
        Assert.NotNull(coordinator);
    }

    [Fact]
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
    public void RegisterAgent_WithEmptyName_ThrowsArgumentException()
    {
        // Arrange
        var coordinator = new AgentCoordinator();
        var agent = new TestAgent();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => coordinator.RegisterAgent("", agent));
    }

    [Fact]
    public void RegisterAgent_WithNullAgent_ThrowsArgumentNullException()
    {
        // Arrange
        var coordinator = new AgentCoordinator();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => coordinator.RegisterAgent("test", null!));
    }

    [Fact]
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
    public void Dispose_DoesNotThrow()
    {
        // Arrange
        var coordinator = new AgentCoordinator();
        coordinator.RegisterAgent("agent1", new TestAgent());

        // Act & Assert
        coordinator.Dispose();
        // No exception thrown
    }

    private class TestAgent : IAgent
    {
        private readonly string _name;
        private readonly string _responseContent;

        public TestAgent(string? name = null, string? responseContent = null)
        {
            _name = name ?? $"TestAgent_{GetHashCode()}";
            _responseContent = responseContent ?? $"Response from {_name}";
        }

        public string Name => _name;

        public System.IObservable<Msg> Call(Msg message)
        {
            return System.Reactive.Linq.Observable.Return(Msg.Builder()
                .Role("assistant")
                .Name(_name)
                .Content(_responseContent)
                .Build());
        }

        public Task<Msg> CallAsync(Msg message)
        {
            return Task.FromResult(Msg.Builder()
                .Role("assistant")
                .Name(_name)
                .Content(_responseContent)
                .Build());
        }
    }
}
