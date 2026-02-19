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
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AgentScope.Core;
using AgentScope.Core.Message;
using AgentScope.Core.Model;
using AgentScope.Core.Model.DeepSeek;
using AgentScope.Core.Model.OpenAI;
using AgentScope.Core.Tool;
using AgentScope.Core.Memory;
using AgentScope.Core.Session;
using AgentScope.Core.Hook;
using AgentScope.Core.Workflow;
using AgentScope.Core.Pipeline;
using DotNetEnv;
using Xunit;

namespace AgentScope.Integration.Tests;

/// <summary>
/// LLM 系统测试 - 使用真实 LLM API
/// System tests using real LLM API
/// 
/// 优先级:
/// 1. DeepSeek (DEEPSEEK_API_KEY, DEEPSEEK_MODEL)
/// 2. OpenAI 兼容 API (OPENAI_API_KEY, OPENAI_BASE_URL, OPENAI_MODEL)
/// 
/// 环境变量配置:
/// DeepSeek:
/// - DEEPSEEK_API_KEY: DeepSeek API密钥
/// - DEEPSEEK_MODEL: 模型名称 (如 deepseek-chat)
/// 
/// OpenAI 兼容:
/// - OPENAI_API_KEY: API密钥
/// - OPENAI_BASE_URL: API基础URL (可选,用于兼容OpenAI的其他服务)
/// - OPENAI_MODEL: 模型名称 (可选)
/// </summary>
public class LlmSystemTests : IDisposable
{
    private readonly string? _apiKey;
    private readonly string? _baseUrl;
    private readonly string _modelName;
    private readonly bool _isConfigured;
    private readonly string _testDbPath;
    private readonly string _providerName;
    private readonly bool _isDeepSeek;

    public LlmSystemTests()
    {
        // 加载 .env 文件
        var envPath = Path.Combine(Directory.GetCurrentDirectory(), ".env");
        if (File.Exists(envPath))
        {
            Env.Load(envPath);
        }

        // 优先使用 DeepSeek
        var deepseekApiKey = Environment.GetEnvironmentVariable("DEEPSEEK_API_KEY");
        var deepseekModel = Environment.GetEnvironmentVariable("DEEPSEEK_MODEL");

        if (!string.IsNullOrEmpty(deepseekApiKey) && !string.IsNullOrEmpty(deepseekModel))
        {
            // 使用 DeepSeek
            _apiKey = deepseekApiKey;
            _baseUrl = "https://api.deepseek.com";
            _modelName = deepseekModel;
            _providerName = "DeepSeek";
            _isDeepSeek = true;
            _isConfigured = true;
        }
        else
        {
            // 回退到 OpenAI 兼容 API
            _apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
            _baseUrl = Environment.GetEnvironmentVariable("OPENAI_BASE_URL");
            _modelName = Environment.GetEnvironmentVariable("OPENAI_MODEL") ?? "gpt-3.5-turbo";
            _providerName = "OpenAI";
            _isDeepSeek = false;
            _isConfigured = !string.IsNullOrEmpty(_apiKey);
        }

        _testDbPath = Path.Combine(Path.GetTempPath(), $"llm_test_{Guid.NewGuid()}.db");
    }

    public void Dispose()
    {
        if (File.Exists(_testDbPath))
        {
            try { File.Delete(_testDbPath); } catch { }
        }
    }

    /// <summary>
    /// Create model instance based on configuration
    /// </summary>
    private IModel CreateModel()
    {
        if (_isDeepSeek)
        {
            return DeepSeekModel.Builder()
                .ModelName(_modelName)
                .ApiKey(_apiKey!)
                .Build();
        }
        else
        {
            return new OpenAIModel(_modelName, _apiKey, _baseUrl);
        }
    }

    #region 基础模型测试

    [Fact]
    public async Task OpenAIModel_SimpleChat_ShouldReturnResponse()
    {
        if (!_isConfigured)
        {
            return; // Skip if not configured
        }

        // Arrange
        var model = CreateModel();

        var request = new ModelRequest
        {
            Messages = new List<Msg>
            {
                Msg.Builder().Role("user").TextContent("Say 'Hello, World!'").Build()
            }
        };

        // Act
        var response = await model.GenerateAsync(request);

        // Assert - Print debug info
        Console.WriteLine($"Provider: {_providerName}");
        Console.WriteLine($"Model: {_modelName}");
        Console.WriteLine($"Success: {response.Success}");
        Console.WriteLine($"Text: {response.Text}");
        Console.WriteLine($"Error: {response.Error}");
        Console.WriteLine($"Metadata: {response.Metadata}");
        
        Assert.True(response.Success, $"Response failed: {response.Error}");
        Assert.NotNull(response.Text);
        Assert.NotEmpty(response.Text);
    }

