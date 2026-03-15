// Copyright 2024-2026 the original author or authors.
// Licensed under the Apache License, Version 2.0

namespace AgentScope.Core.Model.TTS;

/// <summary>
/// TTS 合成响应：音频数据或 URL。
/// </summary>
public class TTSResponse
{
    public byte[]? AudioData { get; set; }
    public string? AudioUrl { get; set; }
    public string? Format { get; set; }
}
