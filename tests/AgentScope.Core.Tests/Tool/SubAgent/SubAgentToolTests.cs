// Copyright 2024-2026 the original author or authors.
// Licensed under the Apache License, Version 2.0

using AgentScope.Core.Agent;
using AgentScope.Core.Message;
using AgentScope.Core.Tool.SubAgent;
using Xunit;
using SessionStore = AgentScope.Core.Session.Session;

namespace AgentScope.Core.Tests.Tool.SubAgent;

public class SubAgentToolTests
{
    [Fact]
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

    private sealed class EchoAgentProvider : ISubAgentProvider
    {
        public IAgent Provide() => new EchoAgent();
    }

    private sealed class EchoAgent : AgentBase
    {
        public EchoAgent() : base("Echo") { }
        public override IObservable<Msg> Call(Msg message)
        {
            var text = message.GetTextContent() ?? "";
            var reply = Msg.Builder().Name("Echo").TextContent(text).Role("assistant").Build();
            return System.Reactive.Linq.Observable.Return(reply);
        }
    }
}
