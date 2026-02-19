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
using System.Linq;
using AgentScope.Core.Formatter.Gemini.Dto;
using AgentScope.Core.Message;

namespace AgentScope.Core.Formatter.Gemini;

/// <summary>
/// Gemini 消息转换器
/// Gemini message converter
/// 
/// 将AgentScope的Msg转换为Gemini API消息格式
/// Converts AgentScope Msg to Gemini API message format
/// 
/// Gemini API 特点：
/// - 用户消息角色为 "user"
/// - 模型回复角色为 "model"（不是 "assistant"）
/// - 内容格式为 parts 数组
/// </summary>
public static class GeminiMessageConverter
{
    /// <summary>
    /// 将Msg转换为GeminiContent
    /// Convert Msg to GeminiContent
    /// </summary>
    /// <param name="msg">要转换的消息 / Message to convert</param>
    /// <returns>Gemini格式的内容 / Gemini formatted content</returns>
    public static GeminiContent ConvertToContent(Msg msg)
    {
        if (msg == null)
        {
            throw new ArgumentNullException(nameof(msg));
        }

        var role = ConvertRole(msg.Role);
        var parts = ConvertToParts(msg);

        return new GeminiContent
        {
            Role = role,
            Parts = parts
        };
    }

    /// <summary>
    /// 转换角色名称
    /// Convert role name
    /// </summary>
    /// <param name="agentScopeRole">AgentScope角色 / AgentScope role</param>
    /// <returns>Gemini角色 / Gemini role</returns>
    private static string ConvertRole(string? agentScopeRole)
    {
        var role = agentScopeRole?.ToLowerInvariant() ?? "user";

        return role switch
        {
            "assistant" => "model",  // Gemini uses "model" instead of "assistant"
            "model" => "model",
            "system" => "user",      // Gemini handles system messages differently
            "tool" => "user",        // Function responses go in user role
            _ => "user"
        };
    }

    /// <summary>
    /// 将Msg转换为GeminiPart列表
    /// Convert Msg to list of GeminiPart
    /// </summary>
    private static List<GeminiPart> ConvertToParts(Msg msg)
    {
        var parts = new List<GeminiPart>();

        // 处理 Content（可能是字符串、List<ContentBlock> 或其他类型）
        // Handle Content (can be string, List<ContentBlock>, or other types)
        if (msg.Content is string textContent && !string.IsNullOrEmpty(textContent))
        {
            parts.Add(new GeminiPart { Text = textContent });
        }
        else if (msg.Content is List<ContentBlock> contentBlocks && contentBlocks.Count > 0)
        {
            foreach (var block in contentBlocks)
            {
                var part = ConvertContentBlockToPart(block);
                if (part != null)
                {
                    parts.Add(part);
                }
            }
        }
        else if (msg.Content is IEnumerable<ContentBlock> blocks)
        {
            foreach (var block in blocks)
            {
                var part = ConvertContentBlockToPart(block);
                if (part != null)
                {
                    parts.Add(part);
                }
            }
        }
        else
        {
            // 使用 GetTextContent 作为后备
            // Use GetTextContent as fallback
            var text = msg.GetTextContent();
            if (!string.IsNullOrEmpty(text))
            {
                parts.Add(new GeminiPart { Text = text });
            }
        }

        return parts;
    }

    /// <summary>
    /// 将ContentBlock转换为GeminiPart
    /// Convert ContentBlock to GeminiPart
    /// </summary>
    private static GeminiPart? ConvertContentBlockToPart(ContentBlock block)
    {
        return block switch
        {
            TextBlock textBlock when !string.IsNullOrEmpty(textBlock.Text)
                => new GeminiPart { Text = textBlock.Text },

            ImageBlock imageBlock
                => ConvertImageBlockToPart(imageBlock),

            ToolUseBlock toolUseBlock
                => new GeminiPart
                {
                    FunctionCall = new GeminiFunctionCall
                    {
                        Name = toolUseBlock.Name,
                        Args = toolUseBlock.Input
                    }
                },

            ToolResultBlock toolResultBlock
                => new GeminiPart
                {
                    FunctionResponse = new GeminiFunctionResponse
                    {
                        Name = toolResultBlock.Id,
                        Response = new Dictionary<string, object>
                        {
                            ["result"] = toolResultBlock.Output ?? ""
                        }
                    }
                },

            ThinkingBlock thinkingBlock
                => new GeminiPart { Text = thinkingBlock.Thinking },

            _ => null
        };
    }

    /// <summary>
    /// 转换图片块为GeminiPart
    /// Convert image block to GeminiPart
    /// </summary>
    private static GeminiPart? ConvertImageBlockToPart(ImageBlock imageBlock)
    {
        // Gemini prefers inline data over URLs
        // Gemini 更倾向于使用内联数据而非 URL
        if (imageBlock.Data != null && imageBlock.Data.Length > 0)
        {
            return new GeminiPart
            {
                InlineData = new GeminiInlineData
                {
                    MimeType = imageBlock.MimeType ?? DetectMimeType(imageBlock.Url),
                    Data = Convert.ToBase64String(imageBlock.Data)
                }
            };
        }

        if (!string.IsNullOrEmpty(imageBlock.Url))
        {
            // For URLs, check if it's a data URL
            // 对于 URL，检查是否为 data URL
            if (imageBlock.Url.StartsWith("data:"))
            {
                var (mimeType, data) = ParseDataUrl(imageBlock.Url);
                return new GeminiPart
                {
                    InlineData = new GeminiInlineData
                    {
                        MimeType = mimeType,
                        Data = data
                    }
                };
            }

            // Gemini doesn't support external URLs directly
            // For production, you would need to download the image first
            // Gemini 不直接支持外部 URL，生产环境需要先下载图片
            return new GeminiPart
            {
                Text = $"[Image: {imageBlock.Url}]"
            };
        }

        return null;
    }

    /// <summary>
    /// 检测MIME类型
    /// Detect MIME type from URL
    /// </summary>
    private static string DetectMimeType(string? url)
    {
        if (string.IsNullOrEmpty(url))
            return "image/png";

        var lower = url.ToLowerInvariant();
        if (lower.Contains(".jpg") || lower.Contains(".jpeg"))
            return "image/jpeg";
        if (lower.Contains(".gif"))
            return "image/gif";
        if (lower.Contains(".webp"))
            return "image/webp";

        return "image/png";
    }

    /// <summary>
    /// 解析Data URL
    /// Parse Data URL
    /// </summary>
    private static (string mimeType, string data) ParseDataUrl(string dataUrl)
    {
        // Format: data:mimeType;base64,data
        var colonIndex = dataUrl.IndexOf(':');
        var semicolonIndex = dataUrl.IndexOf(';');
        var commaIndex = dataUrl.IndexOf(',');

        if (colonIndex < 0 || semicolonIndex < 0 || commaIndex < 0)
        {
            return ("image/png", dataUrl);
        }

        var mimeType = dataUrl.Substring(colonIndex + 1, semicolonIndex - colonIndex - 1);
        var data = dataUrl.Substring(commaIndex + 1);

        return (mimeType, data);
    }

    /// <summary>
    /// 从GeminiContent提取文本
    /// Extract text from GeminiContent
    /// </summary>
    public static string ExtractText(GeminiContent content)
    {
        if (content?.Parts == null)
            return "";

        var texts = new List<string>();
        foreach (var part in content.Parts)
        {
            if (!string.IsNullOrEmpty(part.Text))
            {
                texts.Add(part.Text);
            }
        }

        return string.Join("\n", texts);
    }
}
