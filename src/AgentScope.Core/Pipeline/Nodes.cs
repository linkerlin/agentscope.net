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
using System.Linq;
using System.Threading.Tasks;
using AgentScope.Core.Agent;
using AgentScope.Core.Message;

namespace AgentScope.Core.Pipeline;

/// <summary>
/// Sequential pipeline node - executes children in order, passing output as input to the next.
/// 顺序执行管道节点 - 按顺序执行子节点，将输出作为下一个节点的输入。
/// Corresponds to Java: io.agentscope.core.pipeline.SequentialPipelineNode
/// </summary>
public class SequentialPipelineNode : PipelineNodeBase
{
    /// <summary>
    /// The child nodes to execute in sequence.
    /// 按顺序执行的子节点列表。
    /// </summary>
    private readonly IReadOnlyList<IPipelineNode> _nodes;

    /// <summary>
    /// Initializes a new instance of SequentialPipelineNode.
    /// 初始化 SequentialPipelineNode 的新实例。
    /// </summary>
    /// <param name="name">The node name. / 节点名称。</param>
    /// <param name="nodes">The child nodes to execute in sequence. / 按顺序执行的子节点。</param>
    /// <exception cref="ArgumentNullException">Thrown when nodes is null. / 当节点为 null 时抛出。</exception>
    /// <exception cref="ArgumentException">Thrown when no nodes provided. / 当未提供节点时抛出。</exception>
    public SequentialPipelineNode(string name, params IPipelineNode[] nodes) : base(name)
    {
        _nodes = nodes?.ToList() ?? throw new ArgumentNullException(nameof(nodes));
        if (_nodes.Count == 0)
        {
            throw new ArgumentException("Sequential pipeline must have at least one node / 顺序管道必须至少有一个节点", nameof(nodes));
        }
    }

    /// <summary>
    /// Executes child nodes sequentially, passing each output as the next input.
    /// 按顺序执行子节点，将每个输出作为下一个输入传递。
    /// </summary>
    /// <param name="input">The input message. / 输入消息。</param>
    /// <param name="context">The pipeline execution context. / Pipeline 执行上下文。</param>
    /// <returns>The result of the last node, or the first failure. / 最后一个节点的结果，或第一个失败的结果。</returns>
    public override async Task<PipelineResult> ExecuteAsync(Msg input, PipelineContext context)
    {
        ValidateContext(context);

        Msg currentInput = input;
        PipelineResult? lastResult = null;

        // Increment node count in metadata / 在元数据中增加节点计数
        context.Metadata["nodeCount"] = context.Metadata.TryGetValue("nodeCount", out var count) 
            ? (int)count + _nodes.Count 
            : _nodes.Count;

        foreach (var node in _nodes)
        {
            if (context.IsStopped || context.CancellationToken.IsCancellationRequested)
            {
                break;
            }

            lastResult = await node.ExecuteAsync(currentInput, context);

            if (!lastResult.Success)
            {
                return lastResult;
            }

            if (lastResult.StopPipeline)
            {
                return lastResult;
            }

            // Use output as input for the next node / 使用输出作为下一个节点的输入
            if (lastResult.Output != null)
            {
                currentInput = lastResult.Output;
            }
        }

        return lastResult ?? PipelineResult.SuccessResult(currentInput);
    }
}

/// <summary>
/// Parallel pipeline node - executes children concurrently and merges outputs.
/// 并行执行管道节点 - 并发执行子节点并合并输出。
/// Corresponds to Java: io.agentscope.core.pipeline.ParallelPipelineNode
/// </summary>
public class ParallelPipelineNode : PipelineNodeBase
{
    /// <summary>
    /// The child nodes to execute in parallel.
    /// 并发执行的子节点列表。
    /// </summary>
    private readonly IReadOnlyList<IPipelineNode> _nodes;

