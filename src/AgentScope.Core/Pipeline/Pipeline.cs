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
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using AgentScope.Core.Message;

namespace AgentScope.Core.Pipeline;

/// <summary>
/// Pipeline execution engine.
/// Executes a pipeline of nodes with proper context management.
/// Pipeline 执行引擎 - 执行节点管道，管理执行上下文。
/// Corresponds to Java: io.agentscope.core.pipeline.Pipeline
/// </summary>
public class Pipeline
{
    /// <summary>
    /// The root node of the pipeline tree.
    /// 管道树的根节点。
    /// </summary>
    private readonly IPipelineNode _rootNode;

    /// <summary>
    /// The pipeline execution options.
    /// Pipeline 执行选项。
    /// </summary>
    private readonly PipelineOptions _options;

    /// <summary>
    /// Initializes a new instance of Pipeline with the specified root node and options.
    /// 使用指定的根节点和选项初始化 Pipeline 的新实例。
    /// </summary>
    /// <param name="rootNode">The root pipeline node. / 根管道节点。</param>
    /// <param name="options">Optional pipeline options. / 可选的管道选项。</param>
    /// <exception cref="ArgumentNullException">Thrown when rootNode is null. / 当 rootNode 为 null 时抛出。</exception>
    public Pipeline(IPipelineNode rootNode, PipelineOptions? options = null)
    {
        _rootNode = rootNode ?? throw new ArgumentNullException(nameof(rootNode));
        _options = options ?? new PipelineOptions();
    }

    /// <summary>
    /// Executes the pipeline with the given input message.
    /// 使用给定的输入消息执行 Pipeline。
    /// </summary>
    /// <param name="input">The input message. / 输入消息。</param>
    /// <param name="cancellationToken">Optional cancellation token. / 可选的取消令牌。</param>
    /// <returns>The pipeline execution result with metadata. / 包含元数据的管道执行结果。</returns>
    public async Task<PipelineResult> ExecuteAsync(Msg input, CancellationToken cancellationToken = default)
    {
        var context = new PipelineContext
        {
            MaxDepth = _options.MaxDepth,
            CancellationToken = cancellationToken
        };

        var stopwatch = Stopwatch.StartNew();

        try
        {
            var result = await _rootNode.ExecuteAsync(input, context);
            
            stopwatch.Stop();
            
            // Add execution metadata / 添加执行元数据
            result.Metadata["executionTimeMs"] = stopwatch.ElapsedMilliseconds;
            result.Metadata["totalNodes"] = context.Metadata.TryGetValue("nodeCount", out var count) ? count : 0;
            
            return result;
        }
        catch (OperationCanceledException)
        {
            return PipelineResult.FailureResult("Pipeline execution cancelled / Pipeline 执行已取消");
        }
        catch (System.Exception ex)
        {
            return PipelineResult.FailureResult($"Pipeline execution failed / Pipeline 执行失败：{ex.Message}");
        }
    }

    /// <summary>
    /// Executes the pipeline with a simple text input.
    /// 使用简单的文本输入执行 Pipeline。
    /// </summary>
    /// <param name="text">The input text. / 输入文本。</param>
    /// <param name="cancellationToken">Optional cancellation token. / 可选的取消令牌。</param>
    /// <returns>The pipeline execution result. / 管道执行结果。</returns>
    public async Task<PipelineResult> ExecuteAsync(string text, CancellationToken cancellationToken = default)
    {
        var input = Msg.Builder()
            .Role("user")
            .TextContent(text)
            .Build();
        
        return await ExecuteAsync(input, cancellationToken);
    }
}

/// <summary>
/// Pipeline execution options.
/// Pipeline 执行选项。
/// Corresponds to Java: io.agentscope.core.pipeline.PipelineOptions
/// </summary>
public class PipelineOptions
{
    /// <summary>
    /// Maximum execution depth for nested pipelines.
    /// 嵌套 Pipeline 的最大执行深度。
    /// </summary>
    public int MaxDepth { get; set; } = 10;

    /// <summary>
    /// Whether to continue execution when a node fails.
    /// 节点失败时是否继续执行。
    /// </summary>
    public bool ContinueOnError { get; set; } = false;

    /// <summary>
    /// Optional timeout for the entire pipeline execution.
    /// 整个 Pipeline 执行的可选超时时间。
    /// </summary>
    public TimeSpan? Timeout { get; set; }
}

