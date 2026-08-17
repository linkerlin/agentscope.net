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
using System.Threading.Tasks;
using AgentScope.Core.Agent;
using AgentScope.Core.Events;
using AgentScope.Core.Message;
using AgentScope.Core.Pipeline;
using Xunit;

// Alias to avoid namespace conflicts
using PipelineEngine = AgentScope.Core.Pipeline.Pipeline;

namespace AgentScope.Core.Tests.Pipeline;

/// <summary>
/// Tests for Pipeline engine, nodes, context, and result types.
/// 管道引擎、节点、上下文和结果类型的测试。
/// </summary>
public class PipelineTests
{
    #region PipelineContext Tests

    /// <summary>
    /// Tests that creating a child context increments the depth counter.
    /// 测试创建子上下文时深度计数器递增。
    /// </summary>
    [Fact]
    public void Context_CreateChildContext_IncrementsDepth()
    {
        var parent = new PipelineContext { Depth = 5, MaxDepth = 10 };
        var child = parent.CreateChildContext();

        Assert.Equal(6, child.Depth);
        Assert.Equal(10, child.MaxDepth);
        Assert.Same(parent.State, child.State); // State should be shared
    }

    /// <summary>
    /// Tests that setting and getting a value from context works correctly.
    /// 测试在上下文中设置和获取值能正确工作。
    /// </summary>
    [Fact]
    public void Context_SetValue_GetValue_ReturnsCorrectValue()
    {
        var context = new PipelineContext();
        context.SetValue("key", "value");

        var result = context.GetValue<string>("key");

        Assert.Equal("value", result);
    }

    /// <summary>
    /// Tests that getting a non-existent key returns the default value (null).
    /// 测试获取不存在的键时返回默认值（null）。
    /// </summary>
    [Fact]
    public void Context_GetValue_NonExistentKey_ReturnsDefault()
    {
        var context = new PipelineContext();

        var result = context.GetValue<string>("nonexistent");

        Assert.Null(result);
    }

    #endregion

    #region PipelineResult Tests

    /// <summary>
    /// Tests that a success result has Success=true and no error message.
    /// 测试成功结果的 Success 为 true 且没有错误消息。
    /// </summary>
    [Fact]
    public void Result_SuccessResult_HasSuccessTrue()
    {
        var result = PipelineResult.SuccessResult();

        Assert.True(result.Success);
        Assert.Null(result.Error);
    }

    /// <summary>
    /// Tests that a failure result has Success=false and contains the error message.
    /// 测试失败结果的 Success 为 false 且包含错误消息。
    /// </summary>
    [Fact]
    public void Result_FailureResult_HasSuccessFalse()
    {
        var result = PipelineResult.FailureResult("error message");

        Assert.False(result.Success);
        Assert.Equal("error message", result.Error);
    }

    /// <summary>
    /// Tests that a stop result has both Success=true and StopPipeline=true.
    /// 测试停止结果的 Success 和 StopPipeline 均为 true。
    /// </summary>
    [Fact]
    public void Result_StopResult_HasStopPipelineTrue()
    {
        var result = PipelineResult.StopResult();

        Assert.True(result.Success);
        Assert.True(result.StopPipeline);
    }

    #endregion

    #region SequentialPipeline Tests

    /// <summary>
    /// Tests that sequential pipeline executes nodes in order, chaining outputs as inputs.
    /// 测试顺序管道按顺序执行节点，将输出作为下一个节点的输入。
    /// </summary>
    [Fact]
    public async Task Sequential_Execute_RunsNodesInOrder()
    {
        var node1 = new TransformPipelineNode("node1", msg => 
            Msg.Builder().Role("assistant").TextContent(msg.GetTextContent() + "-1").Build());
        var node2 = new TransformPipelineNode("node2", msg => 
            Msg.Builder().Role("assistant").TextContent(msg.GetTextContent() + "-2").Build());

        var sequential = new SequentialPipelineNode("sequential", node1, node2);
        var input = Msg.Builder().Role("user").TextContent("start").Build();
        var context = new PipelineContext();

        var result = await sequential.ExecuteAsync(input, context);

        Assert.True(result.Success);
        Assert.Equal("start-1-2", result.Output?.GetTextContent());
    }

