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
/// TTS synthesis options: voice, speed, output format, etc.
/// TTS 合成选项：音色、语速、输出格式等。
/// </summary>
public class TTSOptions
{
    /// <summary>
    /// Voice identifier. Default is "default".
    /// 音色标识。默认为 "default"。
    /// </summary>
    public string Voice { get; set; } = "default";

    /// <summary>
    /// Speech speed multiplier. 1.0 is normal speed.
    /// 语速倍率。1.0 表示正常语速。
    /// </summary>
    public double Speed { get; set; } = 1.0;

    /// <summary>
    /// Response audio format, e.g. "mp3", "wav", "pcm". Default is "mp3".
    /// 响应音频格式，例如 "mp3"、"wav"、"pcm"。默认为 "mp3"。
    /// </summary>
    public string ResponseFormat { get; set; } = "mp3";
}
