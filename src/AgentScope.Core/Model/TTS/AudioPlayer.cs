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
/// Default implementation is a stub; platform-specific implementations can be injected
/// using libraries such as NAudio, CSCore, or platform-native APIs.
/// Corresponds to Java: io.agentscope.core.model.tts.AudioPlayer
/// 音频播放器：播放 TTS 输出的音频数据。
/// 默认实现为占位，具体平台实现可注入（如 NAudio、CSCore 或平台原生 API）。
/// 对应 Java: io.agentscope.core.model.tts.AudioPlayer
/// </summary>
public class AudioPlayer
{
    /// <summary>
    /// Plays audio data synchronously. Override in derived classes for actual playback.
    /// 同步播放音频数据。在派生类中重写以实现实际播放。
    /// </summary>
    /// <param name="audioData">Raw audio data bytes (e.g., MP3 or WAV) / 原始音频数据字节（如 MP3 或 WAV）</param>
    public virtual void Play(byte[] audioData)
    {
        // Placeholder: actual implementation can use NAudio, CSCore, or platform API
        // 占位：实际实现可依赖 NAudio、CSCore 或平台 API
    }

    /// <summary>
    /// Plays audio data asynchronously. Default implementation calls the synchronous Play method.
    /// Override for true async playback to avoid blocking the calling thread.
    /// 异步播放音频数据。默认实现调用同步的 Play 方法。
    /// 重写以实现真正的异步播放，避免阻塞调用线程。
    /// </summary>
    /// <param name="audioData">Raw audio data bytes (e.g., MP3 or WAV) / 原始音频数据字节（如 MP3 或 WAV）</param>
    /// <returns>A task representing the async operation / 表示异步操作的任务</returns>
    public virtual Task PlayAsync(byte[] audioData)
    {
        Play(audioData);
        return Task.CompletedTask;
    }
}
