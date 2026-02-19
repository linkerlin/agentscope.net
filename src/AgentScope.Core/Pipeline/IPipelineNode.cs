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
/// Pipeline 执行上下文
/// </summary>
public class PipelineContext
{
/// <summary>
    /// 节点间传递数据的共享状态字典。
    /// </summary>
    public Dictionary<string, object> State { get; set; } = new();

/// <summary>
    /// 执行元数据（计时、节点信息等）。
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();

/// <summary>
    /// 当前执行深度（用于嵌套 Pipeline）。
    /// </summary>
    public int Depth { get; set; }

/// <summary>
    /// 允许的最大执行深度。
    /// </summary>
    public int MaxDepth { get; set; } = 10;

/// <summary>
    /// Pipeline 执行的取消令牌。
    /// </summary>
    public CancellationToken CancellationToken { get; set; }

/// <summary>
    /// 获取或设置一个值，指示 Pipeline 是否应停止。
    /// </summary>
    public bool IsStopped { get; set; }

/// <summary>
    /// 获取或设置 Pipeline 停止的原因（如果已停止）。
    /// </summary>
    public string? StopReason { get; set; }

/// <summary>
    /// 为嵌套 Pipeline 执行创建子上下文。
    /// </summary>
    public PipelineContext CreateChildContext()
    {
        return new PipelineContext
        {
            State = State, // 与父级共享状态
            Metadata = new Dictionary<string, object>(Metadata),
            Depth = Depth + 1,
            MaxDepth = MaxDepth,
            CancellationToken = CancellationToken
        };
    }

/// <summary>
    /// 从状态字典获取值。
    /// </summary>
    public T? GetValue<T>(string key)
    {
        if (State.TryGetValue(key, out var value) && value is T typedValue)
        {
            return typedValue;
        }
        return default;
    }

/// <summary>
    /// 在状态字典中设置值。
    /// </summary>
    public void SetValue<T>(string key, T value)
    {
        State[key] = value!;
    }
}

/// <summary>
/// Pipeline node execution result.
/// Pipeline 节点执行结果
/// </summary>
public class PipelineResult
{
/// <summary>
    /// 节点是否执行成功。
    /// </summary>
    public bool Success { get; set; }

/// <summary>
    /// 节点的输出消息。
    /// </summary>
    public Msg? Output { get; set; }

/// <summary>
    /// 执行失败时的错误消息。
    /// </summary>
    public string? Error { get; set; }

/// <summary>
    /// 此节点后是否停止 Pipeline。
    /// </summary>
    public bool StopPipeline { get; set; }

/// <summary>
    /// 执行的附加元数据。
    /// </summary>
    public Dictionary<string, object> Metadata { get; } = new();

/// <summary>
    /// 创建成功结果。
    /// </summary>
    public static PipelineResult SuccessResult(Msg? output = null)
    {
        return new PipelineResult { Success = true, Output = output };
    }

/// <summary>
    /// 创建失败结果。
    /// </summary>
    public static PipelineResult FailureResult(string error)
    {
        return new PipelineResult { Success = false, Error = error };
    }

/// <summary>
    /// 创建停止 Pipeline 的结果。
    /// </summary>
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
/// Pipeline 节点接口
/// 
/// Java参考: io.agentscope.core.pipeline.PipelineNode
/// </summary>
public interface IPipelineNode
{
/// <summary>
    /// 节点名称。
    /// </summary>
    string Name { get; }

/// <summary>
    /// 使用给定输入和上下文执行节点。
    /// </summary>
    Task<PipelineResult> ExecuteAsync(Msg input, PipelineContext context);
}

/// <summary>
/// Base class for pipeline nodes.
/// Pipeline 节点基类
/// </summary>
public abstract class PipelineNodeBase : IPipelineNode
{
    public string Name { get; }

    protected PipelineNodeBase(string name)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
    }

    public abstract Task<PipelineResult> ExecuteAsync(Msg input, PipelineContext context);

/// <summary>
    /// 验证上下文是否可用于执行。
    /// </summary>
    protected virtual void ValidateContext(PipelineContext context)
    {
        if (context.Depth > context.MaxDepth)
        {
            throw new PipelineException($"超过最大 Pipeline 深度 ({context.MaxDepth})");
        }

        if (context.CancellationToken.IsCancellationRequested)
        {
            throw new OperationCanceledException("Pipeline 执行已取消");
        }
    }
}

/// <summary>
/// Pipeline 异常。
/// </summary>
public class PipelineException : System.Exception
{
    public PipelineException(string message) : base(message) { }
    public PipelineException(string message, System.Exception innerException) : base(message, innerException) { }
}