    /// <summary>
    /// Tests that sequential pipeline stops execution when a node fails.
    /// 测试顺序管道在节点失败时停止执行。
    /// </summary>
    [Fact]
    public async Task Sequential_Execute_StopsOnFailure()
    {
        var node1 = new TransformPipelineNode("node1", msg => msg);
        var node2 = new FakeFailingNode("node2");
        var node3 = new TransformPipelineNode("node3", msg => 
            Msg.Builder().Role("assistant").TextContent("should-not-reach").Build());

        var sequential = new SequentialPipelineNode("sequential", node1, node2, node3);
        var input = Msg.Builder().Role("user").TextContent("test").Build();
        var context = new PipelineContext();

        var result = await sequential.ExecuteAsync(input, context);

        Assert.False(result.Success);
        Assert.Equal("Node failed", result.Error);
    }

    /// <summary>
    /// Tests that constructing a sequential pipeline with no nodes throws an exception.
    /// 测试使用空节点列表构造顺序管道时抛出异常。
    /// </summary>
    [Fact]
    public void Sequential_Constructor_EmptyNodes_ThrowsException()
    {
        Assert.Throws<ArgumentException>(() => new SequentialPipelineNode("empty"));
    }

    #endregion

    #region ParallelPipeline Tests

    /// <summary>
    /// Tests that parallel pipeline runs all nodes concurrently and combines results.
    /// 测试并行管道并发运行所有节点并合并结果。
    /// </summary>
    [Fact]
    public async Task Parallel_Execute_RunsNodesConcurrently()
    {
        var node1 = new TransformPipelineNode("node1", msg => 
            Msg.Builder().Role("assistant").TextContent("A").Build());
        var node2 = new TransformPipelineNode("node2", msg => 
            Msg.Builder().Role("assistant").TextContent("B").Build());

        var parallel = new ParallelPipelineNode("parallel", node1, node2);
        var input = Msg.Builder().Role("user").TextContent("input").Build();
        var context = new PipelineContext();

        var result = await parallel.ExecuteAsync(input, context);

        Assert.True(result.Success);
        Assert.Contains("A", result.Output?.GetTextContent());
        Assert.Contains("B", result.Output?.GetTextContent());
    }

    /// <summary>
    /// Tests that parallel pipeline returns an error when any node fails.
    /// 测试并行管道在任一节点失败时返回错误。
    /// </summary>
    [Fact]
    public async Task Parallel_Execute_Failure_ReturnsError()
    {
        var node1 = new TransformPipelineNode("node1", msg => msg);
        var node2 = new FakeFailingNode("node2");

        var parallel = new ParallelPipelineNode("parallel", node1, node2);
        var input = Msg.Builder().Role("user").TextContent("test").Build();
        var context = new PipelineContext();

        var result = await parallel.ExecuteAsync(input, context);

        Assert.False(result.Success);
        Assert.Contains("并行执行失败", result.Error);
    }

    #endregion

    #region IfElsePipeline Tests

    /// <summary>
    /// Tests that IfElse executes the then-branch when the condition is true.
    /// 测试 IfElse 在条件为 true 时执行 then 分支。
    /// </summary>
    [Fact]
    public async Task IfElse_ConditionTrue_ExecutesThenBranch()
    {
        var thenNode = new TransformPipelineNode("then", msg => 
            Msg.Builder().Role("assistant").TextContent("then-branch").Build());
        var elseNode = new TransformPipelineNode("else", msg => 
            Msg.Builder().Role("assistant").TextContent("else-branch").Build());

        var ifElse = new IfElsePipelineNode("if", ctx => true, thenNode, elseNode);
        var input = Msg.Builder().Role("user").TextContent("test").Build();
        var context = new PipelineContext();

        var result = await ifElse.ExecuteAsync(input, context);

        Assert.True(result.Success);
        Assert.Equal("then-branch", result.Output?.GetTextContent());
    }

