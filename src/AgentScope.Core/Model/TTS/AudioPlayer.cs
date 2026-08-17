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
/// Audio player for playing TTS output audio data.
/// Default implementation is a stub; platform-specific implementations can be injected.
/// 音频播放器：播放 TTS 输出的音频数据（默认实现仅占位，具体平台可注入）。
/// </summary>
public class AudioPlayer
{
    /// <summary>
    /// Plays audio data synchronously.
    /// 同步播放音频数据。
    /// </summary>
    /// <param name="audioData">Raw audio data bytes / 原始音频数据字节</param>
    public virtual void Play(byte[] audioData)
    {
        // Placeholder: actual implementation can use NAudio, CSCore, or platform API
        // 占位：实际实现可依赖 NAudio、CSCore 或平台 API
    }

    /// <summary>
    /// Plays audio data asynchronously.
    /// 异步播放音频数据。
    /// </summary>
    /// <param name="audioData">Raw audio data bytes / 原始音频数据字节</param>
    /// <returns>A task representing the async operation / 表示异步操作的任务</returns>
    public virtual Task PlayAsync(byte[] audioData)
    {
        Play(audioData);
        return Task.CompletedTask;
    }
}
