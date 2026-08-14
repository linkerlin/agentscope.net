using System.Diagnostics;
using AgentScope.Core.Events;

namespace AgentScope.Tracing.OpenTelemetry;

/// <summary>
/// 流式响应聚合器。对标 Java StreamChatResponseAggregator。
/// 在流式事件序列结束后产出完整 span 属性。
/// </summary>
public sealed class StreamingAggregator
{
    private string? _responseId;
    private string? _responseModel;
    private int _inputTokens;
    private int _outputTokens;
    private readonly DateTime _startTime = DateTime.UtcNow;

    public void Accumulate(Event evt)
    {
        if (evt.Message?.Metadata != null)
        {
            if (evt.Message.Metadata.TryGetValue("response_id", out var rid))
                _responseId = rid?.ToString();
            if (evt.Message.Metadata.TryGetValue("model", out var m))
                _responseModel = m?.ToString();
            if (evt.Message.Metadata.TryGetValue("input_tokens", out var it))
                _inputTokens = Convert.ToInt32(it);
            if (evt.Message.Metadata.TryGetValue("output_tokens", out var ot))
                _outputTokens = Convert.ToInt32(ot);
        }
    }

    public void ApplyTo(Activity activity)
    {
        if (_responseId != null)
            activity?.SetTag(GenAiAttributes.ResponseId, _responseId);
        if (_responseModel != null)
            activity?.SetTag(GenAiAttributes.ResponseModel, _responseModel);
        if (_inputTokens > 0)
            activity?.SetTag(GenAiAttributes.UsageInputTokens, _inputTokens);
        if (_outputTokens > 0)
            activity?.SetTag(GenAiAttributes.UsageOutputTokens, _outputTokens);
    }
}
