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
    /// Shared state dictionary for passing data between nodes.
/// </summary>
    public Dictionary<string, object> State { get; set; } = new();

    /// <summary>
    /// Execution metadata (timing, node info, etc.).
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();

    /// <summary>
    /// Current execution depth (for nested pipelines).
    /// </summary>
    public int Depth { get; set; }

    /// <summary>
    /// Maximum allowed execution depth.
    /// </summary>
    public int MaxDepth { get; set; } = 10;

    /// <summary>
    /// Cancellation token for the pipeline execution.
    /// </summary>
    public CancellationToken CancellationToken { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the pipeline should stop.
    /// </summary>
    public bool IsStopped { get; set; }

    /// <summary>
    /// Gets or sets the stop reason if the pipeline was stopped.
    /// </summary>
    public string? StopReason { get; set; }

    /// <summary>
    /// Creates a child context for nested pipeline execution.
    /// </summary>
    public PipelineContext CreateChildContext()
    {
        return new PipelineContext
        {
            State = State, // Share state with parent
            Metadata = new Dictionary<string, object>(Metadata),
            Depth = Depth + 1,
            MaxDepth = MaxDepth,
            CancellationToken = CancellationToken
        };
    }

    /// <summary>
    /// Gets a value from the state dictionary.
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
    /// Sets a value in the state dictionary.
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
    /// Whether the node executed successfully.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Output message from the node.
    /// </summary>
    public Msg? Output { get; set; }

    /// <summary>
    /// Error message if execution failed.
    /// </summary>
    public string? Error { get; set; }

    /// <summary>
    /// Whether to stop the pipeline after this node.
    /// </summary>
    public bool StopPipeline { get; set; }

    /// <summary>
    /// Additional metadata from the execution.
    /// </summary>
    public Dictionary<string, object> Metadata { get; } = new();

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    public static PipelineResult SuccessResult(Msg? output = null)
    {
        return new PipelineResult { Success = true, Output = output };
    }

    /// <summary>
    /// Creates a failed result.
    /// </summary>
    public static PipelineResult FailureResult(string error)
    {
        return new PipelineResult { Success = false, Error = error };
    }

    /// <summary>
    /// Creates a result that stops the pipeline.
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
    /// Node name.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Executes the node with the given input and context.
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
    /// Validates that the context is valid for execution.
    /// </summary>
    protected virtual void ValidateContext(PipelineContext context)
    {
        if (context.Depth > context.MaxDepth)
        {
            throw new PipelineException($"Maximum pipeline depth ({context.MaxDepth}) exceeded");
        }

        if (context.CancellationToken.IsCancellationRequested)
        {
            throw new OperationCanceledException("Pipeline execution was cancelled");
        }
    }
}

/// <summary>
/// Pipeline exception.
/// </summary>
public class PipelineException : System.Exception
{
    public PipelineException(string message) : base(message) { }
    public PipelineException(string message, System.Exception innerException) : base(message, innerException) { }
}