/// <summary>
/// Pipeline builder for fluent configuration.
/// Pipeline 构建器 - 用于流式配置和构建 Pipeline。
/// Corresponds to Java: io.agentscope.core.pipeline.PipelineBuilder
/// </summary>
public class PipelineBuilder
{
    /// <summary>
    /// The root node of the pipeline being built.
    /// 正在构建的管道的根节点。
    /// </summary>
    private IPipelineNode? _rootNode;

    /// <summary>
    /// The pipeline execution options.
    /// Pipeline 执行选项。
    /// </summary>
    private PipelineOptions _options = new();

    /// <summary>
    /// Sets the root node of the pipeline.
    /// 设置 Pipeline 的根节点。
    /// </summary>
    /// <param name="node">The root pipeline node. / 根管道节点。</param>
    /// <returns>The builder instance for chaining. / 用于链式调用的构建器实例。</returns>
    public PipelineBuilder Root(IPipelineNode node)
    {
        _rootNode = node;
        return this;
    }

    /// <summary>
    /// Creates a sequential pipeline from multiple nodes.
    /// 从多个节点创建顺序 Pipeline。
    /// </summary>
    /// <param name="nodes">The nodes to execute sequentially. / 要顺序执行的节点。</param>
    /// <returns>The builder instance for chaining. / 用于链式调用的构建器实例。</returns>
    public PipelineBuilder Sequential(params IPipelineNode[] nodes)
    {
        _rootNode = new SequentialPipelineNode("sequential", nodes);
        return this;
    }

    /// <summary>
    /// Creates a named sequential pipeline from multiple nodes.
    /// 创建带名称的顺序 Pipeline。
    /// </summary>
    /// <param name="name">The name of the sequential node. / 顺序节点的名称。</param>
    /// <param name="nodes">The nodes to execute sequentially. / 要顺序执行的节点。</param>
    /// <returns>The builder instance for chaining. / 用于链式调用的构建器实例。</returns>
    public PipelineBuilder Sequential(string name, params IPipelineNode[] nodes)
    {
        _rootNode = new SequentialPipelineNode(name, nodes);
        return this;
    }

    /// <summary>
    /// Adds a node to the pipeline, wrapping in a sequential node if needed.
    /// 添加节点到 Pipeline，如需要则包装为顺序节点。
    /// </summary>
    /// <param name="node">The node to add. / 要添加的节点。</param>
    /// <returns>The builder instance for chaining. / 用于链式调用的构建器实例。</returns>
    private PipelineBuilder AddNode(IPipelineNode node)
    {
        if (_rootNode == null)
        {
            _rootNode = node;
        }
        else
        {
            _rootNode = new SequentialPipelineNode("pipeline", _rootNode, node);
        }
        return this;
    }

    /// <summary>
    /// Adds an agent-wrapping node to the pipeline.
    /// 添加包装 Agent 的节点到 Pipeline。
    /// </summary>
    /// <param name="agent">The agent to wrap. / 要包装的 Agent。</param>
    /// <param name="name">Optional custom name for the node. / 节点的可选自定义名称。</param>
    /// <returns>The builder instance for chaining. / 用于链式调用的构建器实例。</returns>
    public PipelineBuilder Agent(Agent.IAgent agent, string? name = null)
    {
        var node = new AgentPipelineNode(name ?? agent.Name, agent);
        return AddNode(node);
    }

    /// <summary>
    /// Adds a conditional branch to the pipeline.
    /// 添加条件分支到 Pipeline。
    /// </summary>
    /// <param name="condition">The condition function based on pipeline context. / 基于管道上下文的条件函数。</param>
    /// <param name="thenNode">The node to execute when condition is true. / 条件为真时执行的节点。</param>
    /// <param name="elseNode">Optional node to execute when condition is false. / 条件为假时可选执行的节点。</param>
    /// <returns>The builder instance for chaining. / 用于链式调用的构建器实例。</returns>
    public PipelineBuilder If(Func<PipelineContext, bool> condition, IPipelineNode thenNode, IPipelineNode? elseNode = null)
    {
        var ifNode = new IfElsePipelineNode("if", condition, thenNode, elseNode);
        return AddNode(ifNode);
    }

