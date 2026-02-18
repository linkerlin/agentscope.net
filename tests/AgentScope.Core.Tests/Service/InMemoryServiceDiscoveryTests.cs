// Copyright 2024-2026 the original author or authors.
// Licensed under the Apache License, Version 2.0

using AgentScope.Core.Service;
using Xunit;

namespace AgentScope.Core.Tests.Service;

public class InMemoryServiceDiscoveryTests
{
    [Fact]
    public void Constructor_CreatesEmptyDiscovery()
    {
        // Arrange & Act
        using var discovery = new InMemoryServiceDiscovery();

        // Assert
        // No exception thrown
    }

    [Fact]
    public async Task RegisterAsync_WithNewService_RegistersSuccessfully()
    {
        // Arrange
        using var discovery = new InMemoryServiceDiscovery();
        var service = CreateServiceInfo("test-service");

        // Act
        await discovery.RegisterAsync(service);

        // Assert
        var services = await discovery.ListServicesAsync();
        Assert.Single(services);
    }

    [Fact]
    public async Task RegisterAsync_UpdateExistingService_UpdatesSuccessfully()
    {
        // Arrange
        using var discovery = new InMemoryServiceDiscovery();
        var service = CreateServiceInfo("test-service");
        await discovery.RegisterAsync(service);

        // Act - register again with same ID
        var updatedService = CreateServiceInfo("test-service", id: service.Id);
        await discovery.RegisterAsync(updatedService);

        // Assert
        var services = await discovery.ListServicesAsync();
        Assert.Single(services);
    }

    [Fact]
    public async Task DeregisterAsync_ExistingService_RemovesSuccessfully()
    {
        // Arrange
        using var discovery = new InMemoryServiceDiscovery();
        var service = CreateServiceInfo("test-service");
        await discovery.RegisterAsync(service);

        // Act
        await discovery.DeregisterAsync(service.Id);

        // Assert
        var services = await discovery.ListServicesAsync();
        Assert.Empty(services);
    }

    [Fact]
    public async Task DiscoverAsync_WithMatchingName_ReturnsServices()
    {
        // Arrange
        using var discovery = new InMemoryServiceDiscovery();
        var service1 = CreateServiceInfo("my-service");
        var service2 = CreateServiceInfo("my-service");
        var service3 = CreateServiceInfo("other-service");
        await discovery.RegisterAsync(service1);
        await discovery.RegisterAsync(service2);
        await discovery.RegisterAsync(service3);

        // Act
        var result = await discovery.DiscoverAsync("my-service");

        // Assert
        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task DiscoverAsync_CaseInsensitive_ReturnsServices()
    {
        // Arrange
        using var discovery = new InMemoryServiceDiscovery();
        var service = CreateServiceInfo("MyService");
        await discovery.RegisterAsync(service);

        // Act
        var result = await discovery.DiscoverAsync("myservice");

        // Assert
        Assert.Single(result);
    }

    [Fact]
    public async Task GetServiceAsync_ExistingService_ReturnsService()
    {
        // Arrange
        using var discovery = new InMemoryServiceDiscovery();
        var service = CreateServiceInfo("test-service");
        await discovery.RegisterAsync(service);

        // Act
        var result = await discovery.GetServiceAsync(service.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(service.Id, result.Id);
    }

    [Fact]
    public async Task GetServiceAsync_NonExistingService_ReturnsNull()
    {
        // Arrange
        using var discovery = new InMemoryServiceDiscovery();

        // Act
        var result = await discovery.GetServiceAsync("nonexistent");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task ListServicesAsync_WithServices_ReturnsAllServices()
    {
        // Arrange
        using var discovery = new InMemoryServiceDiscovery();
        await discovery.RegisterAsync(CreateServiceInfo("service1"));
        await discovery.RegisterAsync(CreateServiceInfo("service2"));
        await discovery.RegisterAsync(CreateServiceInfo("service3"));

        // Act
        var result = await discovery.ListServicesAsync();

        // Assert
        Assert.Equal(3, result.Count);
    }

    [Fact]
    public async Task WatchAsync_ServiceRegistered_NotifiesWatcher()
    {
        // Arrange
        using var discovery = new InMemoryServiceDiscovery();
        ServiceChangeEvent? capturedEvent = null;
        using var cts = new CancellationTokenSource();

        var watchTask = Task.Run(async () =>
        {
            await discovery.WatchAsync(evt =>
            {
                capturedEvent = evt;
                cts.Cancel();
            }, cts.Token);
        });

        // Wait a bit for watcher to register
        await Task.Delay(100);

        // Act
        var service = CreateServiceInfo("test-service");
        await discovery.RegisterAsync(service);

        // Wait for notification
        await Task.WhenAny(watchTask, Task.Delay(1000));

        // Assert
        Assert.NotNull(capturedEvent);
        Assert.Equal(ServiceChangeType.Registered, capturedEvent.ChangeType);
        Assert.Equal(service.Id, capturedEvent.Service.Id);
    }

    [Fact]
    public async Task UpdateHeartbeatAsync_ExistingService_UpdatesTimestamp()
    {
        // Arrange
        using var discovery = new InMemoryServiceDiscovery();
        var service = CreateServiceInfo("test-service");
        await discovery.RegisterAsync(service);
        var oldHeartbeat = service.LastHeartbeat;

        await Task.Delay(10); // Small delay to ensure timestamp changes

        // Act
        await discovery.UpdateHeartbeatAsync(service.Id);

        // Assert
        var updated = await discovery.GetServiceAsync(service.Id);
        Assert.NotNull(updated);
        Assert.True(updated.LastHeartbeat > oldHeartbeat);
    }

    private static ServiceInfo CreateServiceInfo(string name, string? id = null)
    {
        return new ServiceInfo
        {
            Id = id ?? Guid.NewGuid().ToString(),
            Name = name,
            Host = "localhost",
            Port = 8080,
            Metadata = new Dictionary<string, string>(),
            Tags = new List<string>()
        };
    }
}
