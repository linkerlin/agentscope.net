// Copyright 2024-2026 the original author or authors.
// Licensed under the Apache License, Version 2.0

namespace AgentScope.Core.Model;

/// <summary>
/// 结构化输出提示类型，用于在生成时提醒模型使用工具或系统提示等。
/// </summary>
public class StructuredOutputReminder
{
    public string Kind { get; }

    private StructuredOutputReminder(string kind)
    {
        Kind = kind;
    }

    public static StructuredOutputReminder ToolChoice { get; } = new("tool_choice");
    public static StructuredOutputReminder SystemPrompt { get; } = new("system_prompt");
}
