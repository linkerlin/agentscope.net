using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using AgentScope.Core.Formatter.OpenAI.Dto;
using AgentScope.Core.Message;

namespace AgentScope.Core.Formatter.OpenAI;

/// <summary>
/// OpenAI 消息转换器
/// OpenAI message converter
/// 
/// 将AgentScope的Msg转换为OpenAI API消息格式
/// Converts AgentScope Msg to OpenAI API message format
/// 
/// Java参考: io.agentscope.core.formatter.openai.OpenAIMessageConverter
/// </summary>
public static class OpenAIMessageConverter
{
    /// <summary>
    /// 将Msg转换为OpenAIMessage
    /// Convert Msg to OpenAIMessage
    /// </summary>
    /// <param name="msg">要转换的消息 / Message to convert</param>
    /// <returns>OpenAI格式的消息 / OpenAI formatted message</returns>
    public static OpenAIMessage ConvertToMessage(Msg msg)
    {
        if (msg == null)
        {
            throw new ArgumentNullException(nameof(msg));
        }

        var role = msg.Role?.ToLowerInvariant() ?? "user";

        return role switch
        {
            "system" => ConvertSystemMessage(msg),
            "user" => ConvertUserMessage(msg),
            "assistant" => ConvertAssistantMessage(msg),
            "tool" => ConvertToolMessage(msg),
            _ => ConvertUserMessage(msg) // 默认作为用户消息 / Default as user message
        };
    }

    /// <summary>
    /// 转换系统消息
    /// Convert system message
    /// </summary>
    private static OpenAIMessage ConvertSystemMessage(Msg msg)
    {
        var content = ExtractTextContent(msg);
        
        return new OpenAIMessage
        {
            Role = "system",
            Content = content,
            Name = msg.Name
        };
    }

    /// <summary>
    /// 转换用户消息（支持多模态）
    /// Convert user message (supports multimodal)
    /// </summary>
    private static OpenAIMessage ConvertUserMessage(Msg msg)
    {
        // 检查是否有多模态内容
        // Check if there is multimodal content
        var hasMedia = HasMediaContent(msg);

        if (hasMedia)
        {
            // 多模态消息，使用content数组
            // Multimodal message, use content array
            var contentParts = ConvertToContentParts(msg);
            
            return new OpenAIMessage
            {
                Role = "user",
                ContentParts = contentParts,
                Name = msg.Name
            };
        }
        else
        {
            // 纯文本消息
            // Plain text message
            var content = ExtractTextContent(msg);
            
            return new OpenAIMessage
            {
                Role = "user",
                Content = content,
                Name = msg.Name
            };
        }
    }

    /// <summary>
    /// 转换助手消息（可能包含工具调用）
    /// Convert assistant message (may include tool calls)
    /// </summary>
    private static OpenAIMessage ConvertAssistantMessage(Msg msg)
    {
        var content = ExtractTextContent(msg);
        var toolCalls = ExtractToolCalls(msg);

        var message = new OpenAIMessage
        {
            Role = "assistant",
            Content = content,
            Name = msg.Name
        };

        if (toolCalls != null && toolCalls.Count > 0)
        {
            message.ToolCalls = toolCalls;
        }

        return message;
    }

    /// <summary>
    /// 转换工具消息
    /// Convert tool message
    /// </summary>
    private static OpenAIMessage ConvertToolMessage(Msg msg)
    {
        var content = ExtractTextContent(msg);
        
        // 尝试从metadata中获取tool_call_id
        // Try to get tool_call_id from metadata
        string? toolCallId = null;
        if (msg.Metadata != null && msg.Metadata.TryGetValue("tool_call_id", out var id))
        {
            toolCallId = id?.ToString();
        }

        return new OpenAIMessage
        {
            Role = "tool",
            Content = content,
            ToolCallId = toolCallId,
            Name = msg.Name
        };
    }

    /// <summary>
    /// 提取文本内容
    /// Extract text content
    /// </summary>
    private static string ExtractTextContent(Msg msg)
    {
        if (msg.Content == null)
        {
            return string.Empty;
        }

        // 如果是字符串，直接返回
        // If string, return directly
        if (msg.Content is string text)
        {
            return text;
        }

        // 如果是JsonObject或Dictionary，尝试提取text字段
        // If JsonObject or Dictionary, try to extract text field
        if (msg.Content is JsonObject jsonObj && jsonObj.TryGetPropertyValue("text", out var jsonText))
        {
            return jsonText?.ToString() ?? string.Empty;
        }

        if (msg.Content is Dictionary<string, object> dict && dict.TryGetValue("text", out var textObj))
        {
            return textObj?.ToString() ?? string.Empty;
        }

        // 否则序列化为JSON字符串
        // Otherwise serialize to JSON string
        return JsonSerializer.Serialize(msg.Content);
    }

