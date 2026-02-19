// Copyright 2024-2026 the original author or authors.
// Licensed under the Apache License, Version 2.0

using System.Collections.Concurrent;

namespace AgentScope.Core.Service;

/// <summary>
/// Manages multiple services
/// 管理多个服务
/// 
/// 参考: agentscope-java 的 ServiceManager 概念
/// </summary>
public class ServiceManager : IDisposable
{
    private readonly ConcurrentDictionary<string, IService> _services = new();
    private readonly IServiceDiscovery? _discovery;
    private readonly Timer? _healthCheckTimer;
    private bool _disposed;

    /// <summary>
    /// Number of registered services
    /// 已注册服务数量
    /// </summary>
    public int Count => _services.Count;

    /// <summary>
    /// All registered service IDs
    /// 所有已注册服务ID
    /// </summary>
    public IReadOnlyCollection<string> ServiceIds => _services.Keys.ToList().AsReadOnly();

    /// <summary>
    /// Creates a new service manager
    /// 创建新的服务管理器
    /// </summary>
    public ServiceManager(IServiceDiscovery? discovery = null, TimeSpan? healthCheckInterval = null)
    {
        _discovery = discovery;

        if (healthCheckInterval.HasValue)
        {
            _healthCheckTimer = new Timer(
                async _ => await RunHealthChecksAsync(),
                null,
                healthCheckInterval.Value,
                healthCheckInterval.Value);
        }
    }

    /// <summary>
    /// Register a service
    /// 注册服务
    /// </summary>
    public async Task<bool> RegisterAsync(IService service, CancellationToken ct = default)
    {
        if (service == null)
            throw new ArgumentNullException(nameof(service));

        var serviceId = service.Info.Id;

        if (_services.TryAdd(serviceId, service))
        {
            // Subscribe to service events
            service.StatusChanged += OnServiceStatusChanged;
            service.Heartbeat += OnServiceHeartbeat;

            // Register with discovery if available
            if (_discovery != null)
            {
                await _discovery.RegisterAsync(service.Info, ct);
            }

            return true;
        }

        return false;
    }

    /// <summary>
    /// Unregister a service
    /// 注销服务
    /// </summary>
    public async Task<bool> UnregisterAsync(string serviceId, CancellationToken ct = default)
    {
        if (_services.TryRemove(serviceId, out var service))
        {
            // Unsubscribe from events
            service.StatusChanged -= OnServiceStatusChanged;
            service.Heartbeat -= OnServiceHeartbeat;

            // Stop the service if running
            if (service.Status == ServiceStatus.Running)
            {
                await service.StopAsync(ct);
            }

            // Deregister from discovery if available
            if (_discovery != null)
            {
                await _discovery.DeregisterAsync(serviceId, ct);
            }

            return true;
        }

        return false;
    }

    /// <summary>
    /// Get a service by ID
    /// 按ID获取服务
    /// </summary>
    public IService? GetService(string serviceId)
    {
        _services.TryGetValue(serviceId, out var service);
        return service;
    }

    /// <summary>
    /// Get a service by name
    /// 按名称获取服务
    /// </summary>
    public IService? GetServiceByName(string serviceName)
    {
        return _services.Values.FirstOrDefault(s => s.Info.Name == serviceName);
    }

    /// <summary>
    /// Get all services
    /// 获取所有服务
    /// </summary>
    public IReadOnlyList<IService> GetAllServices()
    {
        return _services.Values.ToList().AsReadOnly();
    }

    /// <summary>
    /// Get services by tag
    /// 按标签获取服务
    /// </summary>
    public IReadOnlyList<IService> GetServicesByTag(string tag)
    {
        return _services.Values
            .Where(s => s.Info.Tags.Contains(tag))
            .ToList()
            .AsReadOnly();
    }

    /// <summary>
    /// Start a service
    /// 启动服务
    /// </summary>
    public async Task<bool> StartServiceAsync(string serviceId, CancellationToken ct = default)
    {
        if (_services.TryGetValue(serviceId, out var service))
        {
            await service.StartAsync(ct);
            return true;
        }
        return false;
    }

    /// <summary>
    /// Stop a service
    /// 停止服务
    /// </summary>
    public async Task<bool> StopServiceAsync(string serviceId, CancellationToken ct = default)
    {
        if (_services.TryGetValue(serviceId, out var service))
        {
            await service.StopAsync(ct);
            return true;
        }
        return false;
    }

