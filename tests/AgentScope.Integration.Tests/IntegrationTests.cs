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
using System;
using System.IO;
using System.Threading.Tasks;

namespace AgentScope.Integration.Tests;

/// <summary>
/// Integration tests for agent-memory workflows
/// </summary>
public class AgentMemoryIntegrationTests : IDisposable
{
    private readonly string _testDbPath;

    public AgentMemoryIntegrationTests()
    {
        _testDbPath = Path.Combine(Path.GetTempPath(), $"integration_test_{Guid.NewGuid()}.db");
    }

    public void Dispose()
    {
        if (File.Exists(_testDbPath))
        {
            try
            {
                File.Delete(_testDbPath);
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
    }

    [Fact]
    public async Task Agent_WithSqliteMemory_ShouldPersistConversation()
    {
        // Arrange
        var model = MockModel.Builder().Build();
        using var memory = new SqliteMemory(_testDbPath);

        var agent = ReActAgent.Builder()
            .Name("TestAgent")
            .Model(model)
            .Memory(memory)
            .SysPrompt("You are a helpful assistant")
            .Build();

        // Act - First conversation
        var msg1 = Msg.Builder().TextContent("Hello").Build();
        await agent.CallAsync(msg1);

        var msg2 = Msg.Builder().TextContent("How are you?").Build();
        await agent.CallAsync(msg2);

        // Assert - Check memory persistence
        Assert.Equal(4, memory.Count()); // 2 user messages + 2 agent responses

        // Act - Create new agent with same memory
        var agent2 = ReActAgent.Builder()
            .Name("TestAgent2")
            .Model(model)
            .Memory(memory)
            .Build();

        var msg3 = Msg.Builder().TextContent("Third message").Build();
        await agent2.CallAsync(msg3);

        // Assert - New agent should see all previous messages
        Assert.Equal(6, memory.Count()); // Previous 4 + new 2
    }

    [Fact]
    public async Task Agent_WithMemory_ShouldMaintainConversationContext()
    {
        // Arrange
        var model = MockModel.Builder().Build();
        var memory = new MemoryBase();

        var agent = ReActAgent.Builder()
            .Name("TestAgent")
            .Model(model)
            .Memory(memory)
            .Build();

        // Act - Multiple messages
        for (int i = 0; i < 5; i++)
        {
            var msg = Msg.Builder().TextContent($"Message {i}").Build();
            await agent.CallAsync(msg);
        }

        // Assert
        Assert.Equal(10, memory.Count()); // 5 user messages + 5 responses
        var recent = memory.GetRecent(3);
        Assert.Equal(3, recent.Count);
    }

    [Fact]
    public async Task Agent_WithoutMemory_ShouldUseDefaultMemory()
    {
        // Arrange
        var model = MockModel.Builder().Build();
        var agent = ReActAgent.Builder()
            .Name("TestAgent")
            .Model(model)
            .Build();

        // Act
        var msg = Msg.Builder().TextContent("Test").Build();
        var response = await agent.CallAsync(msg);

        // Assert
        Assert.NotNull(response);
        Assert.NotNull(response.GetTextContent());
    }
}

/// <summary>
/// Integration tests for agent-model-tool workflows
/// </summary>
public class AgentModelToolIntegrationTests
{
    [Fact]
    public async Task Agent_WithTools_ShouldHaveToolsAvailable()
    {
        // Arrange
        var model = MockModel.Builder().Build();
        var calculatorTool = new CalculatorTool();
        var timeTool = new GetTimeTool();

        var agent = ReActAgent.Builder()
            .Name("ToolAgent")
            .Model(model)
            .AddTool(calculatorTool)
            .AddTool(timeTool)
            .Build();

        // Act
        var msg = Msg.Builder().TextContent("Calculate something").Build();
        var response = await agent.CallAsync(msg);

        // Assert
        Assert.NotNull(response);
        // Note: Full tool execution integration would require more complete ReActAgent implementation
    }

    [Fact]
    public async Task MultipleAgents_WithSharedMemory_ShouldCommunicate()
    {
        // Arrange
        var model = MockModel.Builder().Build();
        var sharedMemory = new MemoryBase();

        var agent1 = ReActAgent.Builder()
            .Name("Agent1")
            .Model(model)
            .Memory(sharedMemory)
            .Build();

        var agent2 = ReActAgent.Builder()
            .Name("Agent2")
            .Model(model)
            .Memory(sharedMemory)
            .Build();

        // Act
        var msg1 = Msg.Builder().TextContent("Hello from Agent1").Build();
        await agent1.CallAsync(msg1);

        var msg2 = Msg.Builder().TextContent("Hello from Agent2").Build();
        await agent2.CallAsync(msg2);

        // Assert
        Assert.Equal(4, sharedMemory.Count());
        var allMessages = sharedMemory.GetAll();
        Assert.Contains(allMessages, m => m.GetTextContent()?.Contains("Agent1") == true);
        Assert.Contains(allMessages, m => m.GetTextContent()?.Contains("Agent2") == true);
    }
}

/// <summary>
/// End-to-end workflow tests
/// </summary>
public class EndToEndWorkflowTests : IDisposable
{
    private readonly string _testDbPath;

    public EndToEndWorkflowTests()
    {
        _testDbPath = Path.Combine(Path.GetTempPath(), $"e2e_test_{Guid.NewGuid()}.db");
    }

    public void Dispose()
    {
        if (File.Exists(_testDbPath))
        {
            try
            {
                File.Delete(_testDbPath);
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
    }

    [Fact]
    public async Task CompleteWorkflow_CreateAgentAndConverse_ShouldWorkEndToEnd()
    {
        // Arrange - Setup complete environment
        var model = MockModel.Builder().ModelName("test-model").Build();
        using var memory = new SqliteMemory(_testDbPath);
        var calculatorTool = new CalculatorTool();

        var agent = ReActAgent.Builder()
            .Name("CompleteAgent")
            .Model(model)
            .Memory(memory)
            .AddTool(calculatorTool)
            .SysPrompt("You are a helpful assistant")
            .MaxIterations(10)
            .Build();

        // Act - Simulate conversation
        var msg1 = Msg.Builder()
            .Role("user")
            .TextContent("Hello, how can you help me?")
            .Build();
        var response1 = await agent.CallAsync(msg1);

        var msg2 = Msg.Builder()
            .Role("user")
            .TextContent("Can you calculate something?")
            .Build();
        var response2 = await agent.CallAsync(msg2);

        // Assert - Check entire workflow
        Assert.NotNull(response1);
        Assert.NotNull(response2);
        Assert.Equal(4, memory.Count()); // 2 messages + 2 responses

        // Verify memory persistence
        var allMessages = memory.GetAll();
        Assert.Equal(4, allMessages.Count);
        Assert.Equal("user", allMessages[0].Role);
        Assert.Equal("assistant", allMessages[1].Role);

        // Search functionality
        var searchResults = await memory.SearchAsync("help");
        Assert.NotEmpty(searchResults);
    }

    [Fact]
    public async Task MemorySearch_AfterConversation_ShouldFindRelevantMessages()
    {
        // Arrange
        var model = MockModel.Builder().Build();
        using var memory = new SqliteMemory(_testDbPath);
        var agent = ReActAgent.Builder()
            .Name("SearchAgent")
            .Model(model)
            .Memory(memory)
            .Build();

        // Act - Add diverse messages
        await agent.CallAsync(Msg.Builder().TextContent("Weather is sunny today").Build());
        await agent.CallAsync(Msg.Builder().TextContent("I love programming").Build());
        await agent.CallAsync(Msg.Builder().TextContent("The weather forecast looks good").Build());

        // Search for weather-related messages
        var weatherResults = await memory.SearchAsync("weather");

        // Assert
        Assert.NotEmpty(weatherResults);
        Assert.True(weatherResults.Count >= 2);
        Assert.All(weatherResults, msg =>
            Assert.Contains("weather", msg.GetTextContent()?.ToLower() ?? ""));
    }
}
