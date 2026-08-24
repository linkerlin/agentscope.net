// Copyright 2024-2026 the original author or authors.
// Licensed under the Apache License, Version 2.0

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using AgentScope.Core.Message;
using AgentScope.Core.Model;
using AgentScope.Core.Tool;
using Xunit;

namespace AgentScope.Core.Tests.Agent;

/// <summary>
/// Integration tests for runtime tool group management in ReActAgent and EnhancedReActAgent
/// ReActAgent 和 EnhancedReActAgent 运行时工具组管理集成测试
/// </summary>
public class ToolGroupRuntimeIntegrationTests
{
    /// <summary>
    /// Tests that ReActAgent updates the system prompt based on which tool groups are active at runtime.
    /// 测试 ReActAgent 根据运行时激活的工具组动态更新系统提示。
    /// </summary>
    [Fact]
    public async Task ReActAgent_RuntimeToolGroups_UpdatePromptFromActiveTools()
    {
        var manager = new ToolGroupManager();
        var userGroup = new ToolGroup("user", isActive: true);
        userGroup.AddTool("allowed_tool");
        var adminGroup = new ToolGroup("admin", isActive: false);
        adminGroup.AddTool("blocked_tool");
        manager.RegisterGroup(userGroup);
        manager.RegisterGroup(adminGroup);

        var model = new CapturingScriptedModel(
            "Thought: 直接完成\nAction: finish\nAction Input: first",
            "Thought: 直接完成\nAction: finish\nAction Input: second");

        var agent = ReActAgent.Builder()
            .Name("GroupedReAct")
            .Model(model)
            .AddTool(new TrackingTool("allowed_tool", "允许工具"))
            .AddTool(new TrackingTool("blocked_tool", "禁用工具"))
            .ToolGroupManager(manager)
            .Build();

        await agent.CallAsync(Msg.Builder().Role("user").TextContent("first").Build());
        Assert.Contains("allowed_tool", model.CapturedPrompts[0], StringComparison.Ordinal);
        Assert.DoesNotContain("blocked_tool", model.CapturedPrompts[0], StringComparison.Ordinal);

        manager.DeactivateGroup("user");
        manager.ActivateGroup("admin");

        await agent.CallAsync(Msg.Builder().Role("user").TextContent("second").Build());
        Assert.DoesNotContain("allowed_tool", model.CapturedPrompts[1], StringComparison.Ordinal);
        Assert.Contains("blocked_tool", model.CapturedPrompts[1], StringComparison.Ordinal);
    }

    /// <summary>
    /// Tests that ReActAgent does not execute tools belonging to inactive groups.
    /// 测试 ReActAgent 不会执行属于非活跃组的工具。
    /// </summary>
    [Fact]
    public async Task ReActAgent_DoesNotExecuteInactiveGroupedTool()
    {
        var manager = new ToolGroupManager();
        var userGroup = new ToolGroup("user", isActive: true);
        userGroup.AddTool("allowed_tool");
        manager.RegisterGroup(userGroup);

        var allowedTool = new TrackingTool("allowed_tool", "允许工具");
        var blockedTool = new TrackingTool("blocked_tool", "禁用工具");
        var model = new CapturingScriptedModel("Thought: 尝试调用禁用工具\nAction: blocked_tool\nAction Input: {}");

        var agent = ReActAgent.Builder()
            .Name("GroupedReAct")
            .Model(model)
            .AddTool(allowedTool)
            .AddTool(blockedTool)
            .ToolGroupManager(manager)
            .Build();

        var response = await agent.CallAsync(Msg.Builder().Role("user").TextContent("test").Build());

        Assert.Equal(0, blockedTool.ExecutionCount);
        Assert.Equal(string.Empty, response.GetTextContent());
    }

    /// <summary>
    /// Tests that EnhancedReActAgent does not execute tools belonging to inactive groups and reports unknown action.
    /// 测试 EnhancedReActAgent 不会执行非活跃组的工具并报告未知操作。
    /// </summary>
    [Fact]
    public async Task EnhancedReActAgent_DoesNotExecuteInactiveGroupedTool()
    {
        var manager = new ToolGroupManager();
        var userGroup = new ToolGroup("user", isActive: true);
        userGroup.AddTool("allowed_tool");
        manager.RegisterGroup(userGroup);

        var allowedTool = new TrackingTool("allowed_tool", "允许工具");
        var blockedTool = new TrackingTool("blocked_tool", "禁用工具");
        var model = new CapturingScriptedModel("Thought: 尝试调用禁用工具\nAction: blocked_tool\nAction Input: {}");

        var agent = EnhancedReActAgent.Builder()
            .Name("GroupedEnhanced")
            .Model(model)
            .AddTool(allowedTool)
            .AddTool(blockedTool)
            .ToolGroupManager(manager)
            .Build();

        var response = await agent.CallAsync(Msg.Builder().Role("user").TextContent("test").Build());
        var text = response.GetTextContent();

        Assert.Equal(0, blockedTool.ExecutionCount);
        Assert.NotNull(text);
        Assert.Contains("Unknown action", text, StringComparison.Ordinal);
    }

    /// <summary>
    /// A test tool that tracks how many times it was executed.
    /// 跟踪执行次数的测试工具。
    /// </summary>
    private sealed class TrackingTool : ToolBase
    {
        public int ExecutionCount { get; private set; }

        public TrackingTool(string name, string description) : base(name, description)
        {
        }

        public override Task<ToolResult> ExecuteAsync(Dictionary<string, object> parameters)
        {
            _ = parameters;
            ExecutionCount++;
            return Task.FromResult(ToolResult.Ok($"executed:{Name}"));
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

    /// <summary>
    /// A scripted model that captures all prompts sent to it for verification.
    /// 捕获所有发送给它的提示以进行验证的脚本化模型。
    /// </summary>
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