    /// <summary>
    /// Tests that IfElse executes the else-branch when the condition is false.
    /// 测试 IfElse 在条件为 false 时执行 else 分支。
    /// </summary>
    [Fact]
    public async Task IfElse_ConditionFalse_ExecutesElseBranch()
    {
        var thenNode = new TransformPipelineNode("then", msg => 
            Msg.Builder().Role("assistant").TextContent("then-branch").Build());
        var elseNode = new TransformPipelineNode("else", msg => 
            Msg.Builder().Role("assistant").TextContent("else-branch").Build());

        var ifElse = new IfElsePipelineNode("if", ctx => false, thenNode, elseNode);
        var input = Msg.Builder().Role("user").TextContent("test").Build();
        var context = new PipelineContext();

        var result = await ifElse.ExecuteAsync(input, context);

        Assert.True(result.Success);
        Assert.Equal("else-branch", result.Output?.GetTextContent());
    }

    /// <summary>
    /// Tests that IfElse passes input through unchanged when condition is false and no else branch is provided.
    /// 测试 IfElse 在条件为 false 且没有 else 分支时，将输入原样传递。
    /// </summary>
    [Fact]
    public async Task IfElse_ConditionFalse_NoElse_PassesThrough()
    {
        var thenNode = new TransformPipelineNode("then", msg => 
            Msg.Builder().Role("assistant").TextContent("then-branch").Build());

        var ifElse = new IfElsePipelineNode("if", ctx => false, thenNode, null);
        var input = Msg.Builder().Role("user").TextContent("original").Build();
        var context = new PipelineContext();

        var result = await ifElse.ExecuteAsync(input, context);

        Assert.True(result.Success);
        Assert.Equal("original", result.Output?.GetTextContent());
    }

    #endregion

    #region LoopPipeline Tests

    /// <summary>
    /// Tests that loop pipeline runs repeatedly while the condition is true.
    /// 测试循环管道在条件为 true 时重复运行。
    /// </summary>
    [Fact]
    public async Task Loop_Execute_RunsWhileConditionTrue()
    {
        int counter = 0;
        var bodyNode = new ActionPipelineNode("body", (msg, ctx) =>
        {
            counter++;
            ctx.SetValue("count", counter);
            return Task.CompletedTask;
        });

        var loop = new LoopPipelineNode("loop", ctx => ctx.GetValue<int>("count") < 3, bodyNode, maxIterations: 10);
        var input = Msg.Builder().Role("user").TextContent("test").Build();
        var context = new PipelineContext();
        context.SetValue("count", 0);

        var result = await loop.ExecuteAsync(input, context);

        Assert.True(result.Success);
        Assert.Equal(3, counter);
    }

    /// <summary>
    /// Tests that loop pipeline returns failure when max iterations are exceeded.
    /// 测试循环管道在超过最大迭代次数时返回失败。
    /// </summary>
    [Fact]
    public async Task Loop_MaxIterationsExceeded_ReturnsFailure()
    {
        var bodyNode = new TransformPipelineNode("body", msg => msg);

        var loop = new LoopPipelineNode("loop", ctx => true, bodyNode, maxIterations: 5);
        var input = Msg.Builder().Role("user").TextContent("test").Build();
        var context = new PipelineContext();

        var result = await loop.ExecuteAsync(input, context);

        Assert.False(result.Success);
        Assert.Contains("循环超过最大迭代次数", result.Error);
    }

    /// <summary>
    /// Tests that constructing a loop with invalid max iterations throws an exception.
    /// 测试使用无效的最大迭代次数构造循环时抛出异常。
    /// </summary>
    [Fact]
    public void Loop_Constructor_InvalidMaxIterations_ThrowsException()
    {
        var bodyNode = new TransformPipelineNode("body", msg => msg);

        Assert.Throws<ArgumentException>(() => 
            new LoopPipelineNode("loop", ctx => true, bodyNode, maxIterations: 0));
    }

    #endregion

    #region TransformPipeline Tests

