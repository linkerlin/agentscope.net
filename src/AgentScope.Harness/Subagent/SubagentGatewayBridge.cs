// Copyright 2024-2026 the original author or authors.
// Licensed under the Apache License, Version 2.0

using AgentScope.Core.Agent;
using AgentScope.Core.Message;
using AgentScope.Harness.Gateway;

namespace AgentScope.Harness.Subagent;

/// <summary>
/// 子 Agent 网关桥接，连接 Gateway 和 Subagent 系统。
/// 对标 Java SubagentGatewayBridge。
/// 负责将 Gateway 路由到对应的子 Agent 管理器，支持会话级隔离。
/// </summary>
public sealed class SubagentGatewayBridge
{
    private readonly IGateway _gateway;
    private readonly ISubagentManager _subagentManager;

    public SubagentGatewayBridge(IGateway gateway, ISubagentManager subagentManager)
    {
        _gateway = gateway ?? throw new ArgumentNullException(nameof(gateway));
        _subagentManager = subagentManager ?? throw new ArgumentNullException(nameof(subagentManager));
    }

    /// <summary>
    /// 将消息路由到指定子 Agent，通过 Gateway 执行。
    /// </summary>
    public async Task<Msg> RouteToSubagentAsync(
        string subagentName,
        Msg input,
        RuntimeContext? context = null,
        CancellationToken ct = default)
    {
        var agent = _subagentManager.GetOrCreate(subagentName);

        var bridgedMsg = new Msg
        {
            Role = "user",
            Content = input.Content,
            Metadata = input.Metadata,
        };

        return await agent.CallAsync(bridgedMsg, context);
    }

    /// <summary>
    /// 通过 Gateway 将消息路由到子 Agent。
    /// </summary>
    public async Task<Msg> RouteThroughGatewayAsync(
        Msg input,
        RuntimeContext? context = null,
        CancellationToken ct = default)
    {
        return await _gateway.RunAsync(input, context, ct);
    }

    /// <summary>
    /// 在子 Agent 管理器中注册一个由 Gateway 托管的 Agent。
    /// </summary>
    public void RegisterFromGateway(string name, IAgent agent)
    {
        _subagentManager.Register(name, agent);
    }

    /// <summary>
    /// 从子 Agent 管理器中移除指定 Agent。
    /// </summary>
    public void RemoveFromGateway(string name)
    {
        _subagentManager.Remove(name);
    }
}
