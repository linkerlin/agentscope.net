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
/// Sequential pipeline node - executes children in order.
/// 顺序执行管道节点
/// </summary>
public class SequentialPipelineNode : PipelineNodeBase
{
    private readonly IReadOnlyList<IPipelineNode> _nodes;

    public SequentialPipelineNode(string name, params IPipelineNode[] nodes) : base(name)
    {
        _nodes = nodes?.ToList() ?? throw new ArgumentNullException(nameof(nodes));
        if (_nodes.Count == 0)
        {
            throw new ArgumentException("Sequential pipeline must have at least one node", nameof(nodes));
        }
    }

    public override async Task<PipelineResult> ExecuteAsync(Msg input, PipelineContext context)
    {
        ValidateContext(context);

        Msg currentInput = input;
        PipelineResult? lastResult = null;

        // 在元数据中增加节点计数
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

            // 使用输出作为下一个节点的输入
            if (lastResult.Output != null)
            {
                currentInput = lastResult.Output;
            }
        }

        return lastResult ?? PipelineResult.SuccessResult(currentInput);
    }
}

/// <summary>
/// Parallel pipeline node - executes children concurrently.
/// 并行执行管道节点
/// </summary>
public class ParallelPipelineNode : PipelineNodeBase
{
    private readonly IReadOnlyList<IPipelineNode> _nodes;

    public ParallelPipelineNode(string name, params IPipelineNode[] nodes) : base(name)
    {
        _nodes = nodes?.ToList() ?? throw new ArgumentNullException(nameof(nodes));
        if (_nodes.Count == 0)
        {
            throw new ArgumentException("Parallel pipeline must have at least one node", nameof(nodes));
        }
    }

    public override async Task<PipelineResult> ExecuteAsync(Msg input, PipelineContext context)
    {
        ValidateContext(context);

        // Increment node count
        context.Metadata["nodeCount"] = context.Metadata.TryGetValue("nodeCount", out var count) 
            ? (int)count + _nodes.Count 
            : _nodes.Count;

        // 并行执行所有节点
        var tasks = _nodes.Select(node => node.ExecuteAsync(input, context)).ToArray();
        
        var results = await Task.WhenAll(tasks);

        // 检查失败
        var failures = results.Where(r => !r.Success).ToList();
        if (failures.Any())
        {
return PipelineResult.FailureResult(
                $"并行执行失败：{string.Join(", ", failures.Select(f => f.Error))}");
        }

        // 将输出合并为单个消息
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
/// 条件分支管道节点
/// </summary>
public class IfElsePipelineNode : PipelineNodeBase
{
    private readonly Func<PipelineContext, bool> _condition;
    private readonly IPipelineNode _thenNode;
    private readonly IPipelineNode? _elseNode;

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

    public override async Task<PipelineResult> ExecuteAsync(Msg input, PipelineContext context)
    {
        ValidateContext(context);

        // Increment node count
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
            return PipelineResult.FailureResult($"条件评估失败：{ex.Message}");
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
            // 无 else 分支，直接传递
            return PipelineResult.SuccessResult(input);
        }
    }
}

/// <summary>
/// Loop pipeline node - executes body while condition is true.
/// 循环管道节点
/// </summary>
public class LoopPipelineNode : PipelineNodeBase
{
    private readonly Func<PipelineContext, bool> _condition;
    private readonly IPipelineNode _bodyNode;
    private readonly int _maxIterations;

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
            throw new ArgumentException("Max iterations must be positive", nameof(maxIterations));
        }
    }

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

            // 检查条件
            bool shouldContinue;
            try
            {
                shouldContinue = _condition(context);
            }
            catch (System.Exception ex)
            {
                return PipelineResult.FailureResult($"循环条件评估失败：{ex.Message}");
            }

            if (!shouldContinue)
            {
                break;
            }

            // 执行循环体
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

// 增加节点计数
            context.Metadata["nodeCount"] = context.Metadata.TryGetValue("nodeCount", out var count) 
                ? (int)count + 1 
                : 1;
        }

        if (iteration >= _maxIterations)
        {
            return PipelineResult.FailureResult($"循环超过最大迭代次数 ({_maxIterations})");
        }

        return PipelineResult.SuccessResult(currentInput);
    }
}

/// <summary>
/// Agent pipeline node - wraps an IAgent as a pipeline node.
/// Agent 包装管道节点
/// </summary>
public class AgentPipelineNode : PipelineNodeBase
{
    private readonly IAgent _agent;

    public AgentPipelineNode(string name, IAgent agent) : base(name)
    {
        _agent = agent ?? throw new ArgumentNullException(nameof(agent));
    }

    public override async Task<PipelineResult> ExecuteAsync(Msg input, PipelineContext context)
    {
        ValidateContext(context);

        // Increment node count
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
            return PipelineResult.FailureResult($"Agent 执行失败：{ex.Message}");
        }
    }
}

/// <summary>
/// Transform pipeline node - applies a function to transform the message.
/// 转换管道节点
/// </summary>
public class TransformPipelineNode : PipelineNodeBase
{
    private readonly Func<Msg, Msg> _transform;

    public TransformPipelineNode(string name, Func<Msg, Msg> transform) : base(name)
    {
        _transform = transform ?? throw new ArgumentNullException(nameof(transform));
    }

    public override Task<PipelineResult> ExecuteAsync(Msg input, PipelineContext context)
    {
        ValidateContext(context);

        // Increment node count
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
            return Task.FromResult(PipelineResult.FailureResult($"Transform failed: {ex.Message}"));
        }
    }
}

/// <summary>
/// Action pipeline node - executes an action without modifying the message.
/// 动作管道节点 (副作用操作)
/// </summary>
public class ActionPipelineNode : PipelineNodeBase
{
    private readonly Func<Msg, PipelineContext, Task> _action;

    public ActionPipelineNode(string name, Func<Msg, PipelineContext, Task> action) : base(name)
    {
        _action = action ?? throw new ArgumentNullException(nameof(action));
    }

    public override async Task<PipelineResult> ExecuteAsync(Msg input, PipelineContext context)
    {
        ValidateContext(context);

        // Increment node count
        context.Metadata["nodeCount"] = context.Metadata.TryGetValue("nodeCount", out var count) 
            ? (int)count + 1 
            : 1;

        try
        {
            await _action(input, context);
            return PipelineResult.SuccessResult(input); // 原样传递
        }
        catch (System.Exception ex)
        {
            return PipelineResult.FailureResult($"动作失败：{ex.Message}");
        }
    }
}
