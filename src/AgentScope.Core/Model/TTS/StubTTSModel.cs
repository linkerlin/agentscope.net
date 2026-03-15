// Copyright 2024-2026 the original author or authors.
// Licensed under the Apache License, Version 2.0

namespace AgentScope.Core.Model.TTS;

/// <summary>
/// 占位 TTS 实现：返回空音频，用于测试或未配置真实 TTS 时。
/// </summary>
public class StubTTSModel : ITTSModel
{
    public string ModelName { get; } = "stub-tts";

    public Task<TTSResponse> SynthesizeAsync(string text, TTSOptions? options = null, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new TTSResponse { Format = options?.ResponseFormat ?? "mp3", AudioData = Array.Empty<byte>() });
    }
}
