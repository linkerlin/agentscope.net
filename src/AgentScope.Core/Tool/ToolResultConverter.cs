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

using System;
using System.Collections.Generic;
using System.Text.Json;
using AgentScope.Core.Message;

namespace AgentScope.Core.Tool;

/// <summary>
/// 工具结果转换器，将 ToolResult 转换为多种输出格式。
/// </summary>
public static class ToolResultConverter
{
    /// <summary>
    /// 将 ToolResult 转为 ToolResultBlock
    /// </summary>
    public static ToolResultBlock ToResultBlock(ToolResult result, string toolUseId)
    {
        ArgumentNullException.ThrowIfNull(result);

        return new ToolResultBlock
        {
            Id = toolUseId,
            Output = result.Success ? result.Result : result.Error,
            IsError = !result.Success
        };
    }

    /// <summary>
    /// 将 ToolResult 转为纯文本
    /// </summary>
    public static string ToText(ToolResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        if (result.Success)
        {
            return result.Result?.ToString() ?? "(空结果)";
        }

        return $"错误: {result.Error ?? "未知错误"}";
    }

    /// <summary>
    /// 将 ToolResult 转为 JSON 字符串
    /// </summary>
    public static string ToJson(ToolResult result, bool prettyPrint = false)
    {
        ArgumentNullException.ThrowIfNull(result);

        var obj = new Dictionary<string, object?>
        {
            ["success"] = result.Success,
            ["result"] = result.Result,
            ["error"] = result.Error
        };

        var options = new JsonSerializerOptions
        {
            WriteIndented = prettyPrint,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };

        return JsonSerializer.Serialize(obj, options);
    }

    /// <summary>
    /// 将 ToolResult 转为 Msg（tool 角色消息）
    /// </summary>
    public static ToolResultMessage ToMessage(ToolResult result, string? toolName = null)
    {
        ArgumentNullException.ThrowIfNull(result);

        var msg = new ToolResultMessage(toolName, ToText(result));

        if (result.Result != null)
        {
            msg.Metadata = new Dictionary<string, object>
            {
                ["tool_success"] = result.Success
            };
        }

        return msg;
    }

    /// <summary>
    /// 批量转换 ToolResult 列表到 ToolResultBlock 列表
    /// </summary>
    public static List<ToolResultBlock> ToResultBlocks(List<(ToolResult Result, string ToolUseId)> results)
    {
        ArgumentNullException.ThrowIfNull(results);

        var blocks = new List<ToolResultBlock>(results.Count);
        foreach (var (result, id) in results)
        {
            blocks.Add(ToResultBlock(result, id));
        }
        return blocks;
    }
}