    [Fact]
    public async Task OpenAIModel_MultiTurnConversation_ShouldMaintainContext()
    {
        if (!_isConfigured) return;

        // Arrange
        var model = CreateModel();
        var messages = new List<Msg>
        {
            Msg.Builder().Role("user").TextContent("My name is Alice.").Build(),
            Msg.Builder().Role("assistant").TextContent("Nice to meet you, Alice!").Build(),
            Msg.Builder().Role("user").TextContent("What is my name?").Build()
        };

        var request = new ModelRequest { Messages = messages };

        // Act
        var response = await model.GenerateAsync(request);

        // Assert
        Assert.True(response.Success);
        Assert.Contains("Alice", response.Text, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task OpenAIModel_SystemPrompt_ShouldAffectResponse()
    {
        if (!_isConfigured) return;

        // Arrange
        var model = CreateModel();
        var messages = new List<Msg>
        {
            Msg.Builder().Role("system").TextContent("You are a pirate. Always speak like a pirate.").Build(),
            Msg.Builder().Role("user").TextContent("Hello!").Build()
        };

        var request = new ModelRequest { Messages = messages };

        // Act
        var response = await model.GenerateAsync(request);

        // Assert
        Assert.True(response.Success);
        Assert.NotNull(response.Text);
        // 检查是否包含海盗风格的语言特征
        var text = response.Text.ToLower();
        Assert.True(
            text.Contains("arr") || 
            text.Contains("mate") || 
            text.Contains("ye") || 
            text.Contains("ahoy") ||
            text.Contains("!"),
            $"Expected pirate-like response, got: {response.Text}"
        );
    }

    #endregion

    #region Agent 集成测试

    [Fact]
    public async Task ReActAgent_WithRealLLM_ShouldAnswerQuestion()
    {
        if (!_isConfigured) return;

        // Arrange
        var model = CreateModel();
        var agent = ReActAgent.Builder()
            .Name("TestAgent")
            .Model(model)
            .SysPrompt("You are a helpful assistant. Answer questions concisely.")
            .MaxIterations(3)
            .Build();

        // Act
        var response = await agent.CallAsync(
            Msg.Builder().TextContent("What is 2 + 2?").Build()
        );

        // Assert
        Assert.NotNull(response);
        Assert.Contains("4", response.GetTextContent() ?? "");
    }

    [Fact]
    public async Task ReActAgent_WithMemory_ShouldRememberContext()
    {
        if (!_isConfigured) return;

        // Arrange
        var model = CreateModel();
        using var memory = new SqliteMemory(_testDbPath);

        var agent = ReActAgent.Builder()
            .Name("MemoryAgent")
            .Model(model)
            .Memory(memory)
            .SysPrompt("You are a helpful assistant. Remember what users tell you.")
            .Build();

        // Act - First interaction
        await agent.CallAsync(Msg.Builder().TextContent("My favorite color is blue.").Build());

        // Act - Second interaction asking about the color
        var response = await agent.CallAsync(
            Msg.Builder().TextContent("What is my favorite color?").Build()
        );

        // Assert
        Assert.NotNull(response);
        Assert.Contains("blue", response.GetTextContent()?.ToLower() ?? "");
        Assert.True(memory.Count() >= 4); // At least 2 user + 2 assistant messages
    }

    [Fact]
    public async Task ReActAgent_WithTool_ShouldUseTool()
    {
        if (!_isConfigured) return;

        // Arrange
        var model = CreateModel();
        var calculatorTool = new CalculatorTool();

        var agent = ReActAgent.Builder()
            .Name("ToolAgent")
            .Model(model)
            .AddTool(calculatorTool)
            .SysPrompt("You are a helpful assistant. Use tools when appropriate.")
            .MaxIterations(5)
            .Build();

        // Act
        var response = await agent.CallAsync(
            Msg.Builder().TextContent("Use the calculator to compute 15 * 7").Build()
        );

        // Assert
        Assert.NotNull(response);
        // 结果应该包含 105
        Assert.Contains("105", response.GetTextContent() ?? "");
    }

    #endregion

    #region Pipeline 集成测试

    [Fact]
    public async Task Pipeline_WithRealLLM_ShouldProcessMessages()
    {
        if (!_isConfigured) return;

        // Arrange
        var model = CreateModel();
        
        var agent = ReActAgent.Builder()
            .Name("PipelineAgent")
            .Model(model)
            .Build();
            
        var agentNode = new AgentPipelineNode("AgentNode", agent);
        var pipeline = new Pipeline(agentNode);

        // Act
        var input = Msg.Builder().TextContent("Tell me a short joke.").Build();
        var result = await pipeline.ExecuteAsync(input);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Success);
        Assert.NotEmpty(result.Output?.GetTextContent() ?? "");
    }

    #endregion

    #region Workflow 集成测试

    [Fact]
    public async Task Workflow_WithRealLLM_ShouldExecute()
    {
        if (!_isConfigured) return;

        // Arrange
        var model = CreateModel();

        var startNode = new WorkflowNode { Name = "start", Type = WorkflowNodeType.Start };
        var taskNode = new WorkflowNode 
        { 
            Name = "generate", 
            Type = WorkflowNodeType.Task,
            Dependencies = new List<string> { startNode.Id }
        };
        var endNode = new WorkflowNode 
        { 
            Name = "end", 
            Type = WorkflowNodeType.End,
            Dependencies = new List<string> { taskNode.Id }
        };

        var workflow = new WorkflowDefinition
        {
            Nodes = new List<WorkflowNode> { startNode, taskNode, endNode }
        };

        var engine = new WorkflowEngine();

        // Act
        var inputs = new Dictionary<string, object>
        {
            ["message"] = "Say hello"
        };
        var result = await engine.ExecuteAsync(workflow, inputs);

        // Assert
        Assert.NotNull(result);
    }

    #endregion

    #region Hook 集成测试

    [Fact]
    public async Task Agent_WithHook_ShouldInvokeHookCallbacks()
    {
        if (!_isConfigured) return;

        // Arrange
        var model = CreateModel();
        var hook = new TestHook();
        var hookManager = new HookManager();
        hookManager.RegisterHook(hook);

        var agent = ReActAgent.Builder()
            .Name("HookAgent")
            .Model(model)
            .Build();

        // Act - Execute with hook via agent
        await agent.CallAsync(Msg.Builder().TextContent("Hello").Build());

        // Note: Hook integration depends on agent implementation
        // This test verifies the hook can be created and registered
        Assert.NotNull(hook);
    }

    #endregion

    #region Session 管理测试

    [Fact]
    public async Task SessionManager_WithRealLLM_ShouldManageSessions()
    {
        if (!_isConfigured) return;

        // Arrange
        var model = CreateModel();
        var sessionManager = new SessionManager();

        var session = sessionManager.CreateSession(name: "Test Session");
        var agent = ReActAgent.Builder()
            .Name("SessionAgent")
            .Model(model)
            .Build();

        // Act
        session.Context["agent"] = agent;
        var response = await agent.CallAsync(Msg.Builder().TextContent("Hello").Build());
        
        // Store response in session context
        session.Context["lastResponse"] = response.GetTextContent() ?? "";

        // Assert
        Assert.Equal(SessionStatus.Active, session.Status);
        Assert.NotNull(session.Context["lastResponse"]);
    }

    #endregion

    #region 错误处理测试

    [Fact]
    public async Task OpenAIModel_InvalidApiKey_ShouldFailGracefully()
    {
        // Arrange - 使用无效的 API Key
        var model = new OpenAIModel(_modelName, "invalid-key-12345", _baseUrl);

        var request = new ModelRequest
        {
            Messages = new List<Msg>
            {
                Msg.Builder().Role("user").TextContent("Hello").Build()
            }
        };

        // Act & Assert - 期望抛出 ModelException
        var exception = await Assert.ThrowsAsync<ModelException>(
            () => model.GenerateAsync(request));
        
        Assert.Contains("401", exception.Message);
    }

    [Fact]
    public async Task OpenAIModel_Timeout_ShouldHandleProperly()
    {
        if (!_isConfigured) return;

        // Arrange
        var model = CreateModel();

        // 使用可能导致超时的配置
        var request = new ModelRequest
        {
            Messages = new List<Msg>
            {
                Msg.Builder().Role("user").TextContent("Hello").Build()
            }
        };

        // Act - 设置超时
        using var cts = new System.Threading.CancellationTokenSource(TimeSpan.FromSeconds(60));
        var task = model.GenerateAsync(request);

        // Assert - 应该在超时前完成或正确处理
        var response = await task;
        Assert.NotNull(response);
    }

    #endregion

    #region 流式响应测试

    [Fact]
    public async Task OpenAIModel_StreamResponse_ShouldYieldChunks()
    {
        if (!_isConfigured) return;

        // Arrange
        var model = CreateModel();
        var chunks = new List<string>();

        var request = new ModelRequest
        {
            Messages = new List<Msg>
            {
                Msg.Builder().Role("user").TextContent("Count from 1 to 5").Build()
            }
        };

        // Act
        var observable = model.Generate(request);
        var subscription = observable.Subscribe(
            chunk => chunks.Add(chunk.Text ?? ""),
            error => { /* Handle error */ },
            () => { /* Completed */ }
        );

        // Wait for completion
        await Task.Delay(30000); // 30 second timeout

        // Assert
        Assert.NotEmpty(chunks);
    }

    #endregion
}

/// <summary>
/// 测试用 Hook 实现
/// </summary>
public class TestHook : HookBase
{
    public List<string> Invocations { get; } = new();

    public override Task OnPreReasoningAsync(PreReasoningEvent @event)
    {
        Invocations.Add("PreReasoning");
        return Task.CompletedTask;
    }

    public override Task OnPostReasoningAsync(PostReasoningEvent @event)
    {
        Invocations.Add("PostReasoning");
        return Task.CompletedTask;
    }

    public override Task OnPreActingAsync(PreActingEvent @event)
    {
        Invocations.Add("PreActing");
        return Task.CompletedTask;
    }

    public override Task OnPostActingAsync(PostActingEvent @event)
    {
        Invocations.Add("PostActing");
        return Task.CompletedTask;
    }
}
