// Copyright 2024-2026 the original author or authors.
// Licensed under the Apache License, Version 2.0

using AgentScope.Core.Model.TTS;
using Xunit;

namespace AgentScope.Core.Tests.Model.TTS;

public class TTSTests
{
    [Fact]
    public void TTSOptions_DefaultValues()
    {
        var opt = new TTSOptions();
        Assert.Equal("default", opt.Voice);
        Assert.Equal(1.0, opt.Speed);
        Assert.Equal("mp3", opt.ResponseFormat);
    }

    [Fact]
    public void TTSResponse_CanSetAudioData()
    {
        var r = new TTSResponse { AudioData = new byte[] { 1, 2, 3 }, Format = "mp3" };
        Assert.NotNull(r.AudioData);
        Assert.Equal(3, r.AudioData.Length);
    }

    [Fact]
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
    public void AudioPlayer_Play_DoesNotThrow()
    {
        var player = new AudioPlayer();
        player.Play(Array.Empty<byte>());
    }

    [Fact]
    public async Task AudioPlayer_PlayAsync_Completes()
    {
        var player = new AudioPlayer();
        await player.PlayAsync(Array.Empty<byte>());
    }
}
