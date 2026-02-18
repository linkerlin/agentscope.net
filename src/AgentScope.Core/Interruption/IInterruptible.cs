// Copyright 2024-2026 the original author or authors.
// Licensed under the Apache License, Version 2.0

namespace AgentScope.Core.Interruption;

/// <summary>
/// Interruption reason
/// 中断原因
/// </summary>
public enum InterruptionReason
{
    /// <summary>
    /// User requested cancellation
    /// 用户请求取消
    /// </summary>
    UserCancelled,

    /// <summary>
    /// Timeout reached
    /// 超时
    /// </summary>
    Timeout,

    /// <summary>
    /// Error occurred
    /// 发生错误
    /// </summary>
    Error,

    /// <summary>
    /// External signal received
    /// 收到外部信号
    /// </summary>
    ExternalSignal,

    /// <summary>
    /// Resource limit reached
    /// 达到资源限制
    /// </summary>
    ResourceLimit,

    /// <summary>
    /// Operation completed successfully
    /// 操作成功完成
    /// </summary>
    Completed
}

/// <summary>
/// Interruption context
/// 中断上下文
/// </summary>
public class InterruptionContext
{
    /// <summary>
    /// Interruption reason
    /// 中断原因
    /// </summary>
    public required InterruptionReason Reason { get; init; }

    /// <summary>
    /// Timestamp when interruption was requested
    /// 中断请求时间戳
    /// </summary>
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Optional message
    /// 可选消息
    /// </summary>
    public string? Message { get; init; }

    /// <summary>
    /// Exception if interruption was due to error
    /// 如果中断是由于错误，则包含异常
    /// </summary>
    public global::System.Exception? Exception { get; init; }

    /// <summary>
    /// Source of interruption
    /// 中断来源
    /// </summary>
    public string? Source { get; init; }

    /// <summary>
    /// Whether to preserve state for resumption
    /// 是否保留状态以便恢复
    /// </summary>
    public bool PreserveState { get; init; } = true;

    /// <summary>
    /// Custom data for interruption handling
    /// 用于中断处理的自定义数据
    /// </summary>
    public Dictionary<string, object> Data { get; init; } = new();
}

/// <summary>
/// Interface for interruptible operations
/// 可中断操作的接口
/// 
/// 参考: agentscope-java 的 Interruption 概念
/// </summary>
public interface IInterruptible
{
    /// <summary>
    /// Whether the operation is currently running
    /// 操作是否正在运行
    /// </summary>
    bool IsRunning { get; }

    /// <summary>
    /// Whether interruption has been requested
    /// 是否已请求中断
    /// </summary>
    bool IsCancellationRequested { get; }

    /// <summary>
    /// Cancellation token for the operation
    /// 操作的取消令牌
    /// </summary>
    CancellationToken CancellationToken { get; }

    /// <summary>
    /// Request interruption of the operation
    /// 请求中断操作
    /// </summary>
    Task InterruptAsync(InterruptionContext context);

    /// <summary>
    /// Event raised when interruption is requested
    /// 请求中断时触发的事件
    /// </summary>
    event EventHandler<InterruptionContext>? InterruptionRequested;

    /// <summary>
    /// Event raised when operation is safely interrupted
    /// 操作安全中断时触发的事件
    /// </summary>
    event EventHandler<InterruptionContext>? Interrupted;
}

/// <summary>
/// Interface for resumable operations
/// 可恢复操作的接口
/// </summary>
public interface IResumable : IInterruptible
{
    /// <summary>
    /// Whether the operation can be resumed
    /// 操作是否可以恢复
    /// </summary>
    bool CanResume { get; }

    /// <summary>
    /// Get current state for resumption
    /// 获取当前状态以便恢复
    /// </summary>
    Task<InterruptionState> CaptureStateAsync();

    /// <summary>
    /// Resume operation from saved state
    /// 从保存的状态恢复操作
    /// </summary>
    Task ResumeAsync(InterruptionState state);
}

/// <summary>
/// Saved state for operation resumption
/// 操作恢复保存的状态
/// </summary>
public class InterruptionState
{
    /// <summary>
    /// State ID
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// State version
    /// </summary>
    public string Version { get; init; } = "1.0";

    /// <summary>
    /// Timestamp when state was captured
    /// 状态捕获时间戳
    /// </summary>
    public DateTime CapturedAt { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// State data
    /// 状态数据
    /// </summary>
    public Dictionary<string, object> Data { get; init; } = new();

    /// <summary>
    /// Type of the operation
    /// 操作类型
    /// </summary>
    public required string OperationType { get; init; }

    /// <summary>
    /// Progress percentage (0-100)
    /// 进度百分比 (0-100)
    /// </summary>
    public double Progress { get; init; }
}

/// <summary>
/// Progress information for long-running operations
/// 长时间运行操作的进度信息
/// </summary>
public class OperationProgress
{
    /// <summary>
    /// Progress percentage (0-100)
    /// 进度百分比 (0-100)
    /// </summary>
    public double Percentage { get; init; }

    /// <summary>
    /// Current step description
    /// 当前步骤描述
    /// </summary>
    public string? CurrentStep { get; init; }

    /// <summary>
    /// Estimated time remaining
    /// 预计剩余时间
    /// </summary>
    public TimeSpan? EstimatedTimeRemaining { get; init; }

    /// <summary>
    /// Items processed
    /// 已处理项目数
    /// </summary>
    public long ItemsProcessed { get; init; }

    /// <summary>
    /// Total items to process
    /// 总项目数
    /// </summary>
    public long? TotalItems { get; init; }
}
