// Copyright 2024-2026 the original author or authors.
// Licensed under the Apache License, Version 2.0

using System.Runtime.InteropServices;
using AgentScope.Core.Tool.Coding;
using Xunit;

namespace AgentScope.Core.Tests.Tool.Coding;

public class ShellCommandToolTests
{
    [Fact]
    public void UnixCommandValidator_DangerousCommand_ReturnsFalse()
    {
        var v = new UnixCommandValidator();
        Assert.False(v.Validate("sudo rm -rf /"));
        Assert.False(v.Validate("rm -rf /"));
    }

    [Fact]
    public void UnixCommandValidator_SafeCommand_NoWhitelist_ReturnsTrue()
    {
        var v = new UnixCommandValidator();
        Assert.True(v.Validate("ls -la"));
        Assert.True(v.Validate("echo hello"));
    }

    [Fact]
    public void UnixCommandValidator_WithWhitelist_OnlyAllowsListed()
    {
        var v = new UnixCommandValidator { AllowedCommands = new HashSet<string> { "echo", "pwd" } };
        Assert.True(v.Validate("echo hi"));
        Assert.False(v.Validate("ls"));
    }

    [Fact]
    public void WindowsCommandValidator_DangerousCommand_ReturnsFalse()
    {
        var v = new WindowsCommandValidator();
        Assert.False(v.Validate("format C:"));
        Assert.False(v.Validate("del /f /s /q C:\\"));
    }

    [Fact]
    public void WindowsCommandValidator_SafeCommand_NoWhitelist_ReturnsTrue()
    {
        var v = new WindowsCommandValidator();
        Assert.True(v.Validate("dir"));
        Assert.True(v.Validate("echo hello"));
    }

    [Fact]
    public async Task ShellCommandTool_ValidateFails_ReturnsFail()
    {
        var v = new UnixCommandValidator { AllowedCommands = new HashSet<string> { "echo" } };
        var tool = new ShellCommandTool(v);
        var result = await tool.ExecuteAsync(new Dictionary<string, object> { ["command"] = "ls" });
        Assert.False(result.Success);
        Assert.Contains("安全检查", result.Error);
    }

    [Fact]
    public async Task ShellCommandTool_MissingCommand_ReturnsFail()
    {
        var tool = new ShellCommandTool();
        var result = await tool.ExecuteAsync(new Dictionary<string, object>());
        Assert.False(result.Success);
    }

    [Fact]
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
