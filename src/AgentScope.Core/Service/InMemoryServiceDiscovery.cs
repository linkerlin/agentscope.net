// Copyright 2024-2026 the original author or authors.
// Licensed under the Apache License, Version 2.0

using System.Collections.Concurrent;

namespace AgentScope.Core.Service;

/// <summary>
/// In-memory implementation of service discovery
/// 内存中的服务发现实现
/// 
/// 参考: agentscope-java 的服务发现概念
/// </summary>
public class InMemoryServiceDiscovery : IServiceDiscovery, IDisposable
{
    private readonly ConcurrentDictionary<string, ServiceInfo> _services = new();
    private readonly ConcurrentDictionary<string, List<Action<ServiceChangeEvent>>> _watchers = new();
    private readonly Timer _cleanupTimer;
    private readonly TimeSpan _ttl;
    private bool _disposed;

    /// <summary>
    /// Creates a new in-memory service discovery
    /// 创建新的内存服务发现
    /// </summary>
    public InMemoryServiceDiscovery(TimeSpan? ttl = null)
    {
        _ttl = ttl ?? TimeSpan.FromMinutes(5);
        _cleanupTimer = new Timer(
            _ => CleanupExpiredServices(),
            null,
            TimeSpan.FromMinutes(1),
            TimeSpan.FromMinutes(1));
    }

    /// <summary>
    /// Register a service
    /// 注册服务
    /// </summary>
    public Task RegisterAsync(ServiceInfo service, CancellationToken ct = default)
    {
        if (service == null)
            throw new ArgumentNullException(nameof(service));

        var isUpdate = _services.ContainsKey(service.Id);
        _services[service.Id] = service;

        // Notify watchers
        NotifyWatchers(new ServiceChangeEvent
        {
            ChangeType = isUpdate ? ServiceChangeType.Updated : ServiceChangeType.Registered,
            Service = service
        });

        return Task.CompletedTask;
    }

    /// <summary>
    /// Deregister a service
    /// 注销服务
    /// </summary>
    public Task DeregisterAsync(string serviceId, CancellationToken ct = default)
    {
        if (_services.TryRemove(serviceId, out var service))
        {
            // Notify watchers
            NotifyWatchers(new ServiceChangeEvent
            {
                ChangeType = ServiceChangeType.Deregistered,
                Service = service
            });
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Discover services by name
    /// 按名称发现服务
    /// </summary>
    public Task<IReadOnlyList<ServiceInfo>> DiscoverAsync(string serviceName, CancellationToken ct = default)
    {
        var services = _services.Values
            .Where(s => s.Name.Equals(serviceName, StringComparison.OrdinalIgnoreCase))
            .Where(s => IsServiceHealthy(s))
            .OrderBy(s => s.LastHeartbeat)
            .ToList()
            .AsReadOnly();

        return Task.FromResult<IReadOnlyList<ServiceInfo>>(services);
    }

    /// <summary>
    /// Get service by ID
    /// 按ID获取服务
    /// </summary>
    public Task<ServiceInfo?> GetServiceAsync(string serviceId, CancellationToken ct = default)
    {
        _services.TryGetValue(serviceId, out var service);
        return Task.FromResult(service);
    }

    /// <summary>
    /// List all registered services
    /// 列出所有已注册服务
    /// </summary>
    public Task<IReadOnlyList<ServiceInfo>> ListServicesAsync(CancellationToken ct = default)
    {
        return Task.FromResult<IReadOnlyList<ServiceInfo>>(
            _services.Values.ToList().AsReadOnly());
    }

    /// <summary>
    /// Watch for service changes
    /// 监视服务变化
    /// </summary>
    public Task WatchAsync(Action<ServiceChangeEvent> callback, CancellationToken ct = default)
    {
        var watcherId = Guid.NewGuid().ToString();
        _watchers[watcherId] = new List<Action<ServiceChangeEvent>> { callback };

        // Return a task that completes when cancellation is requested
        var tcs = new TaskCompletionSource();
        ct.Register(() =>
        {
            _watchers.TryRemove(watcherId, out _);
            tcs.TrySetResult();
        });

        return tcs.Task;
    }

    /// <summary>
    /// Update service heartbeat
    /// 更新服务心跳
    /// </summary>
    public Task UpdateHeartbeatAsync(string serviceId, CancellationToken ct = default)
    {
        if (_services.TryGetValue(serviceId, out var service))
        {
            service.LastHeartbeat = DateTime.UtcNow;
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Check if service is healthy
    /// 检查服务是否健康
    /// </summary>
    private bool IsServiceHealthy(ServiceInfo service)
    {
        return DateTime.UtcNow - service.LastHeartbeat < _ttl;
    }

    /// <summary>
    /// Cleanup expired services
    /// 清理过期服务
    /// </summary>
    private void CleanupExpiredServices()
    {
        var expiredServices = _services
            .Where(kvp => !IsServiceHealthy(kvp.Value))
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var serviceId in expiredServices)
        {
            if (_services.TryRemove(serviceId, out var service))
            {
                NotifyWatchers(new ServiceChangeEvent
                {
                    ChangeType = ServiceChangeType.HealthChanged,
                    Service = service
                });
            }
        }
    }

    /// <summary>
    /// Notify watchers of a change
    /// 通知观察者变化
    /// </summary>
    private void NotifyWatchers(ServiceChangeEvent changeEvent)
    {
        foreach (var watchers in _watchers.Values)
        {
            foreach (var watcher in watchers)
            {
                try
                {
                    watcher(changeEvent);
                }
                catch (global::System.Exception ex)
                {
                    Console.Error.WriteLine($"通知观察者时出错：{ex.Message}");
                }
            }
        }
    }

    /// <summary>
    /// Dispose resources
    /// 释放资源
    /// </summary>
    public void Dispose()
    {
        if (!_disposed)
        {
            _cleanupTimer.Dispose();
            _services.Clear();
            _watchers.Clear();
            _disposed = true;
        }
    }
}

/// <summary>
/// Service discovery builder
/// 服务发现构建器
/// </summary>
public class ServiceDiscoveryBuilder
{
    /// <summary>
    /// Create in-memory service discovery
    /// 创建内存服务发现
    /// </summary>
    public static IServiceDiscovery CreateInMemory(TimeSpan? ttl = null)
    {
        return new InMemoryServiceDiscovery(ttl);
    }
}
