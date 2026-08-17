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
/// TTS model interface for text-to-speech synthesis.
/// TTS 模型接口：文本转语音合成。
/// </summary>
public interface ITTSModel
{
    /// <summary>
    /// Gets the name of the TTS model.
    /// 获取 TTS 模型的名称。
    /// </summary>
    string ModelName { get; }

    /// <summary>
    /// Synthesizes speech from text asynchronously.
    /// 异步地从文本合成语音。
    /// </summary>
    /// <param name="text">The text to synthesize / 要合成的文本</param>
    /// <param name="options">Optional TTS synthesis options / 可选的 TTS 合成选项</param>
    /// <param name="cancellationToken">Cancellation token for the async operation / 异步操作的取消令牌</param>
    /// <returns>A task representing the async operation, with a <see cref="TTSResponse"/> result / 表示异步操作的任务，返回 TTSResponse</returns>
    Task<TTSResponse> SynthesizeAsync(string text, TTSOptions? options = null, CancellationToken cancellationToken = default);
}
