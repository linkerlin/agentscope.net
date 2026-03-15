// Copyright 2024-2026 the original author or authors.
// Licensed under the Apache License, Version 2.0

namespace AgentScope.Core.Model.TTS;

/// <summary>
/// 实时 TTS 模型接口：支持流式语音合成。
/// </summary>
public interface IRealtimeTTSModel : ITTSModel
{
    IAsyncEnumerable<TTSResponse> StreamSynthesizeAsync(string text, TTSOptions? options = null, CancellationToken cancellationToken = default);
}
