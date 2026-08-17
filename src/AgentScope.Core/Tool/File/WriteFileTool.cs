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
/// Tool for writing content to files within the allowed sandbox directory. Supports overwrite or append mode.
/// 写入文件工具；路径必须在 FileToolUtils 沙箱内。支持覆盖或追加。
/// </summary>
public class WriteFileTool : ToolBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="WriteFileTool"/> class.
    /// 初始化 WriteFileTool 实例。
    /// </summary>
    public WriteFileTool()
        : base("write_file", "写入文件。参数: path(必填), content(必填), append(可选,默认false为覆盖)。路径必须在允许的根目录下。")
    {
    }

    /// <inheritdoc />
    public override async Task<ToolResult> ExecuteAsync(Dictionary<string, object> parameters)
    {
        // Validate required parameters / 校验必填参数
        if (!parameters.TryGetValue("path", out var pathObj) || pathObj is not string path)
            return ToolResult.Fail("缺少必需参数: path");
        if (!parameters.TryGetValue("content", out var contentObj))
            return ToolResult.Fail("缺少必需参数: content");
        var content = contentObj?.ToString() ?? "";

        // Check path is within allowed sandbox / 检查路径是否在沙箱内
        var fullPath = FileToolUtils.GetAllowedFullPath(path);
        if (fullPath == null)
            return ToolResult.Fail("路径不在允许范围内，拒绝访问。");

        // Determine append mode / 判断是否为追加模式
        var append = parameters.TryGetValue("append", out var a) && (a is true or "true");

        try
        {
            // Ensure directory exists / 确保目录存在
            var dir = Path.GetDirectoryName(fullPath);
            if (!string.IsNullOrEmpty(dir))
                System.IO.Directory.CreateDirectory(dir);
            // Append or overwrite / 追加或覆盖写入
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

    /// <inheritdoc />
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
