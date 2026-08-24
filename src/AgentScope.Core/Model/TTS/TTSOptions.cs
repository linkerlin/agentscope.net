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
/// Options for TTS (Text-to-Speech) synthesis.
/// Configures voice, speed, and output format for speech generation.
/// Corresponds to Java: io.agentscope.core.model.tts.TTSOptions
/// TTS 合成选项：配置语音合成的音色、语速和输出格式。
/// 对应 Java: io.agentscope.core.model.tts.TTSOptions
/// </summary>
public class TTSOptions
{
    /// <summary>
    /// Gets or sets the voice identifier (e.g., "alloy", "echo", "fable", "onyx", "nova", "shimmer").
    /// Default is "default".
    /// 获取或设置音色标识（例如 "alloy"、"echo"、"fable"、"onyx"、"nova"、"shimmer"）。
    /// 默认为 "default"。
    /// </summary>
    public string Voice { get; set; } = "default";

    /// <summary>
    /// Gets or sets the speech speed multiplier (0.25 to 4.0, default 1.0).
    /// A value of 1.0 represents normal speed; 2.0 is twice as fast.
    /// 获取或设置语速倍率（0.25 到 4.0，默认 1.0）。
    /// 1.0 表示正常语速，2.0 表示两倍速。
    /// </summary>
    public double Speed { get; set; } = 1.0;

    /// <summary>
    /// Gets or sets the response audio format (e.g., "mp3", "wav", "pcm", "opus", "aac", "flac").
    /// Default is "mp3".
    /// 获取或设置响应音频格式（例如 "mp3"、"wav"、"pcm"、"opus"、"aac"、"flac"）。
    /// 默认为 "mp3"。
    /// </summary>
    public string ResponseFormat { get; set; } = "mp3";
}
