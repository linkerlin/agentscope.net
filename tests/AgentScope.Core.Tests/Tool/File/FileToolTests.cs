// Copyright 2024-2026 the original author or authors.
// Licensed under the Apache License, Version 2.0

using System.Runtime.InteropServices;
using AgentScope.Core.Tool.File;
using Xunit;

namespace AgentScope.Core.Tests.Tool.File;

public class FileToolTests
{
    private readonly string _tempDir;

    public FileToolTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "AgentScope_FileTool_" + Guid.NewGuid().ToString("N"));
        System.IO.Directory.CreateDirectory(_tempDir);
        FileToolUtils.AllowedRoots = new[] { _tempDir, Path.GetTempPath() };
    }

    [Fact]
    public void FileToolUtils_IsPathAllowed_WithinRoot_ReturnsTrue()
    {
        var path = Path.Combine(_tempDir, "a.txt");
        Assert.True(FileToolUtils.IsPathAllowed(path));
    }

    [Fact]
    public void FileToolUtils_IsPathAllowed_OutsideRoot_ReturnsFalse()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            Assert.False(FileToolUtils.IsPathAllowed("C:\\Windows\\System32\\config\\sam"));
        }
        else
        {
            Assert.False(FileToolUtils.IsPathAllowed("/etc/shadow"));
        }
    }

    [Fact]
    public async Task WriteFileTool_Then_ReadFileTool_RoundTrips()
    {
        var rel = "f1.txt";
        var full = Path.Combine(_tempDir, rel);
        var write = new WriteFileTool();
        var read = new ReadFileTool();
        await write.ExecuteAsync(new Dictionary<string, object> { ["path"] = full, ["content"] = "hello" });
        Assert.True(System.IO.File.Exists(full));
        var result = await read.ExecuteAsync(new Dictionary<string, object> { ["path"] = full });
        Assert.True(result.Success);
        Assert.Equal("hello", result.Result);
    }

    [Fact]
    public async Task ReadFileTool_DisallowedPath_ReturnsFail()
    {
        var read = new ReadFileTool();
        var result = await read.ExecuteAsync(new Dictionary<string, object> { ["path"] = "/etc/passwd" });
        Assert.False(result.Success);
        Assert.Contains("路径不在允许范围内", result.Error);
    }

    [Fact]
    public async Task ReadFileTool_MissingPath_ReturnsFail()
    {
        var read = new ReadFileTool();
        var result = await read.ExecuteAsync(new Dictionary<string, object>());
        Assert.False(result.Success);
    }
}
