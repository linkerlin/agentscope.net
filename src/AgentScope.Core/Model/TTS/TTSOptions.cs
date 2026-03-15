// Copyright 2024-2026 the original author or authors.
// Licensed under the Apache License, Version 2.0

namespace AgentScope.Core.Model.TTS;

/// <summary>
/// TTS 合成选项：音色、语速、输出格式等。
/// </summary>
public class TTSOptions
{
    public string Voice { get; set; } = "default";
    public double Speed { get; set; } = 1.0;
    public string ResponseFormat { get; set; } = "mp3";
}
