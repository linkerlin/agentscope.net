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
/// TTS synthesis response containing audio data or URL.
/// Either AudioData (inline bytes) or AudioUrl (remote reference) is populated, depending on the TTS provider.
/// Corresponds to Java: io.agentscope.core.model.tts.TTSResponse
/// TTS 合成响应：包含音频数据或 URL。
/// 根据 TTS 提供商的不同，AudioData（内联字节）或 AudioUrl（远程引用）会被填充。
/// 对应 Java: io.agentscope.core.model.tts.TTSResponse
/// </summary>
public class TTSResponse
{
    /// <summary>
    /// Gets or sets the raw audio data bytes. Populated when audio is returned inline.
    /// 获取或设置原始音频数据字节。当音频内联返回时填充。
    /// </summary>
    public byte[]? AudioData { get; set; }

    /// <summary>
    /// Gets or sets the URL pointing to the synthesized audio file.
    /// Used when the TTS provider returns a downloadable URL instead of inline data.
    /// 获取或设置指向合成音频文件的 URL。
    /// 当 TTS 提供商返回可下载的 URL 而非内联数据时使用。
    /// </summary>
    public string? AudioUrl { get; set; }

    /// <summary>
    /// Gets or sets the audio format identifier (e.g., "mp3", "wav", "pcm", "opus", "aac", "flac").
    /// 获取或设置音频格式标识（例如 "mp3"、"wav"、"pcm"、"opus"、"aac"、"flac"）。
    /// </summary>
    public string? Format { get; set; }
}
