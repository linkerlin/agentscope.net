// Copyright 2024-2026 the original author or authors.
// Licensed under the Apache License, Version 2.0

using System.Collections.Concurrent;

namespace AgentScope.Core.Interruption;

/// <summary>
/// Manages cancellation for multiple operations
/// 管理多个操作的取消
/// 
/// 参考: agentscope-java 的 Cancellation 管理概念
/// </summary>
public class CancellationManager : IDisposable
{
    private readonly ConcurrentDictionary<string, CancellationTokenSource> _tokens = new();
    private readonly ConcurrentDictionary<string, InterruptionContext> _interruptions = new();
    private readonly ConcurrentDictionary<string, InterruptionState> _savedStates = new();
    private bool _disposed;

    /// <summary>
    /// Create a new cancellation scope
    /// 创建新的取消作用域
    /// </summary>
    public CancellationScope CreateScope(string operationId)
    {
        var cts = new CancellationTokenSource();
        _tokens[operationId] = cts;
        return new CancellationScope(operationId, cts, this);
    }

    /// <summary>
    /// Get cancellation token for an operation
    /// 获取操作的取消令牌
    /// </summary>
    public CancellationToken GetToken(string operationId)
    {
        if (_tokens.TryGetValue(operationId, out var cts))
        {
            return cts.Token;
        }
        throw new InvalidOperationException($"No cancellation token found for operation {operationId}");
    }

    /// <summary>
    /// Try to get cancellation token for an operation
    /// 尝试获取操作的取消令牌
    /// </summary>
    public bool TryGetToken(string operationId, out CancellationToken token)
    {
        if (_tokens.TryGetValue(operationId, out var cts))
        {
            token = cts.Token;
            return true;
        }
        token = default;
        return false;
    }

    /// <summary>
    /// Cancel an operation
    /// 取消操作
    /// </summary>
    public async Task CancelAsync(
        string operationId,
        InterruptionReason reason = InterruptionReason.UserCancelled,
        string? message = null,
        bool preserveState = true)
    {
        var context = new InterruptionContext
        {
            Reason = reason,
            Message = message,
            Source = "CancellationManager",
            PreserveState = preserveState
        };

        await InterruptAsync(operationId, context);
    }

    /// <summary>
    /// Interrupt an operation with full context
    /// 使用完整上下文中断操作
    /// </summary>
    public async Task InterruptAsync(string operationId, InterruptionContext context)
    {
        _interruptions[operationId] = context;

        if (_tokens.TryGetValue(operationId, out var cts))
        {
            // Request cancellation
            cts.Cancel();

            // Wait a short time for graceful shutdown
            try
            {
                await Task.Delay(100, cts.Token);
            }
            catch (OperationCanceledException)
            {
                // Expected
            }
        }
    }

    /// <summary>
    /// Cancel all operations
    /// 取消所有操作
    /// </summary>
    public async Task CancelAllAsync(InterruptionReason reason = InterruptionReason.UserCancelled)
    {
        var tasks = _tokens.Keys.Select(id => CancelAsync(id, reason));
        await Task.WhenAll(tasks);
    }

    /// <summary>
    /// Save state for an operation
    /// 保存操作状态
    /// </summary>
    public void SaveState(string operationId, InterruptionState state)
    {
        _savedStates[operationId] = state;
    }

    /// <summary>
    /// Get saved state for an operation
    /// 获取操作的保存状态
    /// </summary>
    public InterruptionState? GetSavedState(string operationId)
    {
        _savedStates.TryGetValue(operationId, out var state);
        return state;
    }

    /// <summary>
    /// Remove saved state
    /// 移除保存的状态
    /// </summary>
    public bool RemoveSavedState(string operationId)
    {
        return _savedStates.TryRemove(operationId, out _);
    }

    /// <summary>
    /// Check if an operation is cancelled
    /// 检查操作是否已取消
    /// </summary>
    public bool IsCancelled(string operationId)
    {
        return _interruptions.ContainsKey(operationId);
    }

    /// <summary>
    /// Get interruption context for an operation
    /// 获取操作的中断上下文
    /// </summary>
    public InterruptionContext? GetInterruptionContext(string operationId)
    {
        _interruptions.TryGetValue(operationId, out var context);
        return context;
    }

    /// <summary>
    /// Clean up resources for an operation
    /// 清理操作的资源
    /// </summary>
    public void Cleanup(string operationId)
    {
        if (_tokens.TryRemove(operationId, out var cts))
        {
            cts.Dispose();
        }
        _interruptions.TryRemove(operationId, out _);
    }

