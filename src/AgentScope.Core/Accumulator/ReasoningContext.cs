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

using AgentScope.Core.Message;

namespace AgentScope.Core.Accumulator;

/// <summary>
/// Reasoning context: aggregates text/thinking/tool-call accumulators, processes streaming chunks, and builds the final message.
/// 推理上下文：聚合文本/思考/工具调用累加器，处理流式块并构建最终消息。
/// </summary>
public class ReasoningContext
{
    private readonly TextAccumulator _textAccumulator = new();
    private readonly ThinkingAccumulator _thinkingAccumulator = new();
    private readonly ToolCallsAccumulator _toolCallsAccumulator = new();

    /// <summary>
    /// Accumulated text content.
    /// 已累积的文本。
    /// </summary>
    public string AccumulatedText => _textAccumulator.GetText();

    /// <summary>
    /// Accumulated thinking content.
    /// 已累积的思考内容。
    /// </summary>
    public string AccumulatedThinking => _thinkingAccumulator.GetThinking();

    /// <summary>
    /// Processes a content block and dispatches it to the corresponding accumulator.
    /// 处理一个内容块并分发到对应累加器。
    /// </summary>
    public void ProcessChunk(ContentBlock block)
    {
        _textAccumulator.Accumulate(block);
        _thinkingAccumulator.Accumulate(block);
        _toolCallsAccumulator.Accumulate(block);
    }

    /// <summary>
    /// Builds an assistant message from the accumulated content (text or ContentBlock list).
    /// 根据当前累积内容构建一条助手消息（Content 为文本或 ContentBlock 列表）。
    /// </summary>
    public Msg BuildFinalMessage()
    {
        var text = _textAccumulator.GetText();
        var blocks = new List<ContentBlock>();
        if (_textAccumulator.GetAccumulated() is ContentBlock tb) blocks.Add(tb);
        if (_thinkingAccumulator.GetAccumulated() is ContentBlock th) blocks.Add(th);
        foreach (var b in _toolCallsAccumulator.GetBlocks()) blocks.Add(b);
        // If there are no blocks, fall back to plain text; otherwise use the block list
        // 如果没有块则回退为纯文本，否则使用块列表
        var content = blocks.Count == 0 ? (object?)(text ?? "") : (object)blocks;
        if (content is List<ContentBlock> list && list.Count == 0)
            content = text ?? "";
        return new Msg(null, content, "assistant");
    }

    /// <summary>
    /// Resets all accumulators to their initial state.
    /// 重置所有累加器。
    /// </summary>
    public void Reset()
    {
        _textAccumulator.Reset();
        _thinkingAccumulator.Reset();
        _toolCallsAccumulator.Reset();
    }
}
