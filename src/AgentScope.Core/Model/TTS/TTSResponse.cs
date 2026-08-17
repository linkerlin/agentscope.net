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
/// TTS 合成响应：音频数据或 URL。
/// </summary>
public class TTSResponse
{
    /// <summary>
    /// Raw audio data bytes. Populated when audio is returned inline.
    /// 原始音频数据字节。当音频内联返回时填充。
    /// </summary>
    public byte[]? AudioData { get; set; }

    /// <summary>
    /// URL pointing to the synthesized audio file.
    /// 指向合成音频文件的 URL。
    /// </summary>
    public string? AudioUrl { get; set; }

    /// <summary>
    /// Audio format identifier, e.g. "mp3", "wav", "pcm".
    /// 音频格式标识，例如 "mp3"、"wav"、"pcm"。
    /// </summary>
    public string? Format { get; set; }
}