    /// <summary>
    /// Initializes a new instance of ParallelPipelineNode.
    /// 初始化 ParallelPipelineNode 的新实例。
    /// </summary>
    /// <param name="name">The node name. / 节点名称。</param>
    /// <param name="nodes">The child nodes to execute in parallel. / 并发执行的子节点。</param>
    /// <exception cref="ArgumentNullException">Thrown when nodes is null. / 当节点为 null 时抛出。</exception>
    /// <exception cref="ArgumentException">Thrown when no nodes provided. / 当未提供节点时抛出。</exception>
    public ParallelPipelineNode(string name, params IPipelineNode[] nodes) : base(name)
    {
        _nodes = nodes?.ToList() ?? throw new ArgumentNullException(nameof(nodes));
        if (_nodes.Count == 0)
        {
            throw new ArgumentException("Parallel pipeline must have at least one node / 并行管道必须至少有一个节点", nameof(nodes));
        }
    }

    /// <summary>
    /// Executes all child nodes concurrently and merges their outputs.
    /// 并发执行所有子节点并合并输出。
    /// </summary>
    /// <param name="input">The input message to pass to all children. / 传递给所有子节点的输入消息。</param>
    /// <param name="context">The pipeline execution context. / Pipeline 执行上下文。</param>
    /// <returns>A merged result containing combined output from all children. / 包含所有子节点合并输出的结果。</returns>
    public override async Task<PipelineResult> ExecuteAsync(Msg input, PipelineContext context)
    {
        ValidateContext(context);

        // Increment node count in metadata / 在元数据中增加节点计数
        context.Metadata["nodeCount"] = context.Metadata.TryGetValue("nodeCount", out var count) 
            ? (int)count + _nodes.Count 
            : _nodes.Count;

        // Execute all nodes in parallel / 并行执行所有节点
        var tasks = _nodes.Select(node => node.ExecuteAsync(input, context)).ToArray();
        
        var results = await Task.WhenAll(tasks);

        // Check for failures / 检查失败
        var failures = results.Where(r => !r.Success).ToList();
        if (failures.Any())
        {
            return PipelineResult.FailureResult(
                $"Parallel execution failed / 并行执行失败：{string.Join(", ", failures.Select(f => f.Error))}");
        }

        // Merge outputs into a single message / 将输出合并为单个消息
        var outputs = results.Where(r => r.Output != null).Select(r => r.Output!).ToList();
        var combinedContent = string.Join("\n\n", outputs.Select(o => o.GetTextContent()));
        
        var combinedOutput = Msg.Builder()
            .Role("assistant")
            .TextContent(combinedContent)
            .Build();

        return PipelineResult.SuccessResult(combinedOutput);
    }
}

/// <summary>
/// If-Else conditional pipeline node.
/// 条件分支管道节点 - 根据条件选择执行 then 或 else 分支。
/// Corresponds to Java: io.agentscope.core.pipeline.IfElsePipelineNode
/// </summary>
public class IfElsePipelineNode : PipelineNodeBase
{
    /// <summary>
    /// The condition function to evaluate.
    /// 要评估的条件函数。
    /// </summary>
    private readonly Func<PipelineContext, bool> _condition;

    /// <summary>
    /// The node to execute when condition is true.
    /// 条件为 true 时执行的节点。
    /// </summary>
    private readonly IPipelineNode _thenNode;

    /// <summary>
    /// The optional node to execute when condition is false.
    /// 条件为 false 时可选执行的节点。
    /// </summary>
    private readonly IPipelineNode? _elseNode;

    /// <summary>
    /// Initializes a new instance of IfElsePipelineNode.
    /// 初始化 IfElsePipelineNode 的新实例。
    /// </summary>
    /// <param name="name">The node name. / 节点名称。</param>
    /// <param name="condition">The condition function. / 条件函数。</param>
    /// <param name="thenNode">The node to execute when condition is true. / 条件为 true 时执行的节点。</param>
    /// <param name="elseNode">Optional node to execute when condition is false. / 条件为 false 时可选执行的节点。</param>
    /// <exception cref="ArgumentNullException">Thrown when condition or thenNode is null. / 当条件或 thenNode 为 null 时抛出。</exception>
    public IfElsePipelineNode(
        string name, 
        Func<PipelineContext, bool> condition, 
        IPipelineNode thenNode, 
        IPipelineNode? elseNode = null) : base(name)
    {
        _condition = condition ?? throw new ArgumentNullException(nameof(condition));
        _thenNode = thenNode ?? throw new ArgumentNullException(nameof(thenNode));
        _elseNode = elseNode;
    }

