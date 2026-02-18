// Copyright 2024-2026 the original author or authors.
// Licensed under the Apache License, Version 2.0

using AgentScope.Core.Agent;
using AgentScope.Core.Message;

namespace AgentScope.Core.Service;

/// <summary>
/// Service status enum
/// 服务状态枚举
/// </summary>
public enum ServiceStatus
{
    /// <summary>
    /// Service is stopped
    /// 服务已停止
    /// </summary>
    Stopped,

    /// <summary>
    /// Service is starting
    /// 服务正在启动
    /// </summary>
    Starting,

    /// <summary>
    /// Service is running
    /// 服务运行中
    /// </summary>
    Running,

    /// <summary>
    /// Service is stopping
    /// 服务正在停止
    /// </summary>
    Stopping,

    /// <summary>
    /// Service encountered an error
    /// 服务发生错误
    /// </summary>
    Error
}

/// <summary>
/// Service information
/// 服务信息
/// </summary>
public class ServiceInfo
{
    /// <summary>
    /// Service unique identifier
    /// 服务唯一标识符
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// Service name
    /// 服务名称
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Service description
    /// 服务描述
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Service version
    /// 服务版本
    /// </summary>
    public string Version { get; init; } = "1.0.0";

    /// <summary>
    /// Service host address
    /// 服务主机地址
    /// </summary>
    public string? Host { get; init; }

    /// <summary>
    /// Service port
    /// 服务端口号
    /// </summary>
    public int Port { get; init; }

    /// <summary>
    /// Service metadata
    /// 服务元数据
    /// </summary>
    public Dictionary<string, string> Metadata { get; init; } = new();

    /// <summary>
    /// Service tags for categorization
    /// 服务标签用于分类
    /// </summary>
    public List<string> Tags { get; init; } = new();

    /// <summary>
    /// Registration time
    /// 注册时间
    /// </summary>
    public DateTime RegisteredAt { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Last heartbeat time
    /// 最后心跳时间
    /// </summary>
    public DateTime LastHeartbeat { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Interface for agent-based services
/// 基于Agent的服务接口
/// 
/// 参考: agentscope-java 的 Service 概念
/// </summary>
public interface IService : IAgent
{
    /// <summary>
    /// Service information
    /// 服务信息
    /// </summary>
    ServiceInfo Info { get; }

    /// <summary>
    /// Current service status
    /// 当前服务状态
    /// </summary>
    ServiceStatus Status { get; }

    /// <summary>
    /// Start the service
    /// 启动服务
    /// </summary>
    Task StartAsync(CancellationToken ct = default);

    /// <summary>
    /// Stop the service
    /// 停止服务
    /// </summary>
    Task StopAsync(CancellationToken ct = default);

    /// <summary>
    /// Check if service is healthy
    /// 检查服务是否健康
    /// </summary>
    Task<bool> HealthCheckAsync(CancellationToken ct = default);

    /// <summary>
    /// Send heartbeat
    /// 发送心跳
    /// </summary>
    Task HeartbeatAsync(CancellationToken ct = default);

    /// <summary>
    /// Event raised when service status changes
    /// 服务状态变化时触发的事件
    /// </summary>
    event EventHandler<ServiceStatusChangedEventArgs>? StatusChanged;

    /// <summary>
    /// Event raised when heartbeat is sent
    /// 发送心跳时触发的事件
    /// </summary>
    event EventHandler<HeartbeatEventArgs>? Heartbeat;
}

/// <summary>
/// Service status changed event args
/// 服务状态变化事件参数
/// </summary>
public class ServiceStatusChangedEventArgs : EventArgs
{
    /// <summary>
    /// Previous status
    /// 之前的状态
    /// </summary>
    public required ServiceStatus PreviousStatus { get; init; }

    /// <summary>
    /// New status
    /// 新状态
    /// </summary>
    public required ServiceStatus NewStatus { get; init; }

    /// <summary>
    /// Status change timestamp
    /// 状态变更时间戳
    /// </summary>
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Optional error message if status is Error
    /// 如果状态为Error，可选的错误消息
    /// </summary>
    public string? ErrorMessage { get; init; }
}

/// <summary>
/// Heartbeat event args
/// 心跳事件参数
/// </summary>
public class HeartbeatEventArgs : EventArgs
{
    /// <summary>
    /// Service ID
    /// 服务ID
    /// </summary>
    public required string ServiceId { get; init; }

    /// <summary>
    /// Heartbeat timestamp
    /// 心跳时间戳
    /// </summary>
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Service status at heartbeat time
    /// 心跳时的服务状态
    /// </summary>
    public ServiceStatus Status { get; init; }
}

/// <summary>
/// Service discovery interface
/// 服务发现接口
/// </summary>
public interface IServiceDiscovery
{
    /// <summary>
    /// Register a service
    /// 注册服务
    /// </summary>
    Task RegisterAsync(ServiceInfo service, CancellationToken ct = default);

    /// <summary>
    /// Deregister a service
    /// 注销服务
    /// </summary>
    Task DeregisterAsync(string serviceId, CancellationToken ct = default);

    /// <summary>
    /// Discover services by name
    /// 按名称发现服务
    /// </summary>
    Task<IReadOnlyList<ServiceInfo>> DiscoverAsync(string serviceName, CancellationToken ct = default);

    /// <summary>
    /// Get service by ID
    /// 按ID获取服务
    /// </summary>
    Task<ServiceInfo?> GetServiceAsync(string serviceId, CancellationToken ct = default);

    /// <summary>
    /// List all registered services
    /// 列出所有已注册服务
    /// </summary>
    Task<IReadOnlyList<ServiceInfo>> ListServicesAsync(CancellationToken ct = default);

    /// <summary>
    /// Watch for service changes
    /// 监视服务变化
    /// </summary>
    Task WatchAsync(Action<ServiceChangeEvent> callback, CancellationToken ct = default);
}

/// <summary>
/// Service change event
/// 服务变更事件
/// </summary>
public class ServiceChangeEvent
{
    /// <summary>
    /// Change type
    /// 变更类型
    /// </summary>
    public ServiceChangeType ChangeType { get; init; }

    /// <summary>
    /// Service information
    /// 服务信息
    /// </summary>
    public required ServiceInfo Service { get; init; }

    /// <summary>
    /// Event timestamp
    /// 事件时间戳
    /// </summary>
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Service change type
/// 服务变更类型
/// </summary>
public enum ServiceChangeType
{
    /// <summary>
    /// Service registered
    /// 服务已注册
    /// </summary>
    Registered,

    /// <summary>
    /// Service deregistered
    /// 服务已注销
    /// </summary>
    Deregistered,

    /// <summary>
    /// Service updated
    /// 服务已更新
    /// </summary>
    Updated,

    /// <summary>
    /// Service health status changed
    /// 服务健康状态变化
    /// </summary>
    HealthChanged
}