    /// <summary>
    /// Adds a loop to the pipeline.
    /// 添加循环到 Pipeline。
    /// </summary>
    /// <param name="condition">The loop continuation condition. / 循环继续条件。</param>
    /// <param name="bodyNode">The node to execute in the loop body. / 循环体中执行的节点。</param>
    /// <param name="maxIterations">Maximum number of iterations. / 最大迭代次数。</param>
    /// <returns>The builder instance for chaining. / 用于链式调用的构建器实例。</returns>
    public PipelineBuilder Loop(Func<PipelineContext, bool> condition, IPipelineNode bodyNode, int maxIterations = 100)
    {
        var loopNode = new LoopPipelineNode("loop", condition, bodyNode, maxIterations);
        return AddNode(loopNode);
    }

    /// <summary>
    /// Adds a parallel execution node to the pipeline.
    /// 添加并行执行节点到 Pipeline。
    /// </summary>
    /// <param name="nodes">The nodes to execute in parallel. / 要并行执行的节点。</param>
    /// <returns>The builder instance for chaining. / 用于链式调用的构建器实例。</returns>
    public PipelineBuilder Parallel(params IPipelineNode[] nodes)
    {
        var parallelNode = new ParallelPipelineNode("parallel", nodes);
        return AddNode(parallelNode);
    }

    /// <summary>
    /// Adds a transform node to modify messages.
    /// 添加转换节点以修改消息。
    /// </summary>
    /// <param name="transform">The transform function. / 转换函数。</param>
    /// <param name="name">Optional custom name for the node. / 节点的可选自定义名称。</param>
    /// <returns>The builder instance for chaining. / 用于链式调用的构建器实例。</returns>
    public PipelineBuilder Transform(Func<Msg, Msg> transform, string? name = null)
    {
        var transformNode = new TransformPipelineNode(name ?? "transform", transform);
        return AddNode(transformNode);
    }

    /// <summary>
    /// Adds a custom node to the pipeline.
    /// 添加自定义节点到 Pipeline。
    /// </summary>
    /// <param name="node">The custom pipeline node. / 自定义管道节点。</param>
    /// <returns>The builder instance for chaining. / 用于链式调用的构建器实例。</returns>
    public PipelineBuilder Add(IPipelineNode node)
    {
        return AddNode(node);
    }

    /// <summary>
    /// Sets the maximum execution depth.
    /// 设置最大执行深度。
    /// </summary>
    /// <param name="maxDepth">The maximum depth. / 最大深度。</param>
    /// <returns>The builder instance for chaining. / 用于链式调用的构建器实例。</returns>
    public PipelineBuilder WithMaxDepth(int maxDepth)
    {
        _options.MaxDepth = maxDepth;
        return this;
    }

    /// <summary>
    /// Sets whether to continue execution on error.
    /// 设置是否在出错时继续执行。
    /// </summary>
    /// <param name="continueOnError">Whether to continue on error. / 是否在出错时继续。</param>
    /// <returns>The builder instance for chaining. / 用于链式调用的构建器实例。</returns>
    public PipelineBuilder ContinueOnError(bool continueOnError = true)
    {
        _options.ContinueOnError = continueOnError;
        return this;
    }

    /// <summary>
    /// Sets the execution timeout.
    /// 设置执行超时时间。
    /// </summary>
    /// <param name="timeout">The timeout duration. / 超时时间。</param>
    /// <returns>The builder instance for chaining. / 用于链式调用的构建器实例。</returns>
    public PipelineBuilder WithTimeout(TimeSpan timeout)
    {
        _options.Timeout = timeout;
        return this;
    }

    /// <summary>
    /// Builds the pipeline.
    /// 构建 Pipeline。
    /// </summary>
    /// <returns>The constructed Pipeline instance. / 构建完成的 Pipeline 实例。</returns>
    /// <exception cref="InvalidOperationException">Thrown when no root node is set. / 当未设置根节点时抛出。</exception>
    public Pipeline Build()
    {
        if (_rootNode == null)
        {
            throw new InvalidOperationException("Pipeline must have at least one node / Pipeline 必须至少有一个节点");
        }

        return new Pipeline(_rootNode, _options);
    }

    /// <summary>
    /// Creates a new builder instance.
    /// 创建新的构建器实例。
    /// </summary>
    /// <returns>A new PipelineBuilder instance. / 新的 PipelineBuilder 实例。</returns>
    public static PipelineBuilder Create() => new();
}
