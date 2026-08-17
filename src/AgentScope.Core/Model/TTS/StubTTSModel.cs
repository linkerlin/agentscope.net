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

namespace AgentScope.Core.Model.TTS;

/// <summary>
/// Stub TTS implementation that returns empty audio.
/// Used for testing or when no real TTS model is configured.
/// 占位 TTS 实现：返回空音频，用于测试或未配置真实 TTS 时。
/// </summary>
public class StubTTSModel : ITTSModel
{
    /// <summary>
    /// Gets the model name, always "stub-tts".
    /// 获取模型名称，始终为 "stub-tts"。
    /// </summary>
    public string ModelName { get; } = "stub-tts";

    /// <summary>
    /// Synthesizes speech by returning an empty audio response.
    /// 通过返回空音频响应来合成语音。
    /// </summary>
    /// <param name="text">The text to synthesize (ignored) / 要合成的文本（忽略）</param>
    /// <param name="options">Optional TTS options for response format / 可选的 TTS 选项，指定响应格式</param>
    /// <param name="cancellationToken">Cancellation token / 取消令牌</param>
    /// <returns>A task with an empty TTS response / 返回空 TTS 响应的任务</returns>
    public Task<TTSResponse> SynthesizeAsync(string text, TTSOptions? options = null, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new TTSResponse { Format = options?.ResponseFormat ?? "mp3", AudioData = Array.Empty<byte>() });
    }
}
