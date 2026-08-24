// Copyright 2024-2026 the original author or authors.
// Licensed under the Apache License, Version 2.0

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using AgentScope.Core.Memory;
using AgentScope.Core.Message;
using AgentScope.Core.Model;
using AgentScope.Core.State;
using AgentScope.Core.Tool;
using Xunit;

namespace AgentScope.Core.Tests.State;

/// <summary>
/// Unit tests for <see cref="EnhancedReActAgent"/> state persistence via <see cref="IStateModule"/>,
/// verifying that SaveTo/LoadFrom round-trips agent identity, memory, and tool group configuration,
/// and that LoadIfExists handles missing state gracefully.
/// 对 <see cref="EnhancedReActAgent"/> 通过 <see cref="IStateModule"/> 进行状态持久化的单元测试，
/// 验证 SaveTo/LoadFrom 能否正确保存和恢复 agent 标识、记忆以及工具组配置，
/// 并验证 LoadIfExists 在状态缺失时能优雅处理。
/// </summary>
public class EnhancedReActAgentStateTests
{
    /// <summary>
    /// Tests that SaveTo persists agent metadata, memory messages, and tool group activation state,
    /// and that LoadFrom restores all of them — overwriting builder-supplied defaults.
    /// 测试 SaveTo 持久化 agent 元数据、记忆消息和工具组激活状态，
    /// 且 LoadFrom 能恢复所有状态——覆盖构造器提供的默认值。
    /// </summary>
    [Fact]
    public async Task SaveTo_And_LoadFrom_RestoresMetaMemoryAndToolGroups()
    {
        var session = new AgentScope.Core.Session.Session("sid");

        var saveMemory = new MemoryBase();
        var saveGroupManager = CreateToolGroupManager(userActive: true, adminActive: false);
        var saveAgent = EnhancedReActAgent.Builder()
            .Name("OriginalAgent")
            .SysPrompt("Original system prompt")
            .Model(new CapturingScriptedModel("Thought: 保存状态\nAction: finish\nAction Input: saved"))
            .Memory(saveMemory)
            .AddTool(new TrackingTool("allowed_tool", "允许工具"))
            .AddTool(new TrackingTool("blocked_tool", "禁用工具"))
            .ToolGroupManager(saveGroupManager)
            .Build();

        await saveAgent.CallAsync(Msg.Builder().Role("user").TextContent("save").Build());
        ((IStateModule)saveAgent).SaveTo(session, "s1");

        var loadMemory = new MemoryBase();
        var loadGroupManager = CreateToolGroupManager(userActive: false, adminActive: true);
        var loadModel = new CapturingScriptedModel("Thought: 恢复后响应\nAction: finish\nAction Input: restored");
        var restoredAgent = EnhancedReActAgent.Builder()
            .Name("DifferentName")
            .SysPrompt("Different prompt")
            .Model(loadModel)
            .Memory(loadMemory)
            .AddTool(new TrackingTool("allowed_tool", "允许工具"))
            .AddTool(new TrackingTool("blocked_tool", "禁用工具"))
            .ToolGroupManager(loadGroupManager)
            .Build();

        ((IStateModule)restoredAgent).LoadFrom(session, "s1");

        Assert.Equal("OriginalAgent", restoredAgent.Name);
        Assert.Equal(2, loadMemory.Count());

        var activeTools = loadGroupManager.GetActiveToolNames().ToList();
        Assert.Contains("allowed_tool", activeTools);
        Assert.DoesNotContain("blocked_tool", activeTools);

        var response = await restoredAgent.CallAsync(Msg.Builder().Role("user").TextContent("after-load").Build());
        Assert.Equal("restored", response.GetTextContent());
        Assert.Single(loadModel.CapturedPrompts);
        Assert.Contains("Original system prompt", loadModel.CapturedPrompts[0], StringComparison.Ordinal);
        Assert.Contains("allowed_tool", loadModel.CapturedPrompts[0], StringComparison.Ordinal);
        Assert.DoesNotContain("blocked_tool", loadModel.CapturedPrompts[0], StringComparison.Ordinal);
    }

