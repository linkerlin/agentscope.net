// Copyright 2024-2026 the original author or authors.
// Licensed under the Apache License, Version 2.0

namespace AgentScope.Core.Tool.Coding;

/// <summary>
/// 命令校验器：白名单/黑名单等，用于 ShellCommandTool 安全执行。
/// </summary>
public interface ICommandValidator
{
    /// <summary>
    /// 校验命令是否允许执行。
    /// </summary>
    bool Validate(string command);
}