    /// <summary>
    /// Evaluates the condition and executes the appropriate branch.
    /// 评估条件并执行相应的分支。
    /// </summary>
    /// <param name="input">The input message. / 输入消息。</param>
    /// <param name="context">The pipeline execution context. / Pipeline 执行上下文。</param>
    /// <returns>The result from the executed branch. / 执行分支的结果。</returns>
    public override async Task<PipelineResult> ExecuteAsync(Msg input, PipelineContext context)
    {
        ValidateContext(context);

        // Increment node count in metadata / 在元数据中增加节点计数
        context.Metadata["nodeCount"] = context.Metadata.TryGetValue("nodeCount", out var count) 
            ? (int)count + 1 
            : 1;

        bool conditionResult;
        try
        {
            conditionResult = _condition(context);
        }
        catch (System.Exception ex)
        {
            return PipelineResult.FailureResult($"Condition evaluation failed / 条件评估失败：{ex.Message}");
        }

        if (conditionResult)
        {
            return await _thenNode.ExecuteAsync(input, context);
        }
        else if (_elseNode != null)
        {
            return await _elseNode.ExecuteAsync(input, context);
        }
        else
        {
            // No else branch, pass through / 无 else 分支，直接传递
            return PipelineResult.SuccessResult(input);
        }
    }
}

/// <summary>
/// Loop pipeline node - executes body while condition is true.
/// 循环管道节点 - 当条件为 true 时重复执行循环体。
/// Corresponds to Java: io.agentscope.core.pipeline.LoopPipelineNode
/// </summary>
public class LoopPipelineNode : PipelineNodeBase
{
    /// <summary>
    /// The condition function to evaluate before each iteration.
    /// 每次迭代前评估的条件函数。
    /// </summary>
    private readonly Func<PipelineContext, bool> _condition;

    /// <summary>
    /// The body node to execute in each iteration.
    /// 每次迭代中执行的循环体节点。
    /// </summary>
    private readonly IPipelineNode _bodyNode;

    /// <summary>
    /// Maximum number of iterations to prevent infinite loops.
    /// 最大迭代次数，防止无限循环。
    /// </summary>
    private readonly int _maxIterations;

    /// <summary>
    /// Initializes a new instance of LoopPipelineNode.
    /// 初始化 LoopPipelineNode 的新实例。
    /// </summary>
    /// <param name="name">The node name. / 节点名称。</param>
    /// <param name="condition">The loop condition function. / 循环条件函数。</param>
    /// <param name="bodyNode">The body node to execute. / 要执行的循环体节点。</param>
    /// <param name="maxIterations">Maximum iterations (default: 100). / 最大迭代次数（默认：100）。</param>
    /// <exception cref="ArgumentNullException">Thrown when condition or bodyNode is null. / 当条件或 bodyNode 为 null 时抛出。</exception>
    /// <exception cref="ArgumentException">Thrown when maxIterations is not positive. / 当 maxIterations 不是正数时抛出。</exception>
    public LoopPipelineNode(
        string name, 
        Func<PipelineContext, bool> condition, 
        IPipelineNode bodyNode,
        int maxIterations = 100) : base(name)
    {
        _condition = condition ?? throw new ArgumentNullException(nameof(condition));
        _bodyNode = bodyNode ?? throw new ArgumentNullException(nameof(bodyNode));
        _maxIterations = maxIterations;

        if (maxIterations <= 0)
        {
            throw new ArgumentException("Max iterations must be positive / 最大迭代次数必须为正数", nameof(maxIterations));
        }
    }

