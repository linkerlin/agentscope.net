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
using AgentScope.Core.State;

namespace AgentScope.Core.Tool.SubAgent;

/// <summary>
/// 将 Agent 封装为 Tool，支持多轮对话：session_id 保持会话，状态通过 IStateModule 持久化与恢复。
/// </summary>
public class SubAgentTool : ToolBase
{
    private readonly ISubAgentProvider _provider;
    private readonly SubAgentConfig _config;

    public SubAgentTool(ISubAgentProvider provider, SubAgentConfig config, string name = "sub_agent", string description = "调用子 Agent 进行多轮对话。参数: message(必填), session_id(可选，不传则新建会话)。")
        : base(name, description)
    {
        _provider = provider ?? throw new ArgumentNullException(nameof(provider));
        _config = config ?? throw new ArgumentNullException(nameof(config));
    }

    public override async Task<ToolResult> ExecuteAsync(Dictionary<string, object> parameters)
    {
        if (!parameters.TryGetValue("message", out var msgObj) || msgObj == null)
            return ToolResult.Fail("缺少必需参数: message");

        var message = msgObj.ToString() ?? "";
        var sessionId = parameters.TryGetValue("session_id", out var sidObj) ? sidObj?.ToString() : null;
        var isNewSession = string.IsNullOrWhiteSpace(sessionId);
        sessionId ??= Guid.NewGuid().ToString();

        var agent = _provider.Provide();
        if (agent == null)
            return ToolResult.Fail("SubAgent 提供者返回了空 Agent。");

        if (!isNewSession && agent is IStateModule stateModule)
            stateModule.LoadIfExists(_config.Session, sessionId);

        Msg response;
        try
        {
            response = await agent.CallAsync(Msg.Builder().TextContent(message).Build()).ConfigureAwait(false);
        }
        catch (System.Exception ex)
        {
            return ToolResult.Fail("子 Agent 调用异常: " + ex.Message);
        }

        if (agent is IStateModule sm)
            sm.SaveTo(_config.Session, sessionId);

        var text = response?.GetTextContent() ?? "";
        var result = new Dictionary<string, object> { ["session_id"] = sessionId, ["response"] = text };
        return ToolResult.Ok(result);
    }

    public override Dictionary<string, object> GetSchema()
    {
        return new Dictionary<string, object>
        {
            ["name"] = Name,
            ["description"] = Description,
            ["parameters"] = new Dictionary<string, object>
            {
                ["message"] = new Dictionary<string, object> { ["type"] = "string", ["description"] = "发送给子 Agent 的消息", ["required"] = true },
                ["session_id"] = new Dictionary<string, object> { ["type"] = "string", ["description"] = "会话 ID，不传则新建", ["required"] = false }
            }
        };
    }
}