    /// <summary>
    /// Tests that transform node applies the transformation function to the input message.
    /// 测试转换节点对输入消息应用转换函数。
    /// </summary>
    [Fact]
    public async Task Transform_Execute_AppliesFunction()
    {
        var transform = new TransformPipelineNode("transform", msg =>
            Msg.Builder().Role("assistant").TextContent($"Transformed: {msg.GetTextContent()}").Build());

        var input = Msg.Builder().Role("user").TextContent("hello").Build();
        var context = new PipelineContext();

        var result = await transform.ExecuteAsync(input, context);

        Assert.True(result.Success);
        Assert.Equal("Transformed: hello", result.Output?.GetTextContent());
    }

    /// <summary>
    /// Tests that transform node returns failure when the transformation function throws an exception.
    /// 测试转换节点在转换函数抛出异常时返回失败。
    /// </summary>
    [Fact]
    public async Task Transform_Execute_FunctionThrows_ReturnsFailure()
    {
        var transform = new TransformPipelineNode("transform", msg =>
            throw new InvalidOperationException("Transform error"));

        var input = Msg.Builder().Role("user").TextContent("hello").Build();
        var context = new PipelineContext();

        var result = await transform.ExecuteAsync(input, context);

        Assert.False(result.Success);
        Assert.Contains("Transform failed", result.Error);
    }

    #endregion

    #region ActionPipeline Tests

    /// <summary>
    /// Tests that action node executes the action and passes the input through unchanged.
    /// 测试动作节点执行操作并将输入原样传递。
    /// </summary>
    [Fact]
    public async Task Action_Execute_RunsAction_PassesThrough()
    {
        bool actionCalled = false;
        var action = new ActionPipelineNode("action", (msg, ctx) =>
        {
            actionCalled = true;
            return Task.CompletedTask;
        });

        var input = Msg.Builder().Role("user").TextContent("original").Build();
        var context = new PipelineContext();

        var result = await action.ExecuteAsync(input, context);

        Assert.True(result.Success);
        Assert.True(actionCalled);
        Assert.Equal("original", result.Output?.GetTextContent()); // Passes through unchanged
    }

    #endregion

    #region Pipeline Execution Tests

    /// <summary>
    /// Tests that the pipeline engine executes the root node and returns a result with execution metadata.
    /// 测试管道引擎执行根节点并返回包含执行元数据的结果。
    /// </summary>
    [Fact]
    public async Task Pipeline_Execute_ReturnsResult()
    {
        var node = new TransformPipelineNode("node", msg =>
            Msg.Builder().Role("assistant").TextContent($"Result: {msg.GetTextContent()}").Build());

        var pipeline = new PipelineEngine(node);
        var input = Msg.Builder().Role("user").TextContent("test").Build();

        var result = await pipeline.ExecuteAsync(input);

        Assert.True(result.Success);
        Assert.Equal("Result: test", result.Output?.GetTextContent());
        Assert.True(result.Metadata.ContainsKey("executionTimeMs"));
    }

    /// <summary>
    /// Tests that the pipeline engine accepts a string input and converts it to a message.
    /// 测试管道引擎接受字符串输入并将其转换为消息。
    /// </summary>
    [Fact]
    public async Task Pipeline_Execute_WithStringInput()
    {
        var node = new TransformPipelineNode("node", msg =>
            Msg.Builder().Role("assistant").TextContent($"Echo: {msg.GetTextContent()}").Build());

        var pipeline = new PipelineEngine(node);

        var result = await pipeline.ExecuteAsync("hello world");

        Assert.True(result.Success);
        Assert.Equal("Echo: hello world", result.Output?.GetTextContent());
    }

    /// <summary>
    /// Tests that pipeline execution can be cancelled via CancellationToken.
    /// 测试可以通过 CancellationToken 取消管道执行。
    /// </summary>
    [Fact]
    public async Task Pipeline_Execute_Cancellation_StopsExecution()
    {
        var node = new ActionPipelineNode("slow", async (msg, ctx) =>
        {
            await Task.Delay(1000, ctx.CancellationToken);
        });

        var pipeline = new PipelineEngine(node);
        var cts = new System.Threading.CancellationTokenSource();
        cts.CancelAfter(50); // Cancel after 50ms

        var result = await pipeline.ExecuteAsync("test", cts.Token);

        Assert.False(result.Success);
        Assert.Contains("cancel", result.Error, StringComparison.OrdinalIgnoreCase);
    }

