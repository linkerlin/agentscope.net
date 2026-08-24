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
using AgentScope.Core.Message;

namespace AgentScope.Core.Pipeline;

/// <summary>
/// Pipeline execution context.
/// Holds shared state and metadata during pipeline execution.
/// Pipeline 执行上下文，保存 Pipeline 执行期间的共享状态和元数据。
/// Corresponds to Java: io.agentscope.core.pipeline.PipelineContext
/// </summary>
public class PipelineContext
{
    /// <summary>
    /// Shared state dictionary for passing data between nodes.
    /// 节点间传递数据的共享状态字典。
    /// </summary>
    public Dictionary<string, object> State { get; set; } = new();

    /// <summary>
    /// Execution metadata (timing, node info, etc.).
    /// 执行元数据（计时、节点信息等）。
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();

    /// <summary>
    /// Current execution depth (used for nested pipelines).
    /// 当前执行深度（用于嵌套 Pipeline）。
    /// </summary>
    public int Depth { get; set; }

    /// <summary>
    /// Maximum allowed execution depth.
    /// 允许的最大执行深度。
    /// </summary>
    public int MaxDepth { get; set; } = 10;

    /// <summary>
    /// Cancellation token for pipeline execution.
    /// Pipeline 执行的取消令牌。
    /// </summary>
    public CancellationToken CancellationToken { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the pipeline should stop.
    /// 获取或设置一个值，指示 Pipeline 是否应停止。
    /// </summary>
    public bool IsStopped { get; set; }

    /// <summary>
    /// Gets or sets the reason why the pipeline stopped (if stopped).
    /// 获取或设置 Pipeline 停止的原因（如果已停止）。
    /// </summary>
    public string? StopReason { get; set; }

    /// <summary>
    /// Creates a child context for nested pipeline execution.
    /// 为嵌套 Pipeline 执行创建子上下文。
    /// </summary>
    /// <returns>A new PipelineContext with shared state and incremented depth.
    /// 返回共享状态且深度递增的新 PipelineContext。</returns>
    public PipelineContext CreateChildContext()
    {
        return new PipelineContext
        {
            State = State, // Share state with parent / 与父级共享状态
            Metadata = new Dictionary<string, object>(Metadata),
            Depth = Depth + 1,
            MaxDepth = MaxDepth,
            CancellationToken = CancellationToken
        };
    }

    /// <summary>
    /// Gets a typed value from the state dictionary.
    /// 从状态字典获取类型化值。
    /// </summary>
    /// <typeparam name="T">The expected type of the value. / 值的预期类型。</typeparam>
    /// <param name="key">The key to look up. / 要查找的键。</param>
    /// <returns>The typed value if found, otherwise default(T). / 如果找到则返回类型化值，否则返回 default(T)。</returns>
    public T? GetValue<T>(string key)
    {
        if (State.TryGetValue(key, out var value) && value is T typedValue)
        {
            return typedValue;
        }
        return default;
    }

    /// <summary>
    /// Sets a value in the state dictionary.
    /// 在状态字典中设置值。
    /// </summary>
    /// <typeparam name="T">The type of the value. / 值的类型。</typeparam>
    /// <param name="key">The key to store the value under. / 存储值的键。</param>
    /// <param name="value">The value to store. / 要存储的值。</param>
    public void SetValue<T>(string key, T value)
    {
        State[key] = value!;
    }
}

/// <summary>
/// Pipeline node execution result.
/// Pipeline 节点执行结果，包含成功/失败状态、输出消息和元数据。
/// Corresponds to Java: io.agentscope.core.pipeline.PipelineResult
/// </summary>
public class PipelineResult
{
    /// <summary>
    /// Whether the node executed successfully.
    /// 节点是否执行成功。
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// The output message from the node.
    /// 节点的输出消息。
    /// </summary>
    public Msg? Output { get; set; }

    /// <summary>
    /// Error message if execution failed.
    /// 执行失败时的错误消息。
    /// </summary>
    public string? Error { get; set; }

    /// <summary>
    /// Whether to stop the pipeline after this node.
    /// 此节点后是否停止 Pipeline。
    /// </summary>
    public bool StopPipeline { get; set; }

    /// <summary>
    /// Additional metadata for the execution.
    /// 执行的附加元数据。
    /// </summary>
    public Dictionary<string, object> Metadata { get; } = new();

    /// <summary>
    /// Creates a success result.
    /// 创建成功结果。
    /// </summary>
    /// <param name="output">Optional output message. / 可选的输出消息。</param>
    /// <returns>A successful PipelineResult. / 成功的 PipelineResult。</returns>
    public static PipelineResult SuccessResult(Msg? output = null)
    {
        return new PipelineResult { Success = true, Output = output };
    }