    /// <summary>
    /// 检查是否包含多媒体内容
    /// Check if contains multimedia content
    /// </summary>
    private static bool HasMediaContent(Msg msg)
    {
        // 检查是否有URL列表
        // Check if there is URL list
        if (msg.Url != null && msg.Url.Count > 0)
        {
            return true;
        }

        // 检查metadata中是否有多模态标记
        // Check if there is multimodal marker in metadata
        if (msg.Metadata != null)
        {
            if (msg.Metadata.ContainsKey("images") || 
                msg.Metadata.ContainsKey("videos") ||
                msg.Metadata.ContainsKey("audio"))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// 转换为内容部分列表
    /// Convert to content parts list
    /// </summary>
    private static List<OpenAIMessageContent> ConvertToContentParts(Msg msg)
    {
        var parts = new List<OpenAIMessageContent>();

        // 添加文本部分
        // Add text part
        var text = ExtractTextContent(msg);
        if (!string.IsNullOrWhiteSpace(text))
        {
            parts.Add(new OpenAIMessageContent
            {
                Type = "text",
                Text = text
            });
        }

        // 添加图片
        // Add images
        if (msg.Url != null)
        {
            foreach (var url in msg.Url)
            {
                if (IsImageUrl(url))
                {
                    var imageUrl = OpenAIConverterUtils.ConvertImageSourceToUrl(url);
                    parts.Add(new OpenAIMessageContent
                    {
                        Type = "image_url",
                        ImageUrl = new OpenAIImageUrl
                        {
                            Url = imageUrl
                        }
                    });
                }
                else if (IsVideoUrl(url))
                {
                    var videoUrl = OpenAIConverterUtils.ConvertVideoSourceToUrl(url);
                    parts.Add(new OpenAIMessageContent
                    {
                        Type = "video_url",
                        VideoUrl = new OpenAIVideoUrl
                        {
                            Url = videoUrl
                        }
                    });
                }
            }
        }

        // 从metadata中添加多媒体内容
        // Add multimedia content from metadata
        if (msg.Metadata != null)
        {
            // 处理images
            if (msg.Metadata.TryGetValue("images", out var images))
            {
                AddImageParts(parts, images);
            }

            // 处理videos
            if (msg.Metadata.TryGetValue("videos", out var videos))
            {
                AddVideoParts(parts, videos);
            }

            // 处理audio
            if (msg.Metadata.TryGetValue("audio", out var audio))
            {
                AddAudioParts(parts, audio);
            }
        }

        return parts;
    }

    /// <summary>
    /// 添加图片部分
    /// Add image parts
    /// </summary>
    private static void AddImageParts(List<OpenAIMessageContent> parts, object images)
    {
        if (images is IEnumerable<object> imageList)
        {
            foreach (var img in imageList)
            {
                var url = img?.ToString();
                if (!string.IsNullOrWhiteSpace(url))
                {
                    var imageUrl = OpenAIConverterUtils.ConvertImageSourceToUrl(url);
                    parts.Add(new OpenAIMessageContent
                    {
                        Type = "image_url",
                        ImageUrl = new OpenAIImageUrl
                        {
                            Url = imageUrl
                        }
                    });
                }
            }
        }
    }

    /// <summary>
    /// 添加视频部分
    /// Add video parts
    /// </summary>
    private static void AddVideoParts(List<OpenAIMessageContent> parts, object videos)
    {
        if (videos is IEnumerable<object> videoList)
        {
            foreach (var vid in videoList)
            {
                var url = vid?.ToString();
                if (!string.IsNullOrWhiteSpace(url))
                {
                    var videoUrl = OpenAIConverterUtils.ConvertVideoSourceToUrl(url);
                    parts.Add(new OpenAIMessageContent
                    {
                        Type = "video_url",
                        VideoUrl = new OpenAIVideoUrl
                        {
                            Url = videoUrl
                        }
                    });
                }
            }
        }
    }

    /// <summary>
    /// 添加音频部分
    /// Add audio parts
    /// </summary>
    private static void AddAudioParts(List<OpenAIMessageContent> parts, object audio)
    {
        if (audio is Dictionary<string, object> audioDict)
        {
            if (audioDict.TryGetValue("data", out var data) && 
                audioDict.TryGetValue("format", out var format))
            {
                parts.Add(new OpenAIMessageContent
                {
                    Type = "input_audio",
                    InputAudio = new OpenAIInputAudio
                    {
                        Data = data?.ToString() ?? string.Empty,
                        Format = format?.ToString() ?? "wav"
                    }
                });
            }
        }
    }

    /// <summary>
    /// 提取工具调用
    /// Extract tool calls
    /// </summary>
    private static List<OpenAIToolCall>? ExtractToolCalls(Msg msg)
    {
        if (msg.Metadata == null)
        {
            return null;
        }

        if (!msg.Metadata.TryGetValue("tool_calls", out var toolCallsObj))
        {
            return null;
        }

        var toolCalls = new List<OpenAIToolCall>();

        if (toolCallsObj is IEnumerable<object> toolCallsList)
        {
            foreach (var call in toolCallsList)
            {
                if (call is Dictionary<string, object> callDict)
                {
                    var toolCall = ParseToolCall(callDict);
                    if (toolCall != null)
                    {
                        toolCalls.Add(toolCall);
                    }
                }
                else if (call is JsonObject jsonObj)
                {
                    var toolCall = ParseToolCallFromJsonObject(jsonObj);
                    if (toolCall != null)
                    {
                        toolCalls.Add(toolCall);
                    }
                }
            }
        }

        return toolCalls.Count > 0 ? toolCalls : null;
    }

    /// <summary>
    /// 解析工具调用
    /// Parse tool call
    /// </summary>
    private static OpenAIToolCall? ParseToolCall(Dictionary<string, object> dict)
    {
        if (!dict.TryGetValue("name", out var name))
        {
            return null;
        }

        var id = dict.TryGetValue("id", out var idObj) ? 
            idObj?.ToString() : Guid.NewGuid().ToString();

        var arguments = dict.TryGetValue("arguments", out var argsObj) ?
            JsonSerializer.Serialize(argsObj) : "{}";

        return new OpenAIToolCall
        {
            Id = id ?? Guid.NewGuid().ToString(),
            Type = "function",
            Function = new OpenAIFunctionCall
            {
                Name = name?.ToString() ?? string.Empty,
                Arguments = arguments
            }
        };
    }

    /// <summary>
    /// 从JsonObject解析工具调用
    /// Parse tool call from JsonObject
    /// </summary>
    private static OpenAIToolCall? ParseToolCallFromJsonObject(JsonObject jsonObj)
    {
        var name = jsonObj["name"]?.ToString();
        if (string.IsNullOrWhiteSpace(name))
        {
            return null;
        }

        var id = jsonObj["id"]?.ToString() ?? Guid.NewGuid().ToString();
        var arguments = jsonObj["arguments"]?.ToString() ?? "{}";

        return new OpenAIToolCall
        {
            Id = id,
            Type = "function",
            Function = new OpenAIFunctionCall
            {
                Name = name,
                Arguments = arguments
            }
        };
    }

    /// <summary>
    /// 判断是否是图片URL
    /// Check if is image URL
    /// </summary>
    private static bool IsImageUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return false;
        }

        var lowerUrl = url.ToLowerInvariant();
        
        // 检查data URI
        if (lowerUrl.StartsWith("data:image/"))
        {
            return true;
        }

        // 检查文件扩展名
        return lowerUrl.EndsWith(".jpg") || 
               lowerUrl.EndsWith(".jpeg") || 
               lowerUrl.EndsWith(".png") || 
               lowerUrl.EndsWith(".gif") || 
               lowerUrl.EndsWith(".webp") || 
               lowerUrl.EndsWith(".bmp");
    }

    /// <summary>
    /// 判断是否是视频URL
    /// Check if is video URL
    /// </summary>
    private static bool IsVideoUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return false;
        }

        var lowerUrl = url.ToLowerInvariant();
        
        // 检查data URI
        if (lowerUrl.StartsWith("data:video/"))
        {
            return true;
        }

        // 检查文件扩展名
        return lowerUrl.EndsWith(".mp4") || 
               lowerUrl.EndsWith(".mpeg") || 
               lowerUrl.EndsWith(".mpg") || 
               lowerUrl.EndsWith(".mov") || 
               lowerUrl.EndsWith(".avi") || 
               lowerUrl.EndsWith(".wmv") || 
               lowerUrl.EndsWith(".flv") || 
               lowerUrl.EndsWith(".webm") || 
               lowerUrl.EndsWith(".mkv");
    }
}
