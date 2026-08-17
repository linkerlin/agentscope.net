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
/// Real-time TTS model interface supporting streaming speech synthesis.
/// 实时 TTS 模型接口：支持流式语音合成。
/// </summary>
public interface IRealtimeTTSModel : ITTSModel
{
    /// <summary>
    /// Synthesizes speech from text as an async stream of audio chunks.
    /// 从文本合成语音，以异步流的形式返回音频片段。
    /// </summary>
    /// <param name="text">The text to synthesize / 要合成的文本</param>
    /// <param name="options">Optional TTS synthesis options / 可选的 TTS 合成选项</param>
    /// <param name="cancellationToken">Cancellation token for the async operation / 异步操作的取消令牌</param>
    /// <returns>An async enumerable of TTS response chunks / TTS 响应片段的异步可枚举序列</returns>
    IAsyncEnumerable<TTSResponse> StreamSynthesizeAsync(string text, TTSOptions? options = null, CancellationToken cancellationToken = default);
}