    /// <summary>
    /// Executes the loop body while the condition is true.
    /// 当条件为 true 时重复执行循环体。
    /// </summary>
    /// <param name="input">The initial input message. / 初始输入消息。</param>
    /// <param name="context">The pipeline execution context. / Pipeline 执行上下文。</param>
    /// <returns>The result after loop completion. / 循环完成后的结果。</returns>
    public override async Task<PipelineResult> ExecuteAsync(Msg input, PipelineContext context)
    {
        ValidateContext(context);

        Msg currentInput = input;
        int iteration = 0;

        while (iteration < _maxIterations)
        {
            if (context.IsStopped || context.CancellationToken.IsCancellationRequested)
            {
                break;
            }

            // Check loop condition / 检查循环条件
            bool shouldContinue;
            try
            {
                shouldContinue = _condition(context);
            }
            catch (System.Exception ex)
            {
                return PipelineResult.FailureResult($"Loop condition evaluation failed / 循环条件评估失败：{ex.Message}");
            }

            if (!shouldContinue)
            {
                break;
            }

            // Execute loop body / 执行循环体
            var result = await _bodyNode.ExecuteAsync(currentInput, context);

            if (!result.Success)
            {
                return result;
            }

            if (result.StopPipeline)
            {
                return result;
            }

            if (result.Output != null)
            {
                currentInput = result.Output;
            }

            iteration++;

            // Increment node count in metadata / 在元数据中增加节点计数
            context.Metadata["nodeCount"] = context.Metadata.TryGetValue("nodeCount", out var count) 
                ? (int)count + 1 
                : 1;
        }

        if (iteration >= _maxIterations)
        {
            return PipelineResult.FailureResult($"Loop exceeded max iterations ({_maxIterations}) / 循环超过最大迭代次数 ({_maxIterations})");
        }

        return PipelineResult.SuccessResult(currentInput);
    }
}

/// <summary>
/// Agent pipeline node - wraps an IAgent as a pipeline node.
/// Agent 包装管道节点 - 将 IAgent 包装为管道节点，使其可在 Pipeline 中执行。
/// Corresponds to Java: io.agentscope.core.pipeline.AgentPipelineNode
/// </summary>
public class AgentPipelineNode : PipelineNodeBase
{
    /// <summary>
    /// The wrapped agent instance.
    /// 被包装的 Agent 实例。
    /// </summary>
    private readonly IAgent _agent;

    /// <summary>
    /// Initializes a new instance of AgentPipelineNode.
    /// 初始化 AgentPipelineNode 的新实例。
    /// </summary>
    /// <param name="name">The node name. / 节点名称。</param>
    /// <param name="agent">The agent to wrap. / 要包装的 Agent。</param>
    /// <exception cref="ArgumentNullException">Thrown when agent is null. / 当 agent 为 null 时抛出。</exception>
    public AgentPipelineNode(string name, IAgent agent) : base(name)
    {
        _agent = agent ?? throw new ArgumentNullException(nameof(agent));
    }

    /// <summary>
    /// Executes the wrapped agent with the given input.
    /// 使用给定的输入执行被包装的 Agent。
    /// </summary>
    /// <param name="input">The input message. / 输入消息。</param>
    /// <param name="context">The pipeline execution context. / Pipeline 执行上下文。</param>
    /// <returns>The result from the agent execution. / Agent 执行的结果。</returns>
    public override async Task<PipelineResult> ExecuteAsync(Msg input, PipelineContext context)
    {
        ValidateContext(context);

        // Increment node count in metadata / 在元数据中增加节点计数
        context.Metadata["nodeCount"] = context.Metadata.TryGetValue("nodeCount", out var count) 
            ? (int)count + 1 
            : 1;

        try
        {
            var output = await _agent.CallAsync(input);
            return PipelineResult.SuccessResult(output);
        }
        catch (System.Exception ex)
        {
            return PipelineResult.FailureResult($"Agent execution failed / Agent 执行失败：{ex.Message}");
        }
    }
}

