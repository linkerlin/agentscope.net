// Copyright 2024-2026 the original author or authors.
// Licensed under the Apache License, Version 2.0

namespace AgentScope.Core.Tool.Multimodal;

/// <summary>
/// OpenAI 多模态工具：文生图、图生文、文生语音、语音转文字等。需配置 API 后接入真实实现。
/// </summary>
public class OpenAIMultiModalTool : ToolBase
{
    private const string DefaultName = "openai_multimodal";
    private const string DefaultDescription = "OpenAI 多模态：text_to_image | image_to_text | text_to_audio | audio_to_text。参数: action, prompt(或 image_urls/audio_url), model, size 等。";

    public OpenAIMultiModalTool(string? name = null, string? description = null)
        : base(name ?? DefaultName, description ?? DefaultDescription)
    {
    }

    public override async Task<ToolResult> ExecuteAsync(Dictionary<string, object> parameters)
    {
        if (!parameters.TryGetValue("action", out var actionObj) || actionObj is not string action)
            return ToolResult.Fail("缺少参数: action（text_to_image | image_to_text | text_to_audio | audio_to_text）");

        return action.ToLowerInvariant() switch
        {
            "text_to_image" => await TextToImageAsync(parameters).ConfigureAwait(false),
            "image_to_text" => await ImageToTextAsync(parameters).ConfigureAwait(false),
            "text_to_audio" => await TextToAudioAsync(parameters).ConfigureAwait(false),
            "audio_to_text" => await AudioToTextAsync(parameters).ConfigureAwait(false),
            _ => ToolResult.Fail("不支持的 action: " + action)
        };
    }

    /// <summary>文生图（DALL-E）。需配置 OpenAI API 后实现。</summary>
    protected virtual Task<ToolResult> TextToImageAsync(Dictionary<string, object> parameters)
    {
        _ = parameters;
        return Task.FromResult(ToolResult.Fail("openai_text_to_image 需配置 OpenAI API Key 后接入实现。"));
    }

    /// <summary>图生文（GPT-4 Vision）。需配置 OpenAI API 后实现。</summary>
    protected virtual Task<ToolResult> ImageToTextAsync(Dictionary<string, object> parameters)
    {
        _ = parameters;
        return Task.FromResult(ToolResult.Fail("openai_image_to_text 需配置 OpenAI API Key 后接入实现。"));
    }

    /// <summary>文生语音。需配置 OpenAI API 后实现。</summary>
    protected virtual Task<ToolResult> TextToAudioAsync(Dictionary<string, object> parameters)
    {
        _ = parameters;
        return Task.FromResult(ToolResult.Fail("openai_text_to_audio 需配置 OpenAI API Key 后接入实现。"));
    }

    /// <summary>语音转文字（Whisper）。需配置 OpenAI API 后实现。</summary>
    protected virtual Task<ToolResult> AudioToTextAsync(Dictionary<string, object> parameters)
    {
        _ = parameters;
        return Task.FromResult(ToolResult.Fail("openai_audio_to_text 需配置 OpenAI API Key 后接入实现。"));
    }

    public override Dictionary<string, object> GetSchema()
    {
        return new Dictionary<string, object>
        {
            ["name"] = Name,
            ["description"] = Description,
            ["parameters"] = new Dictionary<string, object>
            {
                ["action"] = new Dictionary<string, object> { ["type"] = "string", ["description"] = "text_to_image | image_to_text | text_to_audio | audio_to_text", ["required"] = true },
                ["prompt"] = new Dictionary<string, object> { ["type"] = "string", ["description"] = "文本描述或提示", ["required"] = false },
                ["image_urls"] = new Dictionary<string, object> { ["type"] = "string", ["description"] = "图片 URL，逗号分隔", ["required"] = false },
                ["audio_url"] = new Dictionary<string, object> { ["type"] = "string", ["description"] = "音频 URL", ["required"] = false },
                ["model"] = new Dictionary<string, object> { ["type"] = "string", ["description"] = "模型名", ["required"] = false },
                ["size"] = new Dictionary<string, object> { ["type"] = "string", ["description"] = "图片尺寸，如 1024x1024", ["required"] = false }
            }
        };
    }
}
