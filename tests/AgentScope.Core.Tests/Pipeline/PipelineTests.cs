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
using System.Threading.Tasks;
using AgentScope.Core.Agent;
using AgentScope.Core.Message;
using AgentScope.Core.Pipeline;
using Xunit;

// Alias to avoid namespace conflicts
using PipelineEngine = AgentScope.Core.Pipeline.Pipeline;

namespace AgentScope.Core.Tests.Pipeline;

public class PipelineTests
{
    #region PipelineContext Tests

    [Fact]
    public void Context_CreateChildContext_IncrementsDepth()
    {
        var parent = new PipelineContext { Depth = 5, MaxDepth = 10 };
        var child = parent.CreateChildContext();

        Assert.Equal(6, child.Depth);
        Assert.Equal(10, child.MaxDepth);
        Assert.Same(parent.State, child.State); // State should be shared
    }

    [Fact]
    public void Context_SetValue_GetValue_ReturnsCorrectValue()
    {
        var context = new PipelineContext();
        context.SetValue("key", "value");

        var result = context.GetValue<string>("key");

        Assert.Equal("value", result);
    }

    [Fact]
    public void Context_GetValue_NonExistentKey_ReturnsDefault()
    {
        var context = new PipelineContext();

        var result = context.GetValue<string>("nonexistent");

        Assert.Null(result);
    }

    #endregion

    #region PipelineResult Tests

    [Fact]
    public void Result_SuccessResult_HasSuccessTrue()
    {
        var result = PipelineResult.SuccessResult();

        Assert.True(result.Success);
        Assert.Null(result.Error);
    }

    [Fact]
    public void Result_FailureResult_HasSuccessFalse()
    {
        var result = PipelineResult.FailureResult("error message");

        Assert.False(result.Success);
        Assert.Equal("error message", result.Error);
    }

    [Fact]
    public void Result_StopResult_HasStopPipelineTrue()
    {
        var result = PipelineResult.StopResult();

        Assert.True(result.Success);
        Assert.True(result.StopPipeline);
    }

    #endregion

    #region SequentialPipeline Tests

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

    [Fact]
    public void Sequential_Constructor_EmptyNodes_ThrowsException()
    {
        Assert.Throws<ArgumentException>(() => new SequentialPipelineNode("empty"));
    }

    #endregion

    #region ParallelPipeline Tests

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
        Assert.Contains("Parallel execution failed", result.Error);
    }

    #endregion

    #region IfElsePipeline Tests

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

    [Fact]
    public async Task Loop_MaxIterationsExceeded_ReturnsFailure()
    {
        var bodyNode = new TransformPipelineNode("body", msg => msg);

        var loop = new LoopPipelineNode("loop", ctx => true, bodyNode, maxIterations: 5);
        var input = Msg.Builder().Role("user").TextContent("test").Build();
        var context = new PipelineContext();

        var result = await loop.ExecuteAsync(input, context);

        Assert.False(result.Success);
        Assert.Contains("exceeded maximum iterations", result.Error);
    }

    [Fact]
    public void Loop_Constructor_InvalidMaxIterations_ThrowsException()
    {
        var bodyNode = new TransformPipelineNode("body", msg => msg);

        Assert.Throws<ArgumentException>(() => 
            new LoopPipelineNode("loop", ctx => true, bodyNode, maxIterations: 0));
    }

    #endregion

    #region TransformPipeline Tests

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

    [Fact]
    public void Builder_Create_ReturnsBuilder()
    {
        var builder = PipelineBuilder.Create();
        Assert.NotNull(builder);
    }

    [Fact]
    public void Builder_SingleAgent_CreatesPipeline()
    {
        var mockAgent = new FakeAgent("test-agent");
        
        var pipeline = PipelineBuilder.Create()
            .Agent(mockAgent)
            .Build();

        Assert.NotNull(pipeline);
    }

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

    [Fact]
    public void Builder_Transform_AddsTransformNode()
    {
        var pipeline = PipelineBuilder.Create()
            .Transform(msg => Msg.Builder().Role("assistant").TextContent("transformed").Build())
            .Build();

        Assert.NotNull(pipeline);
    }

    [Fact]
    public void Builder_If_AddsConditional()
    {
        var thenNode = new TransformPipelineNode("then", msg => msg);
        
        var pipeline = PipelineBuilder.Create()
            .If(ctx => true, thenNode)
            .Build();

        Assert.NotNull(pipeline);
    }

    [Fact]
    public void Builder_Loop_AddsLoop()
    {
        var bodyNode = new TransformPipelineNode("body", msg => msg);
        
        var pipeline = PipelineBuilder.Create()
            .Loop(ctx => false, bodyNode, maxIterations: 5)
            .Build();

        Assert.NotNull(pipeline);
    }

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

    [Fact]
    public void Builder_NoRoot_ThrowsException()
    {
        Assert.Throws<InvalidOperationException>(() =>
            PipelineBuilder.Create().Build());
    }

    #endregion

    #region Helper Classes

    private class FakeFailingNode : PipelineNodeBase
    {
        public FakeFailingNode(string name) : base(name) { }

        public override Task<PipelineResult> ExecuteAsync(Msg input, PipelineContext context)
        {
            return Task.FromResult(PipelineResult.FailureResult("Node failed"));
        }
    }

    private class FakeAgent : global::AgentScope.Core.Agent.IAgent
    {
        public string Name { get; }

        public FakeAgent(string name)
        {
            Name = name;
        }

        public System.IObservable<Msg> Call(Msg message)
        {
            return System.Reactive.Linq.Observable.Return(
                Msg.Builder().Role("assistant").TextContent($"Response from {Name}").Build());
        }

        public Task<Msg> CallAsync(Msg message)
        {
            return Task.FromResult(
                Msg.Builder().Role("assistant").TextContent($"Response from {Name}").Build());
        }
    }

    #endregion
}
