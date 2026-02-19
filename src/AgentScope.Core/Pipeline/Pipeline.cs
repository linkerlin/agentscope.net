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
/// Pipeline 执行引擎
/// 
/// Java参考: io.agentscope.core.pipeline.Pipeline
/// </summary>
public class Pipeline
{
    private readonly IPipelineNode _rootNode;
    private readonly PipelineOptions _options;

/// <summary>
    /// 使用指定根节点创建新 Pipeline。
    /// </summary>
    public Pipeline(IPipelineNode rootNode, PipelineOptions? options = null)
    {
        _rootNode = rootNode ?? throw new ArgumentNullException(nameof(rootNode));
        _options = options ?? new PipelineOptions();
    }

/// <summary>
    /// 使用给定输入消息执行 Pipeline。
    /// </summary>
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
            
            // 添加执行元数据
            result.Metadata["executionTimeMs"] = stopwatch.ElapsedMilliseconds;
            result.Metadata["totalNodes"] = context.Metadata.TryGetValue("nodeCount", out var count) ? count : 0;
            
            return result;
        }
        catch (OperationCanceledException)
        {
            return PipelineResult.FailureResult("Pipeline 执行已取消");
        }
        catch (System.Exception ex)
        {
            return PipelineResult.FailureResult($"Pipeline 执行失败：{ex.Message}");
        }
    }

/// <summary>
    /// 使用简单文本输入执行 Pipeline。
    /// </summary>
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
/// Pipeline 执行选项。
/// </summary>
public class PipelineOptions
{
/// <summary>
    /// 嵌套 Pipeline 的最大执行深度。
    /// </summary>
    public int MaxDepth { get; set; } = 10;

/// <summary>
    /// 节点失败时是否继续执行。
    /// </summary>
    public bool ContinueOnError { get; set; } = false;

/// <summary>
    /// 整个 Pipeline 执行的超时时间。
    /// </summary>
    public TimeSpan? Timeout { get; set; }
}

/// <summary>
/// Pipeline builder for fluent configuration.
/// Pipeline 构建器
/// 
/// Java参考: io.agentscope.core.pipeline.PipelineBuilder
/// </summary>
public class PipelineBuilder
{
    private IPipelineNode? _rootNode;
    private PipelineOptions _options = new();

/// <summary>
    /// 设置 Pipeline 的根节点。
    /// </summary>
    public PipelineBuilder Root(IPipelineNode node)
    {
        _rootNode = node;
        return this;
    }

/// <summary>
    /// 从多个节点创建顺序 Pipeline。
    /// </summary>
    public PipelineBuilder Sequential(params IPipelineNode[] nodes)
    {
        _rootNode = new SequentialPipelineNode("sequential", nodes);
        return this;
    }

/// <summary>
    /// 创建带名称的顺序 Pipeline。
    /// </summary>
    public PipelineBuilder Sequential(string name, params IPipelineNode[] nodes)
    {
        _rootNode = new SequentialPipelineNode(name, nodes);
        return this;
    }

/// <summary>
    /// 添加节点到 Pipeline，如需要则包装为顺序节点。
    /// </summary>
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
    /// 添加包装 Agent 的节点。
    /// </summary>
    public PipelineBuilder Agent(Agent.IAgent agent, string? name = null)
    {
        var node = new AgentPipelineNode(name ?? agent.Name, agent);
        return AddNode(node);
    }

/// <summary>
    /// 添加条件分支到 Pipeline。
    /// </summary>
    public PipelineBuilder If(Func<PipelineContext, bool> condition, IPipelineNode thenNode, IPipelineNode? elseNode = null)
    {
        var ifNode = new IfElsePipelineNode("if", condition, thenNode, elseNode);
        return AddNode(ifNode);
    }

/// <summary>
    /// 添加循环到 Pipeline。
    /// </summary>
    public PipelineBuilder Loop(Func<PipelineContext, bool> condition, IPipelineNode bodyNode, int maxIterations = 100)
    {
        var loopNode = new LoopPipelineNode("loop", condition, bodyNode, maxIterations);
        return AddNode(loopNode);
    }

/// <summary>
    /// 添加并行执行节点。
    /// </summary>
    public PipelineBuilder Parallel(params IPipelineNode[] nodes)
    {
        var parallelNode = new ParallelPipelineNode("parallel", nodes);
        return AddNode(parallelNode);
    }

/// <summary>
    /// 添加转换节点以修改消息。
    /// </summary>
    public PipelineBuilder Transform(Func<Msg, Msg> transform, string? name = null)
    {
        var transformNode = new TransformPipelineNode(name ?? "transform", transform);
        return AddNode(transformNode);
    }

/// <summary>
    /// 添加自定义节点到 Pipeline。
    /// </summary>
    public PipelineBuilder Add(IPipelineNode node)
    {
        return AddNode(node);
    }

/// <summary>
    /// 设置最大执行深度。
    /// </summary>
    public PipelineBuilder WithMaxDepth(int maxDepth)
    {
        _options.MaxDepth = maxDepth;
        return this;
    }

/// <summary>
    /// 设置是否在出错时继续。
    /// </summary>
    public PipelineBuilder ContinueOnError(bool continueOnError = true)
    {
        _options.ContinueOnError = continueOnError;
        return this;
    }

/// <summary>
    /// 设置执行超时时间。
    /// </summary>
    public PipelineBuilder WithTimeout(TimeSpan timeout)
    {
        _options.Timeout = timeout;
        return this;
    }

/// <summary>
    /// 构建 Pipeline。
    /// </summary>
    public Pipeline Build()
    {
        if (_rootNode == null)
        {
            throw new InvalidOperationException("Pipeline 必须至少有一个节点");
        }

        return new Pipeline(_rootNode, _options);
    }

/// <summary>
    /// 创建新构建器。
    /// </summary>
    public static PipelineBuilder Create() => new();
}