    /// <summary>
    /// Creates a failure result.
    /// 创建失败结果。
    /// </summary>
    /// <param name="error">Error message describing the failure. / 描述失败的错误消息。</param>
    /// <returns>A failed PipelineResult. / 失败的 PipelineResult。</returns>
    public static PipelineResult FailureResult(string error)
    {
        return new PipelineResult { Success = false, Error = error };
    }

    /// <summary>
    /// Creates a result that stops the pipeline.
    /// 创建停止 Pipeline 的结果。
    /// </summary>
    /// <param name="output">Optional output message. / 可选的输出消息。</param>
    /// <param name="reason">Optional reason for stopping. / 可选的停止原因。</param>
    /// <returns>A PipelineResult with StopPipeline set to true. / 设置 StopPipeline 为 true 的 PipelineResult。</returns>
    public static PipelineResult StopResult(Msg? output = null, string? reason = null)
    {
        return new PipelineResult 
        { 
            Success = true, 
            Output = output, 
            StopPipeline = true 
        };
    }
}

/// <summary>
/// Interface for pipeline nodes.
/// Pipeline 节点接口，定义节点的名称和执行方法。
/// Corresponds to Java: io.agentscope.core.pipeline.PipelineNode
/// </summary>
public interface IPipelineNode
{
    /// <summary>
    /// Gets the name of this node.
    /// 获取节点名称。
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Executes the node with the given input and context.
    /// 使用给定输入和上下文执行节点。
    /// </summary>
    /// <param name="input">The input message to process. / 要处理的输入消息。</param>
    /// <param name="context">The pipeline execution context. / Pipeline 执行上下文。</param>
    /// <returns>A PipelineResult containing the execution outcome. / 包含执行结果的 PipelineResult。</returns>
    Task<PipelineResult> ExecuteAsync(Msg input, PipelineContext context);
}

/// <summary>
/// Base class for pipeline nodes.
/// Pipeline 节点基类，提供名称属性和上下文验证功能。
/// Corresponds to Java: io.agentscope.core.pipeline.PipelineNodeBase
/// </summary>
public abstract class PipelineNodeBase : IPipelineNode
{
    /// <summary>
    /// Gets the name of this node.
    /// 获取节点名称。
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Initializes a new instance of PipelineNodeBase with the specified name.
    /// 使用指定名称初始化 PipelineNodeBase 的新实例。
    /// </summary>
    /// <param name="name">The node name. / 节点名称。</param>
    /// <exception cref="ArgumentNullException">Thrown when name is null. / 当名称为 null 时抛出。</exception>
    protected PipelineNodeBase(string name)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
    }

    /// <summary>
    /// Executes the node with the given input and context.
    /// 使用给定输入和上下文执行节点。
    /// </summary>
    /// <param name="input">The input message to process. / 要处理的输入消息。</param>
    /// <param name="context">The pipeline execution context. / Pipeline 执行上下文。</param>
    /// <returns>A PipelineResult containing the execution outcome. / 包含执行结果的 PipelineResult。</returns>
    public abstract Task<PipelineResult> ExecuteAsync(Msg input, PipelineContext context);

    /// <summary>
    /// Validates that the context is usable for execution.
    /// 验证上下文是否可用于执行。
    /// </summary>
    /// <param name="context">The pipeline execution context to validate. / 要验证的 Pipeline 执行上下文。</param>
    /// <exception cref="PipelineException">Thrown when max depth is exceeded. / 当超过最大深度时抛出。</exception>
    /// <exception cref="OperationCanceledException">Thrown when cancellation is requested. / 当请求取消时抛出。</exception>
    protected virtual void ValidateContext(PipelineContext context)
    {
        if (context.Depth > context.MaxDepth)
        {
            throw new PipelineException($"Exceeded max pipeline depth ({context.MaxDepth}) / 超过最大 Pipeline 深度 ({context.MaxDepth})");
        }

        if (context.CancellationToken.IsCancellationRequested)
        {
            throw new OperationCanceledException("Pipeline execution was cancelled / Pipeline 执行已取消");
        }
    }
}

/// <summary>
/// Exception thrown for pipeline-related errors.
/// Pipeline 异常，用于 Pipeline 执行过程中的错误。
/// Corresponds to Java: io.agentscope.core.pipeline.PipelineException
/// </summary>
public class PipelineException : System.Exception
{
    /// <summary>
    /// Initializes a new instance of PipelineException with a message.
    /// 使用消息初始化 PipelineException 的新实例。
    /// </summary>
    /// <param name="message">The error message. / 错误消息。</param>
    public PipelineException(string message) : base(message) { }

    /// <summary>
    /// Initializes a new instance of PipelineException with a message and inner exception.
    /// 使用消息和内部异常初始化 PipelineException 的新实例。
    /// </summary>
    /// <param name="message">The error message. / 错误消息。</param>
    /// <param name="innerException">The inner exception. / 内部异常。</param>
    public PipelineException(string message, System.Exception innerException) : base(message, innerException) { }
}
