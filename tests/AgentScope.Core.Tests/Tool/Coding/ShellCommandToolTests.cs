// Copyright 2024-2026 the original author or authors.
// Licensed under the Apache License, Version 2.0

using System.Runtime.InteropServices;
using AgentScope.Core.Tool.Coding;
using Xunit;

namespace AgentScope.Core.Tests.Tool.Coding;

/// <summary>
/// Tests for ShellCommandTool and related command validators
/// ShellCommandTool 及相关命令验证器的测试
/// </summary>
public class ShellCommandToolTests
{
    [Fact]
    /// <summary>
    /// UnixCommandValidator returns false for dangerous system commands
    /// 测试 UnixCommandValidator 对危险系统命令返回 false
    /// </summary>
    public void UnixCommandValidator_DangerousCommand_ReturnsFalse()
    {
        var v = new UnixCommandValidator();
        Assert.False(v.Validate("sudo rm -rf /"));
        Assert.False(v.Validate("rm -rf /"));
    }

    [Fact]
    /// <summary>
    /// UnixCommandValidator returns true for safe commands without whitelist
    /// 测试 UnixCommandValidator 对安全命令（无白名单）返回 true
    /// </summary>
    public void UnixCommandValidator_SafeCommand_NoWhitelist_ReturnsTrue()
    {
        var v = new UnixCommandValidator();
        Assert.True(v.Validate("ls -la"));
        Assert.True(v.Validate("echo hello"));
    }

    [Fact]
    /// <summary>
    /// UnixCommandValidator with whitelist only allows listed commands
    /// 测试 UnixCommandValidator 在白名单模式下只允许列表中的命令
    /// </summary>
    public void UnixCommandValidator_WithWhitelist_OnlyAllowsListed()
    {
        var v = new UnixCommandValidator { AllowedCommands = new HashSet<string> { "echo", "pwd" } };
        Assert.True(v.Validate("echo hi"));
        Assert.False(v.Validate("ls"));
    }

    [Fact]
    /// <summary>
    /// WindowsCommandValidator returns false for dangerous system commands
    /// 测试 WindowsCommandValidator 对危险系统命令返回 false
    /// </summary>
    public void WindowsCommandValidator_DangerousCommand_ReturnsFalse()
    {
        var v = new WindowsCommandValidator();
        Assert.False(v.Validate("format C:"));
        Assert.False(v.Validate("del /f /s /q C:\\"));
    }

    [Fact]
    /// <summary>
    /// WindowsCommandValidator returns true for safe commands without whitelist
    /// 测试 WindowsCommandValidator 对安全命令（无白名单）返回 true
    /// </summary>
    public void WindowsCommandValidator_SafeCommand_NoWhitelist_ReturnsTrue()
    {
        var v = new WindowsCommandValidator();
        Assert.True(v.Validate("dir"));
        Assert.True(v.Validate("echo hello"));
    }

    [Fact]
    /// <summary>
    /// ShellCommandTool returns fail result when validation fails
    /// 测试 ShellCommandTool 在验证失败时返回失败结果
    /// </summary>
    public async Task ShellCommandTool_ValidateFails_ReturnsFail()
    {
        var v = new UnixCommandValidator { AllowedCommands = new HashSet<string> { "echo" } };
        var tool = new ShellCommandTool(v);
        var result = await tool.ExecuteAsync(new Dictionary<string, object> { ["command"] = "ls" });
        Assert.False(result.Success);
        Assert.Contains("安全检查", result.Error);
    }

    [Fact]
    /// <summary>
    /// ShellCommandTool returns fail result when command parameter is missing
    /// 测试 ShellCommandTool 在缺少命令参数时返回失败结果
    /// </summary>
    public async Task ShellCommandTool_MissingCommand_ReturnsFail()
    {
        var tool = new ShellCommandTool();
        var result = await tool.ExecuteAsync(new Dictionary<string, object>());
        Assert.False(result.Success);
    }

    [Fact]
    /// <summary>
    /// ShellCommandTool successfully executes echo command and returns output
    /// 测试 ShellCommandTool 成功执行 echo 命令并返回输出
    /// </summary>
    public async Task ShellCommandTool_Echo_ReturnsOutput()
    {
        var v = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? (ICommandValidator)new WindowsCommandValidator()
            : new UnixCommandValidator();
        var tool = new ShellCommandTool(v, TimeSpan.FromSeconds(5));
        var result = await tool.ExecuteAsync(new Dictionary<string, object>
        {
            ["command"] = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "echo ok" : "echo ok"
        });
        Assert.True(result.Success);
        Assert.NotNull(result.Result);
        Assert.Contains("ok", result.Result.ToString());
    }
}
