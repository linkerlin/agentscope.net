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

namespace AgentScope.Core.Tool.Multimodal;

/// <summary>
/// OpenAI multimodal tool: text-to-image, image-to-text, text-to-audio, audio-to-text. Requires API configuration for real implementation.
/// OpenAI 多模态工具：文生图、图生文、文生语音、语音转文字等。需配置 API 后接入真实实现。
/// </summary>
public class OpenAIMultiModalTool : ToolBase
{
    private const string DefaultName = "openai_multimodal";
    private const string DefaultDescription = "OpenAI 多模态：text_to_image | image_to_text | text_to_audio | audio_to_text。参数: action, prompt(或 image_urls/audio_url), model, size 等。";

    /// <summary>
    /// Initializes a new instance of the <see cref="OpenAIMultiModalTool"/> class.
    /// 初始化 OpenAIMultiModalTool 实例。
    /// </summary>
    /// <param name="name">Optional tool name override / 可选工具名称</param>
    /// <param name="description">Optional description override / 可选描述</param>
    public OpenAIMultiModalTool(string? name = null, string? description = null)
        : base(name ?? DefaultName, description ?? DefaultDescription)
    {
    }

    /// <inheritdoc />
    public override async Task<ToolResult> ExecuteAsync(Dictionary<string, object> parameters)
    {
        // Validate action parameter / 校验 action 参数
        if (!parameters.TryGetValue("action", out var actionObj) || actionObj is not string action)
            return ToolResult.Fail("缺少参数: action（text_to_image | image_to_text | text_to_audio | audio_to_text）");

        // Route to specific handler based on action / 根据 action 分发到具体处理方法
        return action.ToLowerInvariant() switch
        {
            "text_to_image" => await TextToImageAsync(parameters).ConfigureAwait(false),
            "image_to_text" => await ImageToTextAsync(parameters).ConfigureAwait(false),
            "text_to_audio" => await TextToAudioAsync(parameters).ConfigureAwait(false),
            "audio_to_text" => await AudioToTextAsync(parameters).ConfigureAwait(false),
            _ => ToolResult.Fail("不支持的 action: " + action)
        };
    }

    /// <summary>
    /// Text-to-image generation (DALL-E). Requires OpenAI API configuration for real implementation.
    /// 文生图（DALL-E）。需配置 OpenAI API 后实现。
    /// </summary>
    /// <param name="parameters">Parameters including prompt, model, size / 包含 prompt、model、size 等参数</param>
    protected virtual Task<ToolResult> TextToImageAsync(Dictionary<string, object> parameters)
    {
        _ = parameters;
        return Task.FromResult(ToolResult.Fail("openai_text_to_image 需配置 OpenAI API Key 后接入实现。"));
    }

    /// <summary>
    /// Image-to-text analysis (GPT-4 Vision). Requires OpenAI API configuration for real implementation.
    /// 图生文（GPT-4 Vision）。需配置 OpenAI API 后实现。
    /// </summary>
    /// <param name="parameters">Parameters including image_urls, prompt / 包含 image_urls、prompt 等参数</param>
    protected virtual Task<ToolResult> ImageToTextAsync(Dictionary<string, object> parameters)
    {
        _ = parameters;
        return Task.FromResult(ToolResult.Fail("openai_image_to_text 需配置 OpenAI API Key 后接入实现。"));
    }

    /// <summary>
    /// Text-to-audio synthesis (TTS). Requires OpenAI API configuration for real implementation.
    /// 文生语音。需配置 OpenAI API 后实现。
    /// </summary>
    /// <param name="parameters">Parameters including prompt, model / 包含 prompt、model 等参数</param>
    protected virtual Task<ToolResult> TextToAudioAsync(Dictionary<string, object> parameters)
    {
        _ = parameters;
        return Task.FromResult(ToolResult.Fail("openai_text_to_audio 需配置 OpenAI API Key 后接入实现。"));
    }

    /// <summary>
    /// Audio-to-text transcription (Whisper). Requires OpenAI API configuration for real implementation.
    /// 语音转文字（Whisper）。需配置 OpenAI API 后实现。
    /// </summary>
    /// <param name="parameters">Parameters including audio_url / 包含 audio_url 等参数</param>
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
