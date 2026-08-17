// Copyright 2024-2026 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
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

    /// <inheritdoc />
    public event EventHandler<InterruptionContext>? InterruptionRequested;

    /// <inheritdoc />
    public event EventHandler<InterruptionContext>? Interrupted;

    protected InterruptibleAgentBase(string name, string? description = null) : base(name, description)
    {
    }

    /// <summary>
    /// 重写 CallAsync 以支持中断
    /// </summary>
    public override async Task<Msg> CallAsync(IReadOnlyList<Msg> messages, RuntimeContext? context = null)
    {
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(_cts.Token);
        try
        {
            IsRunning = true;
            return await ExecuteAsync(messages, linkedCts.Token);
        }
        catch (OperationCanceledException) when (_cts.IsCancellationRequested)
        {
            return Msg.Builder()
                .Role("system")
                .Content("操作已被中断 Operation was interrupted")
                .Build();
        }
        finally
        {
            IsRunning = false;
        }
    }

    /// <summary>
    /// 执行 Agent 核心逻辑（带 CancellationToken）
    /// </summary>
    protected abstract Task<Msg> ExecuteAsync(IReadOnlyList<Msg> messages, CancellationToken ct);

    /// <inheritdoc />
    public virtual async Task InterruptAsync(InterruptionContext context)
    {
        if (!IsRunning) return;

        InterruptionRequested?.Invoke(this, context);

        _cts.Cancel();

        var timeout = TimeSpan.FromSeconds(5);
        var startTime = DateTime.UtcNow;

        while (IsRunning && DateTime.UtcNow - startTime < timeout)
        {
            await Task.Delay(50);
        }

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
        CaptureCustomState(state.Data);
        _savedState = state;
        return Task.FromResult(state);
    }

    /// <inheritdoc />
    public virtual async Task ResumeAsync(InterruptionState state)
    {
        if (!CanResume)
        {
            throw new InvalidOperationException("没有可恢复的保存状态 No saved state to resume from");
        }
        _cts.TryReset();
        RestoreCustomState(state.Data);
        await ResumeOperationAsync(state);
    }

    /// <summary>
    /// 获取当前进度 (0-100)
    /// </summary>
    protected virtual double GetCurrentProgress() => 0;

    /// <summary>
    /// 捕获自定义状态数据
    /// </summary>
    protected virtual void CaptureCustomState(Dictionary<string, object> stateData) { }

    /// <summary>
    /// 恢复自定义状态数据
    /// </summary>
    protected virtual void RestoreCustomState(Dictionary<string, object> stateData) { }

    /// <summary>
    /// 从保存的状态恢复操作
    /// </summary>
    protected virtual Task ResumeOperationAsync(InterruptionState state) => Task.CompletedTask;

    /// <summary>
    /// 检查取消并在请求时抛出
    /// </summary>
    protected void CheckCancellation()
    {
        CancellationToken.ThrowIfCancellationRequested();
    }

    /// <summary>
    /// 带取消检查的安全延迟
    /// </summary>
    protected async Task DelayAsync(TimeSpan delay, CancellationToken? ct = null)
    {
        var token = ct ?? CancellationToken;
        await Task.Delay(delay, token);
    }

    /// <summary>
    /// 重置取消令牌以便重用
    /// </summary>
    protected void ResetCancellation()
    {
        _cts.TryReset();
    }

    /// <summary>
    /// 释放资源
    /// </summary>
    public virtual void Dispose()
    {
        _cts.Cancel();
        _cts.Dispose();
    }
}
