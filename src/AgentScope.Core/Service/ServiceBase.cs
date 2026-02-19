// Copyright 2024-2026 the original author or authors.
// Licensed under the Apache License, Version 2.0

using System.Reactive.Linq;
using AgentScope.Core.Agent;
using AgentScope.Core.Message;

namespace AgentScope.Core.Service;

/// <summary>
/// Abstract base class for services
/// 服务的抽象基类
/// 
/// 参考: agentscope-java 的 ServiceBase 概念
/// </summary>
public abstract class ServiceBase : AgentBase, IService
{
    private readonly CancellationTokenSource _cts = new();
    private ServiceStatus _status = ServiceStatus.Stopped;
    private readonly object _statusLock = new();

    /// <summary>
    /// Service information
    /// 服务信息
    /// </summary>
    public ServiceInfo Info { get; protected set; }

    /// <summary>
    /// Current service status
    /// 当前服务状态
    /// </summary>
    public ServiceStatus Status
    {
        get
        {
            lock (_statusLock)
            {
                return _status;
            }
        }
        protected set
        {
            ServiceStatus oldStatus;
            lock (_statusLock)
            {
                if (_status == value) return;
                oldStatus = _status;
                _status = value;
            }
            OnStatusChanged(oldStatus, value);
        }
    }

    /// <summary>
    /// Event raised when service status changes
    /// 服务状态变化时触发的事件
    /// </summary>
    public event EventHandler<ServiceStatusChangedEventArgs>? StatusChanged;

    /// <summary>
    /// Event raised when heartbeat is sent
    /// 发送心跳时触发的事件
    /// </summary>
    public event EventHandler<HeartbeatEventArgs>? Heartbeat;

    /// <summary>
    /// Heartbeat interval
    /// 心跳间隔
    /// </summary>
    protected TimeSpan HeartbeatInterval { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Creates a new service
    /// 创建新服务
    /// </summary>
    protected ServiceBase(ServiceInfo info) : base(info.Name)
    {
        Info = info ?? throw new ArgumentNullException(nameof(info));
    }

    /// <summary>
    /// Start the service
    /// 启动服务
    /// </summary>
    public virtual async Task StartAsync(CancellationToken ct = default)
    {
        if (Status == ServiceStatus.Running)
            return;

        Status = ServiceStatus.Starting;

        try
        {
            await OnStartAsync(ct);
            Status = ServiceStatus.Running;

            // Start heartbeat loop
            _ = HeartbeatLoopAsync(_cts.Token);
        }
        catch (global::System.Exception ex)
        {
            Status = ServiceStatus.Error;
            OnStatusChanged(ServiceStatus.Starting, ServiceStatus.Error, ex.Message);
            throw;
        }
    }

    /// <summary>
    /// Stop the service
    /// 停止服务
    /// </summary>
    public virtual async Task StopAsync(CancellationToken ct = default)
    {
        if (Status == ServiceStatus.Stopped)
            return;

        Status = ServiceStatus.Stopping;

        try
        {
            await OnStopAsync(ct);
            Status = ServiceStatus.Stopped;
        }
        catch (global::System.Exception ex)
        {
            Status = ServiceStatus.Error;
            OnStatusChanged(ServiceStatus.Stopping, ServiceStatus.Error, ex.Message);
            throw;
        }
    }

    /// <summary>
    /// Check if service is healthy
    /// 检查服务是否健康
    /// </summary>
    public virtual Task<bool> HealthCheckAsync(CancellationToken ct = default)
    {
        return Task.FromResult(Status == ServiceStatus.Running);
    }

    /// <summary>
    /// Send heartbeat
    /// 发送心跳
    /// </summary>
    public virtual Task HeartbeatAsync(CancellationToken ct = default)
    {
        Heartbeat?.Invoke(this, new HeartbeatEventArgs
        {
            ServiceId = Info.Id,
            Status = Status
        });
        return Task.CompletedTask;
    }

    /// <summary>
    /// Process a message (implements IAgent)
    /// 处理消息（实现IAgent接口）
    /// </summary>
    public override IObservable<Msg> Call(Msg message)
    {
        return Observable.FromAsync(async ct =>
        {
            if (Status != ServiceStatus.Running)
            {
                return Msg.Builder()
                    .Role("system")
                    .Content($"服务 {Info.Name} 未运行。当前状态：{Status}")
                    .Build();
            }

            return await ProcessMessageAsync(message, ct);
        });
    }

    /// <summary>
    /// Called when starting the service
    /// 启动服务时调用
    /// </summary>
    protected abstract Task OnStartAsync(CancellationToken ct);

    /// <summary>
    /// Called when stopping the service
    /// 停止服务时调用
    /// </summary>
    protected abstract Task OnStopAsync(CancellationToken ct);

    /// <summary>
    /// Process a message
    /// 处理消息
    /// </summary>
    protected abstract Task<Msg> ProcessMessageAsync(Msg message, CancellationToken ct);

    /// <summary>
    /// Heartbeat loop
    /// 心跳循环
    /// </summary>
    private async Task HeartbeatLoopAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested && Status == ServiceStatus.Running)
        {
            try
            {
                await Task.Delay(HeartbeatInterval, ct);
                if (Status == ServiceStatus.Running)
                {
                    await HeartbeatAsync(ct);
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (global::System.Exception)
            {
// 记录心跳错误但不停止服务
            // Log heartbeat error but don't stop the service
            }
        }
    }

    /// <summary>
    /// Trigger status changed event
    /// 触发状态变化事件
    /// </summary>
    protected virtual void OnStatusChanged(ServiceStatus oldStatus, ServiceStatus newStatus, string? errorMessage = null)
    {
        StatusChanged?.Invoke(this, new ServiceStatusChangedEventArgs
        {
            PreviousStatus = oldStatus,
            NewStatus = newStatus,
            ErrorMessage = errorMessage
        });
    }

    /// <summary>
    /// Dispose resources
    /// 释放资源
    /// </summary>
    public virtual void Dispose()
    {
        _cts.Cancel();
        _cts.Dispose();
    }
}

/// <summary>
/// Simple service implementation that wraps an agent
/// 包装Agent的简单服务实现
/// </summary>
public class AgentService : ServiceBase
{
    private readonly Func<Msg, CancellationToken, Task<Msg>> _handler;

    /// <summary>
    /// Creates a new agent service
    /// 创建新的Agent服务
    /// </summary>
    public AgentService(
        ServiceInfo info,
        Func<Msg, CancellationToken, Task<Msg>> handler) : base(info)
    {
        _handler = handler ?? throw new ArgumentNullException(nameof(handler));
    }

    /// <summary>
    /// Called when starting the service
    /// 启动服务时调用
    /// </summary>
    protected override Task OnStartAsync(CancellationToken ct)
    {
        // Nothing special to start
        return Task.CompletedTask;
    }

    /// <summary>
    /// Called when stopping the service
    /// 停止服务时调用
    /// </summary>
    protected override Task OnStopAsync(CancellationToken ct)
    {
        // Nothing special to stop
        return Task.CompletedTask;
    }

    /// <summary>
    /// Process a message
    /// 处理消息
    /// </summary>
    protected override Task<Msg> ProcessMessageAsync(Msg message, CancellationToken ct)
    {
        return _handler(message, ct);
    }
}
