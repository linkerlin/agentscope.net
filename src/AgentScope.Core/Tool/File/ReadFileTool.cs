// Copyright 2024-2026 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

namespace AgentScope.Core.Tool.File;

/// <summary>
/// 读取文件内容工具；路径必须在 FileToolUtils 沙箱内。
/// </summary>
public class ReadFileTool : ToolBase
{
    /// <summary>默认单次读取最大字符数（可配合 offset/limit 分块读大文件）</summary>
    public int DefaultLimit { get; set; } = 100_000;

    public ReadFileTool()
        : base("read_file", "读取文件内容。参数: path(必填), offset(可选,默认0), limit(可选,默认按配置)。路径必须在允许的根目录下。")
    {
    }

    public override async Task<ToolResult> ExecuteAsync(Dictionary<string, object> parameters)
    {
        if (!parameters.TryGetValue("path", out var pathObj) || pathObj is not string path)
            return ToolResult.Fail("缺少必需参数: path");

        var fullPath = FileToolUtils.GetAllowedFullPath(path);
        if (fullPath == null)
            return ToolResult.Fail("路径不在允许范围内，拒绝访问。");

        if (!System.IO.File.Exists(fullPath))
            return ToolResult.Fail("文件不存在: " + path);

        var offset = GetInt(parameters, "offset", 0);
        var limit = GetInt(parameters, "limit", DefaultLimit);
        if (offset < 0 || limit <= 0 || limit > 10 * 1024 * 1024)
            return ToolResult.Fail("offset 须 >=0，limit 须为正数且不超过 10MB 字符。");

        try
        {
            var content = await System.IO.File.ReadAllTextAsync(fullPath).ConfigureAwait(false);
            if (offset > 0 || limit < content.Length)
            {
                var start = Math.Min(offset, content.Length);
                var len = Math.Min(limit, content.Length - start);
                content = content.Substring(start, len);
            }
            return ToolResult.Ok(content);
        }
        catch (System.Exception ex)
        {
            return ToolResult.Fail("读取文件失败: " + ex.Message);
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
                ["offset"] = new Dictionary<string, object> { ["type"] = "integer", ["description"] = "起始字符偏移", ["required"] = false },
                ["limit"] = new Dictionary<string, object> { ["type"] = "integer", ["description"] = "最多读取字符数", ["required"] = false }
            }
        };
    }

    private static int GetInt(Dictionary<string, object> parameters, string key, int defaultValue)
    {
        if (!parameters.TryGetValue(key, out var v))
            return defaultValue;
        if (v is int i)
            return i;
        if (v is long l)
            return (int)l;
        if (v is string s && int.TryParse(s, out var parsed))
            return parsed;
        return defaultValue;
    }
}
