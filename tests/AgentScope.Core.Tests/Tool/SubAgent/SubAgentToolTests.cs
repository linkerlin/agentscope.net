// Copyright 2024-2026 the original author or authors.
// Licensed under the Apache License, Version 2.0

using System.Collections.Generic;
using AgentScope.Core.Agent;
using AgentScope.Core.Message;
using AgentScope.Core.Tool.SubAgent;
using Xunit;
using SessionStore = AgentScope.Core.Session.Session;

namespace AgentScope.Core.Tests.Tool.SubAgent;

/// <summary>
/// Tests for SubAgentTool
/// SubAgentTool 的测试
/// </summary>
public class SubAgentToolTests
{
    [Fact]
    /// <summary>
    /// ExecuteAsync returns fail when message parameter is missing
    /// 测试 ExecuteAsync 在缺少 message 参数时返回失败
    /// </summary>
    public async Task ExecuteAsync_MissingMessage_ReturnsFail()
    {
        var session = new SessionStore();
        var config = new SubAgentConfig(session);
        var provider = new EchoAgentProvider();
        var tool = new SubAgentTool(provider, config);
        var result = await tool.ExecuteAsync(new Dictionary<string, object>());
        Assert.False(result.Success);
        Assert.Contains("message", result.Error);
    }

    [Fact]
    /// <summary>
    /// ExecuteAsync creates a new session and returns session ID with response
    /// 测试 ExecuteAsync 创建新会话并返回 session ID 和响应
    /// </summary>
    public async Task ExecuteAsync_NewSession_ReturnsSessionIdAndResponse()
    {
        var session = new SessionStore();
        var config = new SubAgentConfig(session);
        var provider = new EchoAgentProvider();
        var tool = new SubAgentTool(provider, config);
        var result = await tool.ExecuteAsync(new Dictionary<string, object> { ["message"] = "hi" });
        Assert.True(result.Success);
        Assert.NotNull(result.Result);
        var dict = result.Result as Dictionary<string, object>;
        Assert.NotNull(dict);
        Assert.True(dict!.ContainsKey("session_id"));
        Assert.True(dict.ContainsKey("response"));
        Assert.Equal("hi", dict["response"]);
    }

    [Fact]
    /// <summary>
    /// ExecuteAsync continues conversation when same session ID is provided
    /// 测试 ExecuteAsync 在提供相同 session ID 时继续对话
    /// </summary>
    public async Task ExecuteAsync_SameSessionId_ContinuesConversation()
    {
        var session = new SessionStore();
        var config = new SubAgentConfig(session);
        var provider = new EchoAgentProvider();
        var tool = new SubAgentTool(provider, config);
        var r1 = await tool.ExecuteAsync(new Dictionary<string, object> { ["message"] = "first" });
        Assert.True(r1.Success);
        var sid = (r1.Result as Dictionary<string, object>)?["session_id"]?.ToString();
        Assert.NotNull(sid);
        var r2 = await tool.ExecuteAsync(new Dictionary<string, object> { ["message"] = "second", ["session_id"] = sid });
        Assert.True(r2.Success);
        Assert.Equal(sid, (r2.Result as Dictionary<string, object>)?["session_id"]?.ToString());
    }

    /// <summary>
    /// Echo agent provider for testing - returns an EchoAgent instance
    /// 用于测试的 Echo agent 提供者 - 返回 EchoAgent 实例
    /// </summary>
    private sealed class EchoAgentProvider : ISubAgentProvider
    {
        public IAgent Provide() => new EchoAgent();
    }

    /// <summary>
    /// Echo agent for testing - echoes back the input message content
    /// 用于测试的 Echo agent - 将输入消息内容原样返回
    /// </summary>
    private sealed class EchoAgent : AgentBase
    {
        public EchoAgent() : base("Echo") { }
        protected override Task<Msg> DoCallAsync(IReadOnlyList<Msg> messages)
        {
            var last = messages[^1];
            var text = last.GetTextContent() ?? "";
            var reply = Msg.Builder().Name("Echo").TextContent(text).Role("assistant").Build();
            return Task.FromResult(reply);
        }
    }
}
