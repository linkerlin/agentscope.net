// Copyright 2024-2026 the original author or authors.
// Licensed under the Apache License, Version 2.0

namespace AgentScope.Core.Model.TTS;

/// <summary>
/// TTS 模型接口：文本转语音合成。
/// </summary>
public interface ITTSModel
{
    string ModelName { get; }
    Task<TTSResponse> SynthesizeAsync(string text, TTSOptions? options = null, CancellationToken cancellationToken = default);
}
