// Copyright 2024-2026 the original author or authors.
// Licensed under the Apache License, Version 2.0

using AgentScope.Core.Model.TTS;
using Xunit;

namespace AgentScope.Core.Tests.Model.TTS;

/// <summary>
/// Tests for TTS (Text-to-Speech) types: <see cref="TTSOptions"/>, <see cref="TTSResponse"/>, <see cref="StubTTSModel"/>, and <see cref="AudioPlayer"/>.
/// 对 TTS（文本转语音）类型的测试：TTSOptions、TTSResponse、StubTTSModel 和 AudioPlayer。
/// </summary>
public class TTSTests
{
    [Fact]
    /// <summary>
    /// Tests that <see cref="TTSOptions"/> has correct default values.
    /// 测试 TTSOptions 具有正确的默认值。
    /// </summary>
    public void TTSOptions_DefaultValues()
    {
        var opt = new TTSOptions();
        Assert.Equal("default", opt.Voice);
        Assert.Equal(1.0, opt.Speed);
        Assert.Equal("mp3", opt.ResponseFormat);
    }

    [Fact]
    /// <summary>
    /// Tests that <see cref="TTSResponse"/> properties can be set.
    /// 测试 TTSResponse 的属性可以被设置。
    /// </summary>
    public void TTSResponse_CanSetAudioData()
    {
        var r = new TTSResponse { AudioData = new byte[] { 1, 2, 3 }, Format = "mp3" };
        Assert.NotNull(r.AudioData);
        Assert.Equal(3, r.AudioData.Length);
    }

    [Fact]
    /// <summary>
    /// Tests that <see cref="StubTTSModel"/> synthesizes and returns an empty audio buffer.
    /// 测试 StubTTSModel 合成并返回空的音频缓冲区。
    /// </summary>
    public async Task StubTTSModel_SynthesizeAsync_ReturnsEmptyAudio()
    {
        var model = new StubTTSModel();
        Assert.Equal("stub-tts", model.ModelName);
        var resp = await model.SynthesizeAsync("hello");
        Assert.NotNull(resp);
        Assert.NotNull(resp.AudioData);
        Assert.Empty(resp.AudioData);
    }

    [Fact]
    /// <summary>
    /// Tests that <see cref="AudioPlayer.Play"/> does not throw when playing an empty buffer.
    /// 测试 AudioPlayer.Play 播放空缓冲区时不抛出异常。
    /// </summary>
    public void AudioPlayer_Play_DoesNotThrow()
    {
        var player = new AudioPlayer();
        player.Play(Array.Empty<byte>());
    }

    [Fact]
    /// <summary>
    /// Tests that <see cref="AudioPlayer.PlayAsync"/> completes without error.
    /// 测试 AudioPlayer.PlayAsync 无错误地完成。
    /// </summary>
    public async Task AudioPlayer_PlayAsync_Completes()
    {
        var player = new AudioPlayer();
        await player.PlayAsync(Array.Empty<byte>());
    }
}
