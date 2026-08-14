// Copyright 2024-2026 the original author or authors.
// Licensed under the Apache License, Version 2.0

using System.Threading.Tasks;
using AgentScope.Core.Message;

namespace AgentScope.Core.Agent.User;

/// <summary>
/// 用户交互 Agent：每轮向用户索取输入，再包装为 UserMessage
/// 对应 Java: io.agentscope.core.agent.user.UserAgent
/// </summary>
public class UserAgent
{
    private readonly IUserInput _input;
    public string Name { get; }

    public UserAgent(string name, IUserInput input)
    {
        Name = name;
        _input = input;
    }

    public async Task<Msg> ReplyAsync(string prompt = "You> ")
    {
        var text = await _input.RequestAsync(prompt);
        return Msg.Builder().Name(Name).Role("user").TextContent(text).Build();
    }
}