    /// <summary>
    /// List all active operation IDs
    /// 列出所有活动操作ID
    /// </summary>
    public IReadOnlyList<string> GetActiveOperationIds()
    {
        return _tokens.Keys.ToList().AsReadOnly();
    }

    /// <summary>
    /// Dispose resources
    /// 释放资源
    /// </summary>
    public void Dispose()
    {
        if (!_disposed)
        {
            foreach (var cts in _tokens.Values)
            {
                cts.Dispose();
            }
            _tokens.Clear();
            _interruptions.Clear();
            _savedStates.Clear();
            _disposed = true;
        }
    }
}

/// <summary>
/// Cancellation scope for an operation
/// 操作的取消作用域
/// </summary>
public class CancellationScope : IDisposable
{
    private readonly CancellationTokenSource _cts;
    private readonly CancellationManager _manager;
    private bool _disposed;

    /// <summary>
    /// Operation ID
    /// </summary>
    public string OperationId { get; }

    /// <summary>
    /// Cancellation token
    /// </summary>
    public CancellationToken Token => _cts.Token;

    /// <summary>
    /// Whether cancellation has been requested
    /// </summary>
    public bool IsCancellationRequested => _cts.IsCancellationRequested;

    /// <summary>
    /// Creates a new cancellation scope
    /// </summary>
    internal CancellationScope(string operationId, CancellationTokenSource cts, CancellationManager manager)
    {
        OperationId = operationId;
        _cts = cts;
        _manager = manager;
    }

    /// <summary>
    /// Throw if cancellation requested
    /// 如果请求取消则抛出异常
    /// </summary>
    public void ThrowIfCancellationRequested()
    {
        _cts.Token.ThrowIfCancellationRequested();
    }

    /// <summary>
    /// Register a callback for cancellation
    /// 注册取消回调
    /// </summary>
    public CancellationTokenRegistration Register(Action callback)
    {
        return _cts.Token.Register(callback);
    }

    /// <summary>
    /// Register a callback for cancellation with state
    /// 注册带状态的取消回调
    /// </summary>
    public CancellationTokenRegistration Register(Action<object?> callback, object? state)
    {
        return _cts.Token.Register(callback, state);
    }

    /// <summary>
    /// Link with another cancellation token
    /// 链接到另一个取消令牌
    /// </summary>
    public CancellationTokenSource LinkToken(CancellationToken otherToken)
    {
        return CancellationTokenSource.CreateLinkedTokenSource(_cts.Token, otherToken);
    }

    /// <summary>
    /// Dispose resources
    /// </summary>
    public void Dispose()
    {
        if (!_disposed)
        {
            _manager.Cleanup(OperationId);
            _disposed = true;
        }
    }
}

/// <summary>
/// Helper for cooperative cancellation checks
/// 协作式取消检查帮助类
/// </summary>
public static class CancellationHelper
{
    /// <summary>
    /// Check and throw if cancellation is requested
    /// 检查并在请求取消时抛出
    /// </summary>
    public static void CheckCancellation(CancellationToken token, string? operationName = null)
    {
        if (token.IsCancellationRequested)
        {
            throw new OperationCanceledException(
                operationName != null 
                    ? $"Operation '{operationName}' was cancelled" 
                    : "Operation was cancelled", 
                token);
        }
    }

    /// <summary>
    /// Execute action with periodic cancellation checks
    /// 执行带定期取消检查的操作
    /// </summary>
    public static async Task WithPeriodicCheckAsync(
        Func<Task> action,
        CancellationToken token,
        TimeSpan checkInterval)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(token);
        var checkTask = Task.Delay(checkInterval, cts.Token);
        var actionTask = action();

        while (!actionTask.IsCompleted)
        {
            var completedTask = await Task.WhenAny(actionTask, checkTask);
            
            if (completedTask == checkTask)
            {
                token.ThrowIfCancellationRequested();
                checkTask = Task.Delay(checkInterval, cts.Token);
            }
            else
            {
                break;
            }
        }

        await actionTask; // Propagate any exceptions
    }

    /// <summary>
    /// Create a timeout token
    /// 创建超时令牌
    /// </summary>
    public static CancellationToken CreateTimeoutToken(TimeSpan timeout)
    {
        var cts = new CancellationTokenSource();
        cts.CancelAfter(timeout);
        return cts.Token;
    }

    /// <summary>
    /// Combine multiple tokens
    /// 组合多个令牌
    /// </summary>
    public static CancellationToken CombineTokens(params CancellationToken[] tokens)
    {
        if (tokens.Length == 0) return default;
        if (tokens.Length == 1) return tokens[0];
        
        var cts = CancellationTokenSource.CreateLinkedTokenSource(tokens);
        return cts.Token;
    }
}
