using AgentScope.Core.AgUI.Converter;
using AgentScope.Core.AgUI.Event;
using AgentScope.Core.AgUI.Model;
using AgentScope.Core.Agent;
using AgentScope.Core.Events;
using Evt = AgentScope.Core.Events.Event;

namespace AgentScope.Core.AgUI.Adapter;

/// <summary>
/// AgentScope → AG-UI 协议桥接适配器。对标 Java AguiAgentAdapter。
/// </summary>
public sealed class AguiAgentAdapter
{
    private readonly IAgent _agent;
    private readonly AguiAdapterConfig _config;
    private readonly AguiMessageConverter _msgConverter = new();

    public AguiAgentAdapter(IAgent agent, AguiAdapterConfig? config = null)
    {
        _agent = agent;
        _config = config ?? new AguiAdapterConfig();
    }

    public async IAsyncEnumerable<AguiEvent> RunAsync(RunAgentInput input)
    {
        var t = input.ThreadId;
        var r = input.RunId;
        var msgs = _msgConverter.ToMsgList(input);

        yield return new RunStarted(t, r, null, input);

        await foreach (var evt in _agent.StreamEventsAsync(msgs))
        {
            foreach (var aguiEvent in ConvertEvent(evt, t, r))
                yield return aguiEvent;
        }

        yield return new RunFinished(t, r, new RunFinishedSuccessOutcome(null));
    }

    private IEnumerable<AguiEvent> ConvertEvent(Evt evt, string t, string r)
    {
        switch (evt.Type)
        {
            case EventType.ActingStart:
                yield return new TextMessageStart(t, r, Guid.NewGuid().ToString(), "assistant");
                break;
            case EventType.ActingChunk:
                yield return new TextMessageContent(t, r, evt.Message?.GetTextContent() ?? "");
                break;
            case EventType.ActingFinish:
                yield return new TextMessageEnd(t, r);
                break;
            case EventType.ToolCallStart:
                yield return new ToolCallStart(t, r, Guid.NewGuid().ToString(), evt.Message?.GetTextContent() ?? "tool");
                break;
            case EventType.ToolCallChunk:
                yield return new ToolCallArgs(t, r, evt.Message?.GetTextContent() ?? "");
                break;
            case EventType.ToolCallFinish:
                yield return new ToolCallEnd(t, r);
                break;
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
