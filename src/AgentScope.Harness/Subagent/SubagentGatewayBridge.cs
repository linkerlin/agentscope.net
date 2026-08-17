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

using AgentScope.Core.Agent;
using AgentScope.Core.Message;
using AgentScope.Harness.Gateway;

namespace AgentScope.Harness.Subagent;

/// <summary>
/// Subagent gateway bridge. Connects the Gateway and Subagent systems with session-level isolation.
/// 子 Agent 网关桥接，连接 Gateway 和 Subagent 系统，支持会话级隔离。
/// </summary>
public sealed class SubagentGatewayBridge
{
    private readonly IGateway _gateway;
    private readonly ISubagentManager _subagentManager;

    /// <summary>
    /// Initializes a new SubagentGatewayBridge.
    /// 初始化子 Agent 网关桥接。
    /// </summary>
    /// <param name="gateway">The gateway instance / 网关实例</param>
    /// <param name="subagentManager">The subagent manager / 子 Agent 管理器</param>
    public SubagentGatewayBridge(IGateway gateway, ISubagentManager subagentManager)
    {
        _gateway = gateway ?? throw new ArgumentNullException(nameof(gateway));
        _subagentManager = subagentManager ?? throw new ArgumentNullException(nameof(subagentManager));
    }

    /// <summary>
    /// Routes a message to the specified subagent via the manager.
    /// 将消息路由到指定子 Agent，通过管理器执行。
    /// </summary>
    /// <param name="subagentName">Target subagent name / 目标子 Agent 名称</param>
    /// <param name="input">Input message / 输入消息</param>
    /// <param name="context">Optional runtime context / 可选运行时上下文</param>
    /// <param name="ct">Cancellation token / 取消令牌</param>
    /// <returns>The agent response / Agent 响应</returns>
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
    /// Routes a message through the gateway to a subagent.
    /// 通过 Gateway 将消息路由到子 Agent。
    /// </summary>
    /// <param name="input">Input message / 输入消息</param>
    /// <param name="context">Optional runtime context / 可选运行时上下文</param>
    /// <param name="ct">Cancellation token / 取消令牌</param>
    /// <returns>The gateway response / 网关响应</returns>
    public async Task<Msg> RouteThroughGatewayAsync(
        Msg input,
        RuntimeContext? context = null,
        CancellationToken ct = default)
    {
        return await _gateway.RunAsync(input, context, ct);
    }

    /// <summary>
    /// Registers a Gateway-hosted agent in the subagent manager.
    /// 在子 Agent 管理器中注册一个由 Gateway 托管的 Agent。
    /// </summary>
    /// <param name="name">Agent name / Agent 名称</param>
    /// <param name="agent">The agent instance / Agent 实例</param>
    public void RegisterFromGateway(string name, IAgent agent)
    {
        _subagentManager.Register(name, agent);
    }

    /// <summary>
    /// Removes an agent from the subagent manager by name.
    /// 从子 Agent 管理器中移除指定 Agent。
    /// </summary>
    /// <param name="name">Agent name / Agent 名称</param>
    public void RemoveFromGateway(string name)
    {
        _subagentManager.Remove(name);
    }
}
