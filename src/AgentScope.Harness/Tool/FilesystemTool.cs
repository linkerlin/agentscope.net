using AgentScope.Core.Message;
using AgentScope.Core.Tool;
using AgentScope.Harness.Filesystem;

namespace AgentScope.Harness.Tool;

/// <summary>
/// 文件系统工具。对标 Java FilesystemTool。
/// 提供文件读写、搜索、列表等操作。
/// </summary>
public sealed class FilesystemTool(IFilesystem filesystem) : ITool
{
    public string Name => "filesystem";
    public string Description => "文件系统操作：读写文件、搜索内容、列出目录";

    public async Task<ToolResult> ExecuteAsync(Dictionary<string, object> parameters)
    {
        var op = parameters.GetValueOrDefault("operation")?.ToString();
        var path = parameters.GetValueOrDefault("path")?.ToString();

        try
        {
            return op switch
            {
                "read" => await ReadAsync(path, parameters),
                "write" => await WriteAsync(path, parameters),
                "list" => await ListAsync(path),
                "grep" => await GrepAsync(parameters),
                "glob" => await GlobAsync(parameters),
                "delete" => await DeleteAsync(path),
                _ => ToolResult.Fail($"未知操作: {op}")
            };
        }
        catch (Exception ex)
        {
            return ToolResult.Fail($"文件系统操作失败: {ex.Message}");
        }
    }

    private async Task<ToolResult> ReadAsync(string? path, Dictionary<string, object> p)
    {
        if (path == null) return ToolResult.Fail("需要 path 参数");
        var result = await filesystem.ReadAsync(path);
        return result.Found ? ToolResult.Ok(result.Content!) : ToolResult.Fail("文件不存在");
    }

    private async Task<ToolResult> WriteAsync(string? path, Dictionary<string, object> p)
    {
        if (path == null) return ToolResult.Fail("需要 path 参数");
        var content = p.GetValueOrDefault("content")?.ToString() ?? "";
        await filesystem.WriteAsync(path, content);
        return ToolResult.Ok("写入成功");
    }

    private async Task<ToolResult> ListAsync(string? path)
    {
        if (path == null) path = ".";
        var result = await filesystem.ListAsync(path);
        var names = result.Files?.Select(f => $"{(f.IsDirectory ? "📁" : "📄")} {f.Name}") ?? [];
        return ToolResult.Ok(string.Join("\n", names));
    }

    private async Task<ToolResult> GrepAsync(Dictionary<string, object> p)
    {
        var pattern = p.GetValueOrDefault("pattern")?.ToString() ?? "";
        var path = p.GetValueOrDefault("path")?.ToString();
        var glob = p.GetValueOrDefault("glob")?.ToString();
        var result = await filesystem.GrepAsync(pattern, path, glob);
        var matches = result.Matches?.Select(m => $"{m.File}:{m.LineNumber}: {m.Line}") ?? [];
        return ToolResult.Ok(string.Join("\n", matches));
    }

    private async Task<ToolResult> GlobAsync(Dictionary<string, object> p)
    {
        var pattern = p.GetValueOrDefault("pattern")?.ToString() ?? "*";
        var path = p.GetValueOrDefault("path")?.ToString();
        var result = await filesystem.GlobAsync(pattern, path);
        return ToolResult.Ok(string.Join("\n", result.Paths ?? []));
    }

    private async Task<ToolResult> DeleteAsync(string? path)
    {
        if (path == null) return ToolResult.Fail("需要 path 参数");
        await filesystem.DeleteAsync(path);
        return ToolResult.Ok("删除成功");
    }

    public Dictionary<string, object> GetSchema() => new()
    {
        ["name"] = Name,
        ["description"] = Description,
        ["parameters"] = new Dictionary<string, object>
        {
            ["type"] = "object",
            ["properties"] = new Dictionary<string, object>
            {
                ["operation"] = new Dictionary<string, object> { ["type"] = "string", ["description"] = "操作类型: read/write/list/grep/glob/delete" },
                ["path"] = new Dictionary<string, object> { ["type"] = "string", ["description"] = "文件路径" },
                ["content"] = new Dictionary<string, object> { ["type"] = "string", ["description"] = "写入内容" },
                ["pattern"] = new Dictionary<string, object> { ["type"] = "string", ["description"] = "搜索模式" },
                ["glob"] = new Dictionary<string, object> { ["type"] = "string", ["description"] = "文件匹配模式" }
            },
            ["required"] = new[] { "operation" }
        }
    };
}