    [Fact]
    public async Task LoadIfExists_WhenMissing_DoesNotOverwriteCurrentState()
    {
        var session = new AgentScope.Core.Session.Session("sid");
        var memory = new MemoryBase();
        var toolGroupManager = CreateToolGroupManager(userActive: false, adminActive: true);
        var model = new CapturingScriptedModel("Thought: 保持当前配置\nAction: finish\nAction Input: ok");

        var agent = EnhancedReActAgent.Builder()
            .Name("CurrentAgent")
            .SysPrompt("Current prompt")
            .Model(model)
            .Memory(memory)
            .AddTool(new TrackingTool("allowed_tool", "允许工具"))
            .AddTool(new TrackingTool("blocked_tool", "禁用工具"))
            .ToolGroupManager(toolGroupManager)
            .Build();

        ((IStateModule)agent).LoadIfExists(session, "missing");

        Assert.Equal("CurrentAgent", agent.Name);
        Assert.Empty(memory.GetAll());

        var activeTools = toolGroupManager.GetActiveToolNames().ToList();
        Assert.Contains("blocked_tool", activeTools);
        Assert.DoesNotContain("allowed_tool", activeTools);

        var response = await agent.CallAsync(Msg.Builder().Role("user").TextContent("after-missing").Build());
        Assert.Equal("ok", response.GetTextContent());
        Assert.Single(model.CapturedPrompts);
        Assert.Contains("Current prompt", model.CapturedPrompts[0], StringComparison.Ordinal);
        Assert.Contains("blocked_tool", model.CapturedPrompts[0], StringComparison.Ordinal);
        Assert.DoesNotContain("allowed_tool", model.CapturedPrompts[0], StringComparison.Ordinal);
    }

    private static ToolGroupManager CreateToolGroupManager(bool userActive, bool adminActive)
    {
        var manager = new ToolGroupManager();

        var userGroup = new ToolGroup("user", isActive: userActive);
        userGroup.AddTool("allowed_tool");
        manager.RegisterGroup(userGroup);

        var adminGroup = new ToolGroup("admin", isActive: adminActive);
        adminGroup.AddTool("blocked_tool");
        manager.RegisterGroup(adminGroup);

        return manager;
    }

    private sealed class TrackingTool : ToolBase
    {
        public TrackingTool(string name, string description) : base(name, description)
        {
        }

        public override Task<ToolResult> ExecuteAsync(Dictionary<string, object> parameters)
        {
            _ = parameters;
            return Task.FromResult(ToolResult.Ok(Name));
        }

        public override Dictionary<string, object> GetSchema()
        {
            return new Dictionary<string, object>
            {
                ["name"] = Name,
                ["description"] = Description,
                ["parameters"] = new Dictionary<string, object>()
            };
        }
    }

    private sealed class CapturingScriptedModel : IModel
    {
        private readonly Queue<string> _responses;

        public CapturingScriptedModel(params string[] responses)
        {
            _responses = new Queue<string>(responses);
        }

        public string ModelName => "capturing-scripted";

        public List<string> CapturedPrompts { get; } = new();

        public IObservable<ModelResponse> Generate(ModelRequest request)
        {
            return Observable.Return(CreateResponse(request));
        }

        public Task<ModelResponse> GenerateAsync(ModelRequest request)
        {
            return Task.FromResult(CreateResponse(request));
        }

        private ModelResponse CreateResponse(ModelRequest request)
        {
            var prompt = request.Messages.LastOrDefault()?.GetTextContent() ?? string.Empty;
            CapturedPrompts.Add(prompt);

            var text = _responses.Count > 0 ? _responses.Dequeue() : string.Empty;
            return new ModelResponse
            {
                Success = true,
                Text = text
            };
        }
    }
}