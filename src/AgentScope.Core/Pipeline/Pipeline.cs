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
    /// Creates a new pipeline with the specified root node.
    /// </summary>
    public Pipeline(IPipelineNode rootNode, PipelineOptions? options = null)
    {
        _rootNode = rootNode ?? throw new ArgumentNullException(nameof(rootNode));
        _options = options ?? new PipelineOptions();
    }

    /// <summary>
    /// Executes the pipeline with the given input message.
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
            
            // Add execution metadata
            result.Metadata["executionTimeMs"] = stopwatch.ElapsedMilliseconds;
            result.Metadata["totalNodes"] = context.Metadata.TryGetValue("nodeCount", out var count) ? count : 0;
            
            return result;
        }
        catch (OperationCanceledException)
        {
            return PipelineResult.FailureResult("Pipeline execution was cancelled");
        }
        catch (System.Exception ex)
        {
            return PipelineResult.FailureResult($"Pipeline execution failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Executes the pipeline with a simple text input.
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
/// Pipeline execution options.
/// </summary>
public class PipelineOptions
{
    /// <summary>
    /// Maximum execution depth for nested pipelines.
    /// </summary>
    public int MaxDepth { get; set; } = 10;

    /// <summary>
    /// Whether to continue execution on node failure.
    /// </summary>
    public bool ContinueOnError { get; set; } = false;

    /// <summary>
    /// Timeout for the entire pipeline execution.
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
    /// Sets the root node of the pipeline.
    /// </summary>
    public PipelineBuilder Root(IPipelineNode node)
    {
        _rootNode = node;
        return this;
    }

    /// <summary>
    /// Creates a sequential pipeline from multiple nodes.
    /// </summary>
    public PipelineBuilder Sequential(params IPipelineNode[] nodes)
    {
        _rootNode = new SequentialPipelineNode("sequential", nodes);
        return this;
    }

    /// <summary>
    /// Creates a sequential pipeline with a name.
    /// </summary>
    public PipelineBuilder Sequential(string name, params IPipelineNode[] nodes)
    {
        _rootNode = new SequentialPipelineNode(name, nodes);
        return this;
    }

    /// <summary>
    /// Adds a node that wraps an agent.
    /// </summary>
    public PipelineBuilder Agent(Agent.IAgent agent, string? name = null)
    {
        var node = new AgentPipelineNode(name ?? agent.Name, agent);
        
        if (_rootNode == null)
        {
            _rootNode = node;
        }
        else
        {
            // Wrap existing root with sequential node
            _rootNode = new SequentialPipelineNode("pipeline", _rootNode, node);
        }
        
        return this;
    }

    /// <summary>
    /// Adds a conditional branch to the pipeline.
    /// </summary>
    public PipelineBuilder If(Func<PipelineContext, bool> condition, IPipelineNode thenNode, IPipelineNode? elseNode = null)
    {
        var ifNode = new IfElsePipelineNode("if", condition, thenNode, elseNode);
        
        if (_rootNode == null)
        {
            _rootNode = ifNode;
        }
        else
        {
            _rootNode = new SequentialPipelineNode("pipeline", _rootNode, ifNode);
        }
        
        return this;
    }

    /// <summary>
    /// Adds a loop to the pipeline.
    /// </summary>
    public PipelineBuilder Loop(Func<PipelineContext, bool> condition, IPipelineNode bodyNode, int maxIterations = 100)
    {
        var loopNode = new LoopPipelineNode("loop", condition, bodyNode, maxIterations);
        
        if (_rootNode == null)
        {
            _rootNode = loopNode;
        }
        else
        {
            _rootNode = new SequentialPipelineNode("pipeline", _rootNode, loopNode);
        }
        
        return this;
    }

    /// <summary>
    /// Adds a parallel execution node.
    /// </summary>
    public PipelineBuilder Parallel(params IPipelineNode[] nodes)
    {
        var parallelNode = new ParallelPipelineNode("parallel", nodes);
        
        if (_rootNode == null)
        {
            _rootNode = parallelNode;
        }
        else
        {
            _rootNode = new SequentialPipelineNode("pipeline", _rootNode, parallelNode);
        }
        
        return this;
    }

    /// <summary>
    /// Adds a transform node that modifies the message.
    /// </summary>
    public PipelineBuilder Transform(Func<Msg, Msg> transform, string? name = null)
    {
        var transformNode = new TransformPipelineNode(name ?? "transform", transform);
        
        if (_rootNode == null)
        {
            _rootNode = transformNode;
        }
        else
        {
            _rootNode = new SequentialPipelineNode("pipeline", _rootNode, transformNode);
        }
        
        return this;
    }

    /// <summary>
    /// Adds a custom node to the pipeline.
    /// </summary>
    public PipelineBuilder Add(IPipelineNode node)
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
    /// Sets the maximum execution depth.
    /// </summary>
    public PipelineBuilder WithMaxDepth(int maxDepth)
    {
        _options.MaxDepth = maxDepth;
        return this;
    }

    /// <summary>
    /// Sets whether to continue on error.
    /// </summary>
    public PipelineBuilder ContinueOnError(bool continueOnError = true)
    {
        _options.ContinueOnError = continueOnError;
        return this;
    }

    /// <summary>
    /// Sets the execution timeout.
    /// </summary>
    public PipelineBuilder WithTimeout(TimeSpan timeout)
    {
        _options.Timeout = timeout;
        return this;
    }

    /// <summary>
    /// Builds the pipeline.
    /// </summary>
    public Pipeline Build()
    {
        if (_rootNode == null)
        {
            throw new InvalidOperationException("Pipeline must have at least one node");
        }

        return new Pipeline(_rootNode, _options);
    }

    /// <summary>
    /// Creates a new builder.
    /// </summary>
    public static PipelineBuilder Create() => new();
}
