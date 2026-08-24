// Copyright 2024-2026 the original author or authors.
// Licensed under the Apache License, Version 2.0

using System.Runtime.InteropServices;
using AgentScope.Core.Tool.File;
using Xunit;

namespace AgentScope.Core.Tests.Tool.File;

/// <summary>
/// Tests for file operation tools (ReadFileTool, WriteFileTool)
/// 文件操作工具（ReadFileTool、WriteFileTool）的测试
/// </summary>
public class FileToolTests
{
    /// <summary>
    /// Temporary directory for test file operations
    /// 用于测试文件操作的临时目录
    /// </summary>
    private readonly string _tempDir;

    /// <summary>
    /// Initializes a new test instance with a temporary directory
    /// 使用临时目录初始化测试实例
    /// </summary>
    public FileToolTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "AgentScope_FileTool_" + Guid.NewGuid().ToString("N"));
        System.IO.Directory.CreateDirectory(_tempDir);
        FileToolUtils.AllowedRoots = new[] { _tempDir, Path.GetTempPath() };
    }

    [Fact]
    /// <summary>
    /// IsPathAllowed returns true for paths within the allowed root directory
    /// 测试 IsPathAllowed 对允许根目录内的路径返回 true
    /// </summary>
    public void FileToolUtils_IsPathAllowed_WithinRoot_ReturnsTrue()
    {
        var path = Path.Combine(_tempDir, "a.txt");
        Assert.True(FileToolUtils.IsPathAllowed(path));
    }

    [Fact]
    /// <summary>
    /// IsPathAllowed returns false for paths outside the allowed root directory
    /// 测试 IsPathAllowed 对允许根目录外的路径返回 false
    /// </summary>
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
    /// <summary>
    /// WriteFileTool and ReadFileTool round-trip: write then read back the same content
    /// 测试 WriteFileTool 和 ReadFileTool 的读写往返：写入后读取相同内容
    /// </summary>
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
    /// <summary>
    /// ReadFileTool returns fail result when trying to read a disallowed path
    /// 测试 ReadFileTool 在尝试读取不允许的路径时返回失败结果
    /// </summary>
    public async Task ReadFileTool_DisallowedPath_ReturnsFail()
    {
        var read = new ReadFileTool();
        var result = await read.ExecuteAsync(new Dictionary<string, object> { ["path"] = "/etc/passwd" });
        Assert.False(result.Success);
        Assert.Contains("路径不在允许范围内", result.Error);
    }

    [Fact]
    /// <summary>
    /// ReadFileTool returns fail result when path parameter is missing
    /// 测试 ReadFileTool 在缺少路径参数时返回失败结果
    /// </summary>
    public async Task ReadFileTool_MissingPath_ReturnsFail()
    {
        var read = new ReadFileTool();
        var result = await read.ExecuteAsync(new Dictionary<string, object>());
        Assert.False(result.Success);
    }
}
