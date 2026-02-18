// Copyright 2024-2026 the original author or authors.
// Licensed under the Apache License, Version 2.0

using AgentScope.Core.Agent;
using AgentScope.Core.Message;
using AgentScope.Core.MultiAgent;
using Xunit;

namespace AgentScope.Core.Tests.MultiAgent;

public class AgentRouterTests
{
    [Fact]
    public void Constructor_CreatesEmptyRouter()
    {
        // Arrange & Act
        var router = new AgentRouter();

        // Assert
        Assert.Equal(0, router.AgentCount);
        Assert.Equal(0, router.RuleCount);
    }

    [Fact]
    public void RegisterAgent_WithValidAgent_RegistersSuccessfully()
    {
        // Arrange
        var router = new AgentRouter();
        var agent = new TestAgent();

        // Act
        router.RegisterAgent("test_agent", agent);

        // Assert
        Assert.Equal(1, router.AgentCount);
        Assert.Contains("test_agent", router.GetRegisteredAgentNames());
    }

    [Fact]
    public void RegisterAgent_WithEmptyName_ThrowsArgumentException()
    {
        // Arrange
        var router = new AgentRouter();
        var agent = new TestAgent();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => router.RegisterAgent("", agent));
    }

    [Fact]
    public void RegisterAgent_WithNullAgent_ThrowsArgumentNullException()
    {
        // Arrange
        var router = new AgentRouter();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => router.RegisterAgent("test", null!));
    }

    [Fact]
    public void UnregisterAgent_ExistingAgent_RemovesSuccessfully()
    {
        // Arrange
        var router = new AgentRouter();
        router.RegisterAgent("test_agent", new TestAgent());

        // Act
        var result = router.UnregisterAgent("test_agent");

        // Assert
        Assert.True(result);
        Assert.Equal(0, router.AgentCount);
    }

    [Fact]
    public void UnregisterAgent_NonExistingAgent_ReturnsFalse()
    {
        // Arrange
        var router = new AgentRouter();

        // Act
        var result = router.UnregisterAgent("nonexistent");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void AddRule_WithValidRule_AddsSuccessfully()
    {
        // Arrange
        var router = new AgentRouter();
        var rule = new RoutingRule
        {
            Name = "test_rule",
            TargetAgent = "test_agent",
            Keywords = new List<string> { "help" }
        };

        // Act
        router.AddRule(rule);

        // Assert
        Assert.Equal(1, router.RuleCount);
    }

    [Fact]
    public void AddRule_NullRule_ThrowsArgumentNullException()
    {
        // Arrange
        var router = new AgentRouter();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => router.AddRule(null!));
    }

    [Fact]
    public void RemoveRule_ExistingRule_RemovesSuccessfully()
    {
        // Arrange
        var router = new AgentRouter();
        router.AddRule(new RoutingRule
        {
            Name = "test_rule",
            TargetAgent = "test_agent",
            Keywords = new List<string> { "help" }
        });

        // Act
        var result = router.RemoveRule("test_rule");

        // Assert
        Assert.True(result);
        Assert.Equal(0, router.RuleCount);
    }

    [Fact]
    public void RemoveRule_NonExistingRule_ReturnsFalse()
    {
        // Arrange
        var router = new AgentRouter();

        // Act
        var result = router.RemoveRule("nonexistent");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task RouteAsync_WithMatchingKeyword_RoutesToCorrectAgent()
    {
        // Arrange
        var router = new AgentRouter();
        router.RegisterAgent("support_agent", new TestAgent("SupportAgent"));
        router.AddRule(new RoutingRule
        {
            Name = "support_rule",
            TargetAgent = "support_agent",
            Keywords = new List<string> { "help", "support" }
        });

        var message = Msg.Builder().Role("user").Content("I need help with something").Build();

        // Act
        var response = await router.RouteAsync(message);

        // Assert
        Assert.Contains("SupportAgent", response.Content?.ToString());
    }

    [Fact]
    public async Task RouteAsync_WithCondition_RoutesBasedOnCondition()
    {
        // Arrange
        var router = new AgentRouter();
        router.RegisterAgent("admin_agent", new TestAgent("AdminAgent"));
        router.AddRule(new RoutingRule
        {
            Name = "admin_rule",
            TargetAgent = "admin_agent",
            Condition = msg => (msg.Content?.ToString()?.ToLower().Contains("admin") ?? false),
            Priority = 10
        });

        var message = Msg.Builder().Role("user").Content("Admin access required").Build();

        // Act
        var response = await router.RouteAsync(message);

        // Assert
        Assert.Contains("AdminAgent", response.Content?.ToString());
    }

    [Fact]
    public async Task RouteAsync_WithPattern_RoutesBasedOnRegex()
    {
        // Arrange
        var router = new AgentRouter();
        router.RegisterAgent("email_agent", new TestAgent("EmailAgent"));
        router.AddRule(new RoutingRule
        {
            Name = "email_rule",
            TargetAgent = "email_agent",
            Pattern = @"\b[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Z|a-z]{2,}\b"
        });

        var message = Msg.Builder().Role("user").Content("Contact me at test@example.com").Build();

        // Act
        var response = await router.RouteAsync(message);

        // Assert
        Assert.Contains("EmailAgent", response.Content?.ToString());
    }

    [Fact]
    public async Task RouteAsync_NoMatchWithDefault_UsesDefaultAgent()
    {
        // Arrange
        var router = new AgentRouter();
        router.SetDefaultAgent(new TestAgent("DefaultAgent"));

        var message = Msg.Builder().Role("user").Content("Random message").Build();

        // Act
        var (agentName, response) = await router.RouteWithInfoAsync(message);

        // Assert
        Assert.Equal("default", agentName);
        Assert.Contains("DefaultAgent", response.Content?.ToString());
    }

    [Fact]
    public async Task RouteAsync_NoMatchNoDefault_ReturnsError()
    {
        // Arrange
        var router = new AgentRouter();
        var message = Msg.Builder().Role("user").Content("Random message").Build();

        // Act
        var response = await router.RouteAsync(message);

        // Assert
        Assert.Contains("No suitable agent found", response.Content?.ToString());
    }

    [Fact]
    public void Builder_CreatesRouterWithConfiguration()
    {
        // Arrange & Act
        var agent = new TestAgent("MyAgent");
        var router = AgentRouter.Builder()
            .Name("TestRouter")
            .RegisterAgent("agent1", agent)
            .AddRule("rule1", "agent1", "keyword1", "keyword2")
            .Build();

        // Assert
        Assert.Equal("TestRouter", router.Name);
        Assert.Equal(1, router.AgentCount);
        Assert.Equal(1, router.RuleCount);
    }

    [Fact]
    public void GetRules_ReturnsAllRules()
    {
        // Arrange
        var router = new AgentRouter();
        router.AddRule(new RoutingRule { Name = "rule1", TargetAgent = "agent1" });
        router.AddRule(new RoutingRule { Name = "rule2", TargetAgent = "agent2" });

        // Act
        var rules = router.GetRules();

        // Assert
        Assert.Equal(2, rules.Count);
    }

    [Fact]
    public void SetDefaultAgent_WithNull_ThrowsArgumentNullException()
    {
        // Arrange
        var router = new AgentRouter();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => router.SetDefaultAgent(null!));
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
                .Content($"Response from {_name}")
                .Build());
        }

        public Task<Msg> CallAsync(Msg message)
        {
            return Task.FromResult(Msg.Builder()
                .Role("assistant")
                .Name(_name)
                .Content($"Response from {_name}")
                .Build());
        }
    }
}
