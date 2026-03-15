// Copyright 2024-2026 the original author or authors.
// Licensed under the Apache License, Version 2.0

namespace AgentScope.Core.Tool.File;

/// <summary>
/// 写入文件工具；路径必须在 FileToolUtils 沙箱内。支持覆盖或追加。
/// </summary>
public class WriteFileTool : ToolBase
{
    public WriteFileTool()
        : base("write_file", "写入文件。参数: path(必填), content(必填), append(可选,默认false为覆盖)。路径必须在允许的根目录下。")
    {
    }

    public override async Task<ToolResult> ExecuteAsync(Dictionary<string, object> parameters)
    {
        if (!parameters.TryGetValue("path", out var pathObj) || pathObj is not string path)
            return ToolResult.Fail("缺少必需参数: path");
        if (!parameters.TryGetValue("content", out var contentObj))
            return ToolResult.Fail("缺少必需参数: content");
        var content = contentObj?.ToString() ?? "";

        var fullPath = FileToolUtils.GetAllowedFullPath(path);
        if (fullPath == null)
            return ToolResult.Fail("路径不在允许范围内，拒绝访问。");

        var append = parameters.TryGetValue("append", out var a) && (a is true or "true");

        try
        {
            var dir = Path.GetDirectoryName(fullPath);
            if (!string.IsNullOrEmpty(dir))
                System.IO.Directory.CreateDirectory(dir);
            if (append && System.IO.File.Exists(fullPath))
                await System.IO.File.AppendAllTextAsync(fullPath, content).ConfigureAwait(false);
            else
                await System.IO.File.WriteAllTextAsync(fullPath, content).ConfigureAwait(false);
            return ToolResult.Ok("已写入: " + path);
        }
        catch (System.Exception ex)
        {
            return ToolResult.Fail("写入文件失败: " + ex.Message);
        }
    }

    public override Dictionary<string, object> GetSchema()
    {
        return new Dictionary<string, object>
        {
            ["name"] = Name,
            ["description"] = Description,
            ["parameters"] = new Dictionary<string, object>
            {
                ["path"] = new Dictionary<string, object> { ["type"] = "string", ["description"] = "文件路径（须在允许目录下）", ["required"] = true },
                ["content"] = new Dictionary<string, object> { ["type"] = "string", ["description"] = "要写入的内容", ["required"] = true },
                ["append"] = new Dictionary<string, object> { ["type"] = "boolean", ["description"] = "是否追加（默认 false 为覆盖）", ["required"] = false }
            }
        };
    }
}