    #endregion

    #region PipelineBuilder Tests

    /// <summary>
    /// Tests that PipelineBuilder.Create() returns a non-null builder instance.
    /// 测试 PipelineBuilder.Create() 返回非空的构建器实例。
    /// </summary>
    [Fact]
    public void Builder_Create_ReturnsBuilder()
    {
        var builder = PipelineBuilder.Create();
        Assert.NotNull(builder);
    }

    /// <summary>
    /// Tests that adding a single agent to the builder creates a valid pipeline.
    /// 测试向构建器添加单个 Agent 能创建有效的管道。
    /// </summary>
    [Fact]
    public void Builder_SingleAgent_CreatesPipeline()
    {
        var mockAgent = new FakeAgent("test-agent");
        
        var pipeline = PipelineBuilder.Create()
            .Agent(mockAgent)
            .Build();

        Assert.NotNull(pipeline);
    }

    /// <summary>
    /// Tests that adding multiple agents creates a sequential pipeline.
    /// 测试添加多个 Agent 能创建顺序管道。
    /// </summary>
    [Fact]
    public void Builder_MultipleAgents_CreatesSequentialPipeline()
    {
        var agent1 = new FakeAgent("agent1");
        var agent2 = new FakeAgent("agent2");

        var pipeline = PipelineBuilder.Create()
            .Agent(agent1)
            .Agent(agent2)
            .Build();

        Assert.NotNull(pipeline);
    }

    /// <summary>
    /// Tests that adding a transform step to the builder creates a valid pipeline.
    /// 测试向构建器添加转换步骤能创建有效的管道。
    /// </summary>
    [Fact]
    public void Builder_Transform_AddsTransformNode()
    {
        var pipeline = PipelineBuilder.Create()
            .Transform(msg => Msg.Builder().Role("assistant").TextContent("transformed").Build())
            .Build();

        Assert.NotNull(pipeline);
    }

    /// <summary>
    /// Tests that adding a conditional (if) step to the builder creates a valid pipeline.
    /// 测试向构建器添加条件（if）步骤能创建有效的管道。
    /// </summary>
    [Fact]
    public void Builder_If_AddsConditional()
    {
        var thenNode = new TransformPipelineNode("then", msg => msg);
        
        var pipeline = PipelineBuilder.Create()
            .If(ctx => true, thenNode)
            .Build();

        Assert.NotNull(pipeline);
    }

    /// <summary>
    /// Tests that adding a loop step to the builder creates a valid pipeline.
    /// 测试向构建器添加循环步骤能创建有效的管道。
    /// </summary>
    [Fact]
    public void Builder_Loop_AddsLoop()
    {
        var bodyNode = new TransformPipelineNode("body", msg => msg);
        
        var pipeline = PipelineBuilder.Create()
            .Loop(ctx => false, bodyNode, maxIterations: 5)
            .Build();

        Assert.NotNull(pipeline);
    }

    /// <summary>
    /// Tests that adding parallel branches to the builder creates a valid pipeline.
    /// 测试向构建器添加并行分支能创建有效的管道。
    /// </summary>
    [Fact]
    public void Builder_Parallel_AddsParallel()
    {
        var node1 = new TransformPipelineNode("node1", msg => msg);
        var node2 = new TransformPipelineNode("node2", msg => msg);

        var pipeline = PipelineBuilder.Create()
            .Parallel(node1, node2)
            .Build();

        Assert.NotNull(pipeline);
    }

