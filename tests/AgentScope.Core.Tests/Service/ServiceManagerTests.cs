// Copyright 2024-2026 the original author or authors.
// Licensed under the Apache License, Version 2.0

using AgentScope.Core.Message;
using AgentScope.Core.Service;
using Xunit;

namespace AgentScope.Core.Tests.Service;

public class ServiceManagerTests
{
    [Fact]
    public void Constructor_CreatesEmptyManager()
    {
        // Arrange & Act
        using var manager = new ServiceManager();

        // Assert
        Assert.Equal(0, manager.Count);
    }

    [Fact]
    public async Task RegisterAsync_WithValidService_RegistersSuccessfully()
    {
        // Arrange
        using var manager = new ServiceManager();
        var service = CreateTestService("test-service");

        // Act
        var result = await manager.RegisterAsync(service);

        // Assert
        Assert.True(result);
        Assert.Equal(1, manager.Count);
    }

    [Fact]
    public async Task RegisterAsync_DuplicateService_ReturnsFalse()
    {
        // Arrange
        using var manager = new ServiceManager();
        var service = CreateTestService("test-service");
        await manager.RegisterAsync(service);

        // Act
        var result = await manager.RegisterAsync(service);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task UnregisterAsync_ExistingService_UnregistersSuccessfully()
    {
        // Arrange
        using var manager = new ServiceManager();
        var service = CreateTestService("test-service");
        await manager.RegisterAsync(service);

        // Act
        var result = await manager.UnregisterAsync(service.Info.Id);

        // Assert
        Assert.True(result);
        Assert.Equal(0, manager.Count);
    }

    [Fact]
    public async Task UnregisterAsync_NonExistingService_ReturnsFalse()
    {
        // Arrange
        using var manager = new ServiceManager();

        // Act
        var result = await manager.UnregisterAsync("nonexistent");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task GetService_ExistingService_ReturnsService()
    {
        // Arrange
        using var manager = new ServiceManager();
        var service = CreateTestService("test-service");
        await manager.RegisterAsync(service);

        // Act
        var result = manager.GetService(service.Info.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(service.Info.Id, result.Info.Id);
    }

    [Fact]
    public void GetService_NonExistingService_ReturnsNull()
    {
        // Arrange
        using var manager = new ServiceManager();

        // Act
        var result = manager.GetService("nonexistent");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetServiceByName_ExistingService_ReturnsService()
    {
        // Arrange
        using var manager = new ServiceManager();
        var service = CreateTestService("test-service");
        await manager.RegisterAsync(service);

        // Act
        var result = manager.GetServiceByName("test-service");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("test-service", result.Info.Name);
    }

    [Fact]
    public async Task GetServicesByTag_WithMatchingTag_ReturnsServices()
    {
        // Arrange
        using var manager = new ServiceManager();
        var service1 = CreateTestService("service1", tags: new[] { "tag1", "tag2" });
        var service2 = CreateTestService("service2", tags: new[] { "tag1" });
        await manager.RegisterAsync(service1);
        await manager.RegisterAsync(service2);

        // Act
        var result = manager.GetServicesByTag("tag1");

        // Assert
        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task StartServiceAsync_ExistingService_StartsService()
    {
        // Arrange
        using var manager = new ServiceManager();
        var service = CreateTestService("test-service");
        await manager.RegisterAsync(service);

        // Act
        var result = await manager.StartServiceAsync(service.Info.Id);

        // Assert
        Assert.True(result);
        Assert.Equal(ServiceStatus.Running, service.Status);
    }

    [Fact]
    public async Task StopServiceAsync_RunningService_StopsService()
    {
        // Arrange
        using var manager = new ServiceManager();
        var service = CreateTestService("test-service");
        await manager.RegisterAsync(service);
        await manager.StartServiceAsync(service.Info.Id);

        // Act
        var result = await manager.StopServiceAsync(service.Info.Id);

        // Assert
        Assert.True(result);
        Assert.Equal(ServiceStatus.Stopped, service.Status);
    }

    [Fact]
    public async Task StartAllAsync_StartsAllServices()
    {
        // Arrange
        using var manager = new ServiceManager();
        var service1 = CreateTestService("service1");
        var service2 = CreateTestService("service2");
        await manager.RegisterAsync(service1);
        await manager.RegisterAsync(service2);

        // Act
        await manager.StartAllAsync();

        // Assert
        Assert.Equal(ServiceStatus.Running, service1.Status);
        Assert.Equal(ServiceStatus.Running, service2.Status);
    }

    [Fact]
    public async Task StopAllAsync_StopsAllServices()
    {
        // Arrange
        using var manager = new ServiceManager();
        var service1 = CreateTestService("service1");
        var service2 = CreateTestService("service2");
        await manager.RegisterAsync(service1);
        await manager.RegisterAsync(service2);
        await manager.StartAllAsync();

        // Act
        await manager.StopAllAsync();

        // Assert
        Assert.Equal(ServiceStatus.Stopped, service1.Status);
        Assert.Equal(ServiceStatus.Stopped, service2.Status);
    }

    [Fact]
    public async Task CheckHealthAsync_ReturnsHealthStatus()
    {
        // Arrange
        using var manager = new ServiceManager();
        var service = CreateTestService("test-service");
        await manager.RegisterAsync(service);
        await manager.StartServiceAsync(service.Info.Id);

        // Act
        var result = await manager.CheckHealthAsync();

        // Assert
        Assert.Single(result);
        Assert.True(result[service.Info.Id]);
    }

    [Fact]
    public async Task GetStatistics_ReturnsCorrectStats()
    {
        // Arrange
        using var manager = new ServiceManager();
        var service1 = CreateTestService("service1");
        var service2 = CreateTestService("service2");
        await manager.RegisterAsync(service1);
        await manager.RegisterAsync(service2);
        await manager.StartServiceAsync(service1.Info.Id);

        // Act
        var stats = manager.GetStatistics();

        // Assert
        Assert.Equal(2, stats.TotalCount);
        Assert.Equal(1, stats.RunningCount);
        Assert.Equal(1, stats.StoppedCount);
    }

    private static TestService CreateTestService(string name, string[]? tags = null)
    {
        var info = new ServiceInfo
        {
            Id = Guid.NewGuid().ToString(),
            Name = name,
            Tags = tags?.ToList() ?? new List<string>()
        };

        return new TestService(info);
    }

    private class TestService : ServiceBase
    {
        public TestService(ServiceInfo info) : base(info) { }

        protected override Task OnStartAsync(CancellationToken ct)
        {
            return Task.CompletedTask;
        }

        protected override Task OnStopAsync(CancellationToken ct)
        {
            return Task.CompletedTask;
        }

        protected override Task<Msg> ProcessMessageAsync(Msg message, CancellationToken ct)
        {
            return Task.FromResult(Msg.Builder()
                .Role("assistant")
                .Content($"Processed: {message.Content}")
                .Build());
        }
    }
}