/// <summary>
/// Transform pipeline node - applies a function to transform the message.
/// 转换管道节点 - 应用函数对消息进行转换。
/// Corresponds to Java: io.agentscope.core.pipeline.TransformPipelineNode
/// </summary>
public class TransformPipelineNode : PipelineNodeBase
{
    /// <summary>
    /// The transform function to apply to the message.
    /// 应用于消息的转换函数。
    /// </summary>
    private readonly Func<Msg, Msg> _transform;

    /// <summary>
    /// Initializes a new instance of TransformPipelineNode.
    /// 初始化 TransformPipelineNode 的新实例。
    /// </summary>
    /// <param name="name">The node name. / 节点名称。</param>
    /// <param name="transform">The transform function. / 转换函数。</param>
    /// <exception cref="ArgumentNullException">Thrown when transform is null. / 当 transform 为 null 时抛出。</exception>
    public TransformPipelineNode(string name, Func<Msg, Msg> transform) : base(name)
    {
        _transform = transform ?? throw new ArgumentNullException(nameof(transform));
    }

    /// <summary>
    /// Applies the transform function to the input message.
    /// 对输入消息应用转换函数。
    /// </summary>
    /// <param name="input">The input message to transform. / 要转换的输入消息。</param>
    /// <param name="context">The pipeline execution context. / Pipeline 执行上下文。</param>
    /// <returns>The transformed result. / 转换后的结果。</returns>
    public override Task<PipelineResult> ExecuteAsync(Msg input, PipelineContext context)
    {
        ValidateContext(context);

        // Increment node count in metadata / 在元数据中增加节点计数
        context.Metadata["nodeCount"] = context.Metadata.TryGetValue("nodeCount", out var count) 
            ? (int)count + 1 
            : 1;

        try
        {
            var output = _transform(input);
            return Task.FromResult(PipelineResult.SuccessResult(output));
        }
        catch (System.Exception ex)
        {
            return Task.FromResult(PipelineResult.FailureResult($"Transform failed / 转换失败：{ex.Message}"));
        }
    }
}

/// <summary>
/// Action pipeline node - executes an action without modifying the message.
/// 动作管道节点 - 执行副作用操作，不修改消息内容。
/// Corresponds to Java: io.agentscope.core.pipeline.ActionPipelineNode
/// </summary>
public class ActionPipelineNode : PipelineNodeBase
{
    /// <summary>
    /// The action function to execute (logging, notification, etc.).
    /// 要执行的动作函数（日志记录、通知等）。
    /// </summary>
    private readonly Func<Msg, PipelineContext, Task> _action;

    /// <summary>
    /// Initializes a new instance of ActionPipelineNode.
    /// 初始化 ActionPipelineNode 的新实例。
    /// </summary>
    /// <param name="name">The node name. / 节点名称。</param>
    /// <param name="action">The action function. / 动作函数。</param>
    /// <exception cref="ArgumentNullException">Thrown when action is null. / 当 action 为 null 时抛出。</exception>
    public ActionPipelineNode(string name, Func<Msg, PipelineContext, Task> action) : base(name)
    {
        _action = action ?? throw new ArgumentNullException(nameof(action));
    }

    /// <summary>
    /// Executes the action with the input message, passing it through unchanged.
    /// 使用输入消息执行动作，原样传递消息。
    /// </summary>
    /// <param name="input">The input message. / 输入消息。</param>
    /// <param name="context">The pipeline execution context. / Pipeline 执行上下文。</param>
    /// <returns>A success result with the original input unchanged. / 包含原始输入的成功结果。</returns>
    public override async Task<PipelineResult> ExecuteAsync(Msg input, PipelineContext context)
    {
        ValidateContext(context);

        // Increment node count in metadata / 在元数据中增加节点计数
        context.Metadata["nodeCount"] = context.Metadata.TryGetValue("nodeCount", out var count) 
            ? (int)count + 1 
            : 1;

        try
        {
            await _action(input, context);
            return PipelineResult.SuccessResult(input); // Pass through unchanged / 原样传递
        }
        catch (System.Exception ex)
        {
            return PipelineResult.FailureResult($"Action execution failed / 动作执行失败：{ex.Message}");
        }
    }
}
