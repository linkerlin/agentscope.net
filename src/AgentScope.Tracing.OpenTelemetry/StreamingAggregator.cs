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

using System.Diagnostics;
using AgentScope.Core.Events;

namespace AgentScope.Tracing.OpenTelemetry;

/// <summary>
/// 流式响应聚合器。对标 Java StreamChatResponseAggregator。
/// 在流式事件序列结束后产出完整 span 属性。
/// </summary>
public sealed class StreamingAggregator
{
    /// <summary>
    /// 响应 ID
    /// Response ID
    /// </summary>
    private string? _responseId;

    /// <summary>
    /// 响应模型名称
    /// Response model name
    /// </summary>
    private string? _responseModel;

    /// <summary>
    /// 输入令牌数
    /// Input tokens count
    /// </summary>
    private int _inputTokens;

    /// <summary>
    /// 输出令牌数
    /// Output tokens count
    /// </summary>
    private int _outputTokens;

    /// <summary>
    /// 聚合开始时间（UTC）
    /// Aggregation start time (UTC)
    /// </summary>
    private readonly DateTime _startTime = DateTime.UtcNow;

    /// <summary>
    /// 从流式事件中累积属性数据（响应 ID、模型名、令牌用量等）
    /// Accumulates attribute data from streaming events (response ID, model name, token usage, etc.)
    /// </summary>
    /// <param name="evt">流式事件 / Streaming event</param>
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

    /// <summary>
    /// 将累积的属性应用到 Activity span 上
    /// Applies accumulated attributes to the Activity span
    /// </summary>
    /// <param name="activity">要标记的 Activity 实例 / The Activity instance to tag</param>
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
