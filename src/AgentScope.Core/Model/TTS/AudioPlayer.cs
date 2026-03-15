// Copyright 2024-2026 the original author or authors.
// Licensed under the Apache License, Version 2.0

namespace AgentScope.Core.Model.TTS;

/// <summary>
/// 音频播放器：播放 TTS 输出的音频数据（默认实现仅占位，具体平台可注入）。
/// </summary>
public class AudioPlayer
{
    /// <summary>同步播放</summary>
    public virtual void Play(byte[] audioData)
    {
        // 占位：实际实现可依赖 NAudio、CSCore 或平台 API
    }

    /// <summary>异步播放</summary>
    public virtual Task PlayAsync(byte[] audioData)
    {
        Play(audioData);
        return Task.CompletedTask;
    }
}
