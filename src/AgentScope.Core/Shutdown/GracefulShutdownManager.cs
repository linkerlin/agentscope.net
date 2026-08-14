// Copyright 2024-2026 the original author or authors.
// Licensed under the Apache License, Version 2.0

using System;
using System.Collections.Concurrent;
using System.Threading;

namespace AgentScope.Core.Shutdown;

/// <summary>
/// 优雅关闭管理器，追踪所有活跃 Agent 请求，支持安全中止
/// 对应 Java: io.agentscope.core.shutdown.GracefulShutdownManager
/// </summary>
public class GracefulShutdownManager : IDisposable
{
    private static readonly Lazy<GracefulShutdownManager> _instance = new(() => new());
    public static GracefulShutdownManager Instance => _instance.Value;

    private readonly ConcurrentDictionary<string, ShutdownRequest> _activeRequests = new();
    private readonly CancellationTokenSource _globalCts = new();
    private volatile ShutdownState _state = ShutdownState.Running;

    public ShutdownState State => _state;
    public CancellationToken Token => _globalCts.Token;

    /// <summary>注册一个活跃请求，返回 requestId</summary>
    public string RegisterRequest(object agent)
    {
        var requestId = Guid.NewGuid().ToString();
        _activeRequests[requestId] = new ShutdownRequest
        {
            RequestId = requestId,
            AgentName = agent.GetType().Name,
            RegisteredAt = DateTime.UtcNow
        };
        return requestId;
    }

    /// <summary>取消注册</summary>
    public void UnregisterRequest(string requestId)
    {
        _activeRequests.TryRemove(requestId, out _);
    }

    /// <summary>绑定状态到请求</summary>
    public void BindRequestState(string requestId, object? state)
    {
        if (_activeRequests.TryGetValue(requestId, out var req))
        {
            req.State = state;
        }
    }

    /// <summary>确保仍在接受请求，否则抛出 AgentShuttingDownException</summary>
    public void EnsureAcceptingRequests()
    {
        if (_state == ShutdownState.ShuttingDown || _state == ShutdownState.Completed)
        {
            throw new AgentShuttingDownException("Agent 正在关闭，不再接受新请求");
        }
    }

    /// <summary>发起关闭</summary>
    public void InitiateShutdown()
    {
        if (_state == ShutdownState.Running)
        {
            _state = ShutdownState.ShuttingDown;
            _globalCts.Cancel();
        }
    }

    /// <summary>完成关闭</summary>
    public void Complete() => _state = ShutdownState.Completed;

    /// <summary>等待所有活跃请求完成（最多 waitTimeout）</summary>
    public void WaitForCompletion(TimeSpan waitTimeout)
    {
        var start = DateTime.UtcNow;
        while (_activeRequests.Count > 0 && DateTime.UtcNow - start < waitTimeout)
        {
            Thread.Sleep(100);
        }
    }

    public void Dispose()
    {
        _globalCts.Cancel();
        _globalCts.Dispose();
        _state = ShutdownState.Completed;
    }
}

public enum ShutdownState
{
    Running,
    ShuttingDown,
    Completed
}

public class ShutdownRequest
{
    public string RequestId { get; set; } = "";
    public string AgentName { get; set; } = "";
    public DateTime RegisteredAt { get; set; }
    public object? State { get; set; }
}

/// <summary>
/// Agent 关闭时抛出的异常
/// </summary>
public class AgentShuttingDownException : System.Exception
{
    public AgentShuttingDownException(string message) : base(message) { }
}
