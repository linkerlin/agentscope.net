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

using Xunit;
using AgentScope.Core;
using AgentScope.Core.Message;
using AgentScope.Core.Model;
using AgentScope.Core.Memory;
using AgentScope.Core.Tool;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Threading.Tasks;

namespace AgentScope.Core.Tests.Agent;

/// <summary>
/// Tests for ReActAgent builder and execution
/// ReActAgent 构建器和执行测试
/// </summary>
public class ReActAgentTests
{
    /// <summary>
    /// Tests that ReActAgent.CallAsync processes a user message and stores it in memory.
    /// 测试 ReActAgent.CallAsync 处理用户消息并将其存入记忆。
    /// </summary>
    [Fact]
    public async Task ReActAgent_CallAsync_ShouldProcessMessage()
    {
        // Arrange
        var model = MockModel.Builder().ModelName("test-model").Build();
        var memory = new MemoryBase();
        var agent = ReActAgent.Builder()
            .Name("TestAgent")
            .Model(model)
            .Memory(memory)
            .SysPrompt("You are a test agent")
            .Build();

        var userMsg = Msg.Builder()
            .Role("user")
            .TextContent("Hello")
            .Build();

        // Act
        var response = await agent.CallAsync(userMsg);

        // Assert
        Assert.NotNull(response);
        Assert.NotNull(response.GetTextContent());
        Assert.Equal(2, memory.Count()); // User message + agent response
    }

    /// <summary>
    /// Tests that building ReActAgent without a model throws InvalidOperationException.
    /// 测试构建 ReActAgent 时不提供模型会抛出 InvalidOperationException。
    /// </summary>
    [Fact]
    public void ReActAgent_Builder_ShouldRequireModel()
    {
        // Arrange & Act & Assert
        Assert.Throws<System.InvalidOperationException>(() =>
        {
            ReActAgent.Builder()
                .Name("TestAgent")
                .Build();
        });
    }

    /// <summary>
    /// Tests that building ReActAgent with all properties succeeds and sets the agent name.
    /// 测试使用全部属性构建 ReActAgent 成功并设置代理名称。
    /// </summary>
    [Fact]
    public void ReActAgent_Builder_WithAllProperties_ShouldBuildSuccessfully()
    {
        // Arrange
        var model = MockModel.Builder().Build();
        var memory = new MemoryBase();

        // Act
        var agent = ReActAgent.Builder()
            .Name("TestAgent")
            .Model(model)
            .Memory(memory)
            .SysPrompt("Test prompt")
            .MaxIterations(5)
            .Build();

        // Assert
        Assert.NotNull(agent);
        Assert.Equal("TestAgent", agent.Name);
    }

    /// <summary>
    /// Tests that ReActAgent uses the provided custom memory for storing conversation history.
    /// 测试 ReActAgent 使用提供的自定义记忆来存储对话历史。
    /// </summary>
    [Fact]
    public async Task ReActAgent_WithCustomMemory_ShouldUseProvidedMemory()
    {
        // Arrange
        var model = MockModel.Builder().Build();
        var memory = new MemoryBase();
        var agent = ReActAgent.Builder()
            .Name("TestAgent")
            .Model(model)
            .Memory(memory)
            .Build();

        var userMsg = Msg.Builder().TextContent("Test").Build();

        // Act
        await agent.CallAsync(userMsg);

        // Assert
        Assert.Equal(2, memory.Count());
    }

    /// <summary>
    /// Tests that ReActAgent.Call works as an observable sequence and returns a response.
    /// 测试 ReActAgent.Call 作为可观察序列工作并返回响应。
    /// </summary>
    [Fact]
    public async Task ReActAgent_Call_ShouldWorkAsObservable()
    {
        // Arrange
        var model = MockModel.Builder().Build();
        var agent = ReActAgent.Builder()
            .Name("TestAgent")
            .Model(model)
            .Build();

        var userMsg = Msg.Builder().TextContent("Test").Build();

        // Act
        var response = await agent.Call(userMsg).FirstOrDefaultAsync();

        // Assert
        Assert.NotNull(response);
        Assert.NotNull(response.GetTextContent());
    }

    /// <summary>
    /// Tests that ReActAgent uses Action Input as the final answer when the ReAct format is used with tools.
    /// 测试 ReActAgent 在使用工具和 ReAct 格式时将 Action Input 作为最终回答。
    /// </summary>
    [Fact]
    public async Task ReActAgent_WithToolAndReActFormat_ShouldUseActionInputAsFinalAnswer()
    {
        // Arrange
        var model = new ScriptedModel(
            "Thought: 需要直接回复用户\nAction: finish\nAction Input: 你好！");
        var agent = ReActAgent.Builder()
            .Name("ReActFormatAgent")
            .Model(model)
            .AddTool(new NoOpTool())
            .MaxIterations(2)
            .Build();

        var userMsg = Msg.Builder().TextContent("你好").Build();

        // Act
        var response = await agent.CallAsync(userMsg);

        // Assert
        Assert.Equal("你好！", response.GetTextContent());
    }

    /// <summary>
    /// Tests that when Action Input is empty/missing, the response text is empty (no completion marker).
    /// 测试当 Action Input 为空/缺失时，响应文本为空（无完成标记）。
    /// </summary>
    [Fact]
    public async Task ReActAgent_WithToolAndMissingActionInput_ShouldNotReturnCompletionMarker()
    {
        // Arrange
        var model = new ScriptedModel(
            "Thought: 用户发来问候，无需调用工具，直接完成回复即可。\nAction: finish\nAction Input: Done");
        var agent = ReActAgent.Builder()
            .Name("NoMarkerAgent")
            .Model(model)
            .AddTool(new NoOpTool())
            .MaxIterations(2)
            .Build();

        var userMsg = Msg.Builder().TextContent("你好").Build();

        // Act
        var response = await agent.CallAsync(userMsg);
        var text = response.GetTextContent();

        // Assert
        Assert.True(string.IsNullOrEmpty(text));
    }

    /// <summary>
    /// A tool that performs no operation, used for testing tool integration.
    /// 不执行任何操作的测试工具，用于测试工具集成。
    /// </summary>
    private sealed class NoOpTool : ToolBase
    {
        public NoOpTool() : base("noop", "No operation tool")
        {
        }

        public override Dictionary<string, object> GetSchema() => new();

        public override Task<ToolResult> ExecuteAsync(Dictionary<string, object> parameters)
        {
            return Task.FromResult(ToolResult.Ok("ok"));
        }
    }

    /// <summary>
    /// A scripted model that returns predefined responses in order, used for deterministic testing.
    /// 按顺序返回预定义响应的脚本化模型，用于确定性测试。
    /// </summary>
    private sealed class ScriptedModel : IModel
    {
        private readonly Queue<string> _responses;

        public ScriptedModel(params string[] responses)
        {
            _responses = new Queue<string>(responses);
        }

        public string ModelName => "scripted";

        public IObservable<ModelResponse> Generate(ModelRequest request)
        {
            return Observable.Return(CreateResponse());
        }

        public Task<ModelResponse> GenerateAsync(ModelRequest request)
        {
            return Task.FromResult(CreateResponse());
        }

        private ModelResponse CreateResponse()
        {
            var text = _responses.Count > 0 ? _responses.Dequeue() : string.Empty;
            return new ModelResponse
            {
                Success = true,
                Text = text
            };
        }
    }
}