    /// <summary>
    /// Tests that setting pipeline options (max depth, continue on error, timeout) via the builder works correctly.
    /// 测试通过构建器设置管道选项（最大深度、错误继续、超时）能正确工作。
    /// </summary>
    [Fact]
    public void Builder_WithOptions_SetsOptions()
    {
        var node = new TransformPipelineNode("node", msg => msg);

        var pipeline = PipelineBuilder.Create()
            .Root(node)
            .WithMaxDepth(20)
            .ContinueOnError(true)
            .WithTimeout(TimeSpan.FromSeconds(30))
            .Build();

        Assert.NotNull(pipeline);
    }

    /// <summary>
    /// Tests that building a pipeline without a root node throws an exception.
    /// 测试在没有根节点的情况下构建管道时抛出异常。
    /// </summary>
    [Fact]
    public void Builder_NoRoot_ThrowsException()
    {
        Assert.Throws<InvalidOperationException>(() =>
            PipelineBuilder.Create().Build());
    }

    #endregion

    #region Helper Classes

    /// <summary>
    /// A fake pipeline node that always returns a failure result for testing error handling.
    /// 始终返回失败结果的假管道节点，用于测试错误处理。
    /// </summary>
    private class FakeFailingNode : PipelineNodeBase
    {
        public FakeFailingNode(string name) : base(name) { }

        public override Task<PipelineResult> ExecuteAsync(Msg input, PipelineContext context)
        {
            return Task.FromResult(PipelineResult.FailureResult("Node failed"));
        }
    }

    /// <summary>
    /// A fake IAgent implementation for testing pipeline builder integration.
    /// 用于测试管道构建器集成的假 IAgent 实现。
    /// </summary>
    private class FakeAgent : global::AgentScope.Core.Agent.IAgent
    {
        public string Name { get; }

        public FakeAgent(string name)
        {
            Name = name;
        }

        /// <summary>全局唯一 Agent ID</summary>
        public string AgentId => $"agent_{Name}";

        /// <summary>Agent 描述</summary>
        public string Description => $"Fake agent: {Name}";

        /// <summary>中断（空实现）</summary>
        public void Interrupt() { }

        /// <summary>带消息的中断（空实现）</summary>
        public void Interrupt(Msg message) { }

        /// <summary>
        /// Synchronously returns a response via observable.
        /// 通过可观察对象同步返回响应。
        /// </summary>
        public System.IObservable<Msg> Call(Msg message)
        {
            return System.Reactive.Linq.Observable.Return(
                Msg.Builder().Role("assistant").TextContent($"Response from {Name}").Build());
        }

        /// <summary>Asynchronously returns a response (单条消息).</summary>
        public Task<Msg> CallAsync(Msg message, RuntimeContext? context = null)
        {
            return Task.FromResult(
                Msg.Builder().Role("assistant").TextContent($"Response from {Name}").Build());
        }

        /// <summary>Asynchronously returns a response (消息列表).</summary>
        public Task<Msg> CallAsync(IReadOnlyList<Msg> messages, RuntimeContext? context = null)
        {
            var last = messages[^1];
            return CallAsync(last, context);
        }

        /// <summary>Asynchronously returns a response (纯文本).</summary>
        public Task<Msg> CallAsync(string text, RuntimeContext? context = null)
        {
            var msg = Msg.Builder().Role("user").TextContent(text).Build();
            return CallAsync(msg, context);
        }

        /// <summary>流式处理（不支持）</summary>
        public IAsyncEnumerable<AgentScope.Core.Events.Event> StreamEventsAsync(IReadOnlyList<Msg> messages, RuntimeContext? context = null)
            => throw new NotSupportedException();

        /// <summary>流式处理单条消息（不支持）</summary>
        public IAsyncEnumerable<AgentScope.Core.Events.Event> StreamEventsAsync(Msg message, RuntimeContext? context = null)
            => throw new NotSupportedException();

        /// <summary>观察单条消息</summary>
        public async Task ObserveAsync(Msg message, RuntimeContext? context = null)
            => await CallAsync(message, context);

        /// <summary>观察多条消息</summary>
        public async Task ObserveAsync(IReadOnlyList<Msg> messages, RuntimeContext? context = null)
            => await CallAsync(messages, context);
    }

    #endregion
}