    /// <summary>
    /// Start all services
    /// 启动所有服务
    /// </summary>
    public async Task StartAllAsync(CancellationToken ct = default)
    {
        var tasks = _services.Values.Select(s => s.StartAsync(ct));
        await Task.WhenAll(tasks);
    }

    /// <summary>
    /// Stop all services
    /// 停止所有服务
    /// </summary>
    public async Task StopAllAsync(CancellationToken ct = default)
    {
        var tasks = _services.Values.Select(s => s.StopAsync(ct));
        await Task.WhenAll(tasks);
    }

    /// <summary>
    /// Check health of all services
    /// 检查所有服务的健康状态
    /// </summary>
    public async Task<Dictionary<string, bool>> CheckHealthAsync(CancellationToken ct = default)
    {
        var results = new Dictionary<string, bool>();
        var tasks = _services.Select(async kvp =>
        {
            var (id, service) = kvp;
            var isHealthy = await service.HealthCheckAsync(ct);
            lock (results)
            {
                results[id] = isHealthy;
            }
        });

        await Task.WhenAll(tasks);
        return results;
    }

    /// <summary>
    /// Get service statistics
    /// 获取服务统计信息
    /// </summary>
    public ServiceStatistics GetStatistics()
    {
        var services = _services.Values.ToList();
        return new ServiceStatistics
        {
            TotalCount = services.Count,
            RunningCount = services.Count(s => s.Status == ServiceStatus.Running),
            StoppedCount = services.Count(s => s.Status == ServiceStatus.Stopped),
            ErrorCount = services.Count(s => s.Status == ServiceStatus.Error),
            StartingCount = services.Count(s => s.Status == ServiceStatus.Starting),
            StoppingCount = services.Count(s => s.Status == ServiceStatus.Stopping)
        };
    }

    /// <summary>
    /// Event raised when service status changes
    /// 服务状态变化时触发的事件
    /// </summary>
    public event EventHandler<ServiceStatusChangedEventArgs>? ServiceStatusChanged;

    /// <summary>
    /// Event raised when service heartbeat is received
    /// 收到服务心跳时触发的事件
    /// </summary>
    public event EventHandler<HeartbeatEventArgs>? ServiceHeartbeat;

    /// <summary>
    /// Run health checks on all services
    /// 对所有服务运行健康检查
    /// </summary>
    private async Task RunHealthChecksAsync()
    {
        foreach (var service in _services.Values)
        {
            try
            {
                if (service.Status == ServiceStatus.Running)
                {
                    var isHealthy = await service.HealthCheckAsync();
                    if (!isHealthy)
                    {
                        Console.Error.WriteLine($"服务 {service.Info.Name} 健康检查失败");
                    }
                }
            }
            catch (global::System.Exception ex)
            {
                Console.Error.WriteLine($"服务 {service.Info.Name} 健康检查出错：{ex.Message}");
            }
        }
    }

    private void OnServiceStatusChanged(object? sender, ServiceStatusChangedEventArgs e)
    {
        if (sender is IService service)
        {
            ServiceStatusChanged?.Invoke(this, e);
        }
    }

    private void OnServiceHeartbeat(object? sender, HeartbeatEventArgs e)
    {
        ServiceHeartbeat?.Invoke(this, e);
    }

    /// <summary>
    /// Dispose resources
    /// 释放资源
    /// </summary>
    public void Dispose()
    {
        if (!_disposed)
        {
            _healthCheckTimer?.Dispose();

            foreach (var service in _services.Values)
            {
                service.StatusChanged -= OnServiceStatusChanged;
                service.Heartbeat -= OnServiceHeartbeat;
                (service as IDisposable)?.Dispose();
            }

            _services.Clear();
            _disposed = true;
        }
    }
}

/// <summary>
/// Service statistics
/// 服务统计信息
/// </summary>
public class ServiceStatistics
{
    /// <summary>
    /// Total number of services
    /// 服务总数
    /// </summary>
    public int TotalCount { get; init; }

    /// <summary>
    /// Number of running services
    /// 运行中的服务数量
    /// </summary>
    public int RunningCount { get; init; }

    /// <summary>
    /// Number of stopped services
    /// 已停止的服务数量
    /// </summary>
    public int StoppedCount { get; init; }

    /// <summary>
    /// Number of services with errors
    /// 出错的服务数量
    /// </summary>
    public int ErrorCount { get; init; }

    /// <summary>
    /// Number of services starting
    /// 正在启动的服务数量
    /// </summary>
    public int StartingCount { get; init; }

    /// <summary>
    /// Number of services stopping
    /// 正在停止的服务数量
    /// </summary>
    public int StoppingCount { get; init; }
}
