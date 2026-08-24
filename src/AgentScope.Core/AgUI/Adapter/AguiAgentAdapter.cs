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

using AgentScope.Core.AgUI.Converter;
using AgentScope.Core.AgUI.Event;
using AgentScope.Core.AgUI.Model;
using AgentScope.Core.Agent;
using AgentScope.Core.Events;
using Evt = AgentScope.Core.Events.Event;

namespace AgentScope.Core.AgUI.Adapter;

/// <summary>
/// AgentScope → AG-UI protocol bridge adapter, converts AgentScope stream events into AG-UI protocol events.
/// AgentScope → AG-UI 协议桥接适配器，将 AgentScope 流事件转换为 AG-UI 协议事件。
/// Corresponds to Java: AguiAgentAdapter
/// </summary>
public sealed class AguiAgentAdapter
{
    /// <summary>
    /// The underlying AgentScope agent to be adapted.
    /// 底层 AgentScope Agent，将被适配。
    /// </summary>
    private readonly IAgent _agent;

    /// <summary>
    /// Configuration controlling adapter behavior.
    /// 控制适配器行为的配置。
    /// </summary>
    private readonly AguiAdapterConfig _config;

    /// <summary>
    /// Message converter for transforming between AG-UI and AgentScope message formats.
    /// 消息转换器，用于在 AG-UI 和 AgentScope 消息格式之间转换。
    /// </summary>
    private readonly AguiMessageConverter _msgConverter = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="AguiAgentAdapter"/> class.
    /// 初始化 <see cref="AguiAgentAdapter"/> 类的新实例。
    /// </summary>
    /// <param name="agent">The agent to adapt / 要适配的 Agent</param>
    /// <param name="config">Optional adapter configuration / 可选的适配器配置</param>
    public AguiAgentAdapter(IAgent agent, AguiAdapterConfig? config = null)
    {
        _agent = agent;
        _config = config ?? new AguiAdapterConfig();
    }

    /// <summary>
    /// Runs the agent with the given input and yields AG-UI events asynchronously.
    /// 使用给定的输入运行 Agent 并异步生成 AG-UI 事件。
    /// </summary>
    /// <param name="input">The run input containing messages and configuration / 包含消息和配置的运行输入</param>
    /// <returns>An async stream of AG-UI events / AG-UI 事件的异步流</returns>
    public async IAsyncEnumerable<AguiEvent> RunAsync(RunAgentInput input)
    {
        // 提取线程 ID 和运行 ID
        // Extract thread ID and run ID
        var t = input.ThreadId;
        var r = input.RunId;

        // 将 AG-UI 输入消息转换为 AgentScope 消息列表
        // Convert AG-UI input messages to AgentScope message list
        var msgs = _msgConverter.ToMsgList(input);

        // 发射运行开始事件
        // Emit run started event
        yield return new RunStarted(t, r, null, input);

        // 遍历 Agent 流事件并转换为 AG-UI 事件
        // Iterate through agent stream events and convert to AG-UI events
        await foreach (var evt in _agent.StreamEventsAsync(msgs))
        {
            foreach (var aguiEvent in ConvertEvent(evt, t, r))
                yield return aguiEvent;
        }

        // 发射运行结束事件
        // Emit run finished event
        yield return new RunFinished(t, r, new RunFinishedSuccessOutcome(null));
    }

    /// <summary>
    /// Converts an AgentScope <see cref="Evt"/> to zero or more <see cref="AguiEvent"/> instances.
    /// 将 AgentScope <see cref="Evt"/> 转换为零个或多个 <see cref="AguiEvent"/> 实例。
    /// </summary>
    /// <param name="evt">The source event / 源事件</param>
    /// <param name="t">Thread ID / 线程 ID</param>
    /// <param name="r">Run ID / 运行 ID</param>
    /// <returns>Converted AG-UI events / 转换后的 AG-UI 事件</returns>
    private IEnumerable<AguiEvent> ConvertEvent(Evt evt, string t, string r)
    {
        switch (evt.Type)
        {
            // ── 行动（Assistant 文本回复）事件 ──
            // Acting (assistant text reply) events
            case EventType.ActingStart:
                yield return new TextMessageStart(t, r, Guid.NewGuid().ToString(), "assistant");
                break;
            case EventType.ActingChunk:
                yield return new TextMessageContent(t, r, evt.Message?.GetTextContent() ?? "");
                break;
            case EventType.ActingFinish:
                yield return new TextMessageEnd(t, r);
                break;

            // ── 工具调用事件 ──
            // Tool call events
            case EventType.ToolCallStart:
                yield return new ToolCallStart(t, r, Guid.NewGuid().ToString(), evt.Message?.GetTextContent() ?? "tool");
                break;
            case EventType.ToolCallChunk:
                yield return new ToolCallArgs(t, r, evt.Message?.GetTextContent() ?? "");
                break;
            case EventType.ToolCallFinish:
                yield return new ToolCallEnd(t, r);
                break;

            // ── 推理/思考事件（仅在启用时发射）──
            // Reasoning/thinking events (only emitted when enabled)
            case EventType.ReasoningStart when _config.EnableReasoning:
                yield return new ReasoningStart(t, r);
                yield return new ReasoningMessageStart(t, r);
                break;
            case EventType.ReasoningChunk when _config.EnableReasoning:
                yield return new ReasoningMessageContent(t, r, evt.Message?.GetTextContent() ?? "");
                break;
            case EventType.ReasoningFinish when _config.EnableReasoning:
                yield return new ReasoningMessageEnd(t, r);
                yield return new ReasoningEnd(t, r);
                break;
        }
    }
}
