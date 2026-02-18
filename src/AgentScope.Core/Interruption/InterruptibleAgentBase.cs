// Copyright 2024-2026 the original author or authors.
// Licensed under the Apache License, Version 2.0

using System.Reactive.Linq;
using AgentScope.Core.Agent;
using AgentScope.Core.Message;

namespace AgentScope.Core.Interruption;

/// <summary>
/// Base class for interruptible agents
/// 可中断 Agent 的基类
/// 
/// 参考: agentscope-java 的 Interruptible 概念
/// </summary>
public abstract class InterruptibleAgentBase : AgentBase, IInterruptible, IResumable
{
    private readonly CancellationTokenSource _cts = new();
    private readonly object _stateLock = new();
    private bool _isRunning;
    private InterruptionState? _savedState;

    /// <inheritdoc />
    public bool IsRunning
    {
        get
        {
            lock (_stateLock)
            {
                return _isRunning;
            }
        }
        protected set
        {
            lock (_stateLock)
            {
                _isRunning = value;
            }
        }
    }

    /// <inheritdoc />
    public bool IsCancellationRequested => _cts.IsCancellationRequested;

    /// <inheritdoc />
    public CancellationToken CancellationToken => _cts.Token;

    /// <inheritdoc />
    public bool CanResume => _savedState != null;

    /// <summary>
    /// Progress reporter for long-running operations
    /// 长时间运行操作的进度报告器
    /// </summary>
    public IProgress<OperationProgress>? ProgressReporter { get; set; }

    /// <inheritdoc />
    public event EventHandler<InterruptionContext>? InterruptionRequested;

    /// <inheritdoc />
    public event EventHandler<InterruptionContext>? Interrupted;

    /// <summary>
    /// Creates a new interruptible agent
    /// 创建新的可中断 Agent
    /// </summary>
    protected InterruptibleAgentBase(string name) : base(name)
    {
    }

    /// <inheritdoc />
    public override IObservable<Msg> Call(Msg message)
    {
        return Observable.FromAsync(async ct =>
        {
            // Link external cancellation token with internal one
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(_cts.Token, ct);
            
            try
            {
                IsRunning = true;
                return await ExecuteAsync(message, linkedCts.Token);
            }
            catch (OperationCanceledException) when (_cts.IsCancellationRequested)
            {
                // Handle our cancellation
                return Msg.Builder()
                    .Role("system")
                    .Content("Operation was interrupted")
                    .Build();
            }
            finally
            {
                IsRunning = false;
            }
        });
    }

    /// <inheritdoc />
    public virtual async Task InterruptAsync(InterruptionContext context)
    {
        if (!IsRunning) return;

        InterruptionRequested?.Invoke(this, context);

        // Request cancellation
        _cts.Cancel();

        // Wait for operation to complete gracefully
        var timeout = TimeSpan.FromSeconds(5);
        var startTime = DateTime.UtcNow;
        
        while (IsRunning && DateTime.UtcNow - startTime < timeout)
        {
            await Task.Delay(50);
        }

        // Capture state if requested
        if (context.PreserveState)
        {
            _savedState = await CaptureStateAsync();
        }

        Interrupted?.Invoke(this, context);
    }

    /// <inheritdoc />
    public virtual Task<InterruptionState> CaptureStateAsync()
    {
        var state = new InterruptionState
        {
            Id = Guid.NewGuid().ToString(),
            OperationType = GetType().FullName ?? "Unknown",
            CapturedAt = DateTime.UtcNow,
            Progress = GetCurrentProgress(),
            Data = new Dictionary<string, object>()
        };

        // Add custom state data
        CaptureCustomState(state.Data);

        _savedState = state;
        return Task.FromResult(state);
    }

    /// <inheritdoc />
    public virtual async Task ResumeAsync(InterruptionState state)
    {
        if (!CanResume)
        {
            throw new InvalidOperationException("No saved state to resume from");
        }

        // Reset cancellation token
        _cts.TryReset();

        // Restore custom state
        RestoreCustomState(state.Data);

        // Resume operation
        await ResumeOperationAsync(state);
    }

    /// <summary>
    /// Execute the agent logic
    /// 执行 Agent 逻辑
    /// </summary>
    protected abstract Task<Msg> ExecuteAsync(Msg message, CancellationToken ct);

    /// <summary>
    /// Get current progress (0-100)
    /// 获取当前进度 (0-100)
    /// </summary>
    protected virtual double GetCurrentProgress()
    {
        return 0;
    }

    /// <summary>
    /// Capture custom state data
    /// 捕获自定义状态数据
    /// </summary>
    protected virtual void CaptureCustomState(Dictionary<string, object> stateData)
    {
        // Override in derived classes to add custom state
    }

    /// <summary>
    /// Restore custom state data
    /// 恢复自定义状态数据
    /// </summary>
    protected virtual void RestoreCustomState(Dictionary<string, object> stateData)
    {
        // Override in derived classes to restore custom state
    }

    /// <summary>
    /// Resume operation from saved state
    /// 从保存的状态恢复操作
    /// </summary>
    protected virtual Task ResumeOperationAsync(InterruptionState state)
    {
        // Override in derived classes to implement resume logic
        return Task.CompletedTask;
    }

    /// <summary>
    /// Report progress
    /// 报告进度
    /// </summary>
    protected void ReportProgress(OperationProgress progress)
    {
        ProgressReporter?.Report(progress);
    }

    /// <summary>
    /// Check cancellation and throw if requested
    /// 检查取消并在请求时抛出
    /// </summary>
    protected void CheckCancellation()
    {
        CancellationToken.ThrowIfCancellationRequested();
    }

    /// <summary>
    /// Safe delay that checks for cancellation
    /// 带取消检查的安全延迟
    /// </summary>
    protected async Task DelayAsync(TimeSpan delay, CancellationToken? ct = null)
    {
        var token = ct ?? CancellationToken;
        await Task.Delay(delay, token);
    }

    /// <summary>
    /// Reset cancellation token for reuse
    /// 重置取消令牌以便重用
    /// </summary>
    protected void ResetCancellation()
    {
        _cts.TryReset();
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
/// Options for interruptible agent execution
/// 可中断 Agent 执行选项
/// </summary>
public class InterruptibleExecutionOptions
{
    /// <summary>
    /// Timeout for operation
    /// 操作超时
    /// </summary>
    public TimeSpan? Timeout { get; init; }

    /// <summary>
    /// Whether to auto-save state on interruption
    /// 中断时是否自动保存状态
    /// </summary>
    public bool AutoSaveState { get; init; } = true;

    /// <summary>
    /// Progress reporting interval
    /// 进度报告间隔
    /// </summary>
    public TimeSpan ProgressInterval { get; init; } = TimeSpan.FromSeconds(1);

    /// <summary>
    /// Maximum number of retries on interruption
    /// 中断时最大重试次数
    /// </summary>
    public int MaxRetries { get; init; } = 0;
}
