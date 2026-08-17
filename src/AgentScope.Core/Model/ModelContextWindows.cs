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

namespace AgentScope.Core.Model;

/// <summary>
/// Mapping of known model context window sizes across different providers.
/// Used to determine the maximum number of tokens a model can process in a single request.
/// Supports wildcard matching (suffix *) for model families.
/// Corresponds to Java: io.agentscope.core.model.ModelContextWindows
/// 各厂商模型上下文窗口大小映射。
/// 用于确定模型在单个请求中可处理的最大 Token 数。
/// 支持通配符匹配（后缀 *）以匹配模型系列。
/// 对应 Java: io.agentscope.core.model.ModelContextWindows
/// </summary>
public static class ModelContextWindows
{
    /// <summary>
    /// Dictionary of known model context window sizes.
    /// Key: model identifier (supports wildcard suffix *), Value: context window size in tokens.
    /// 已知模型的上下文窗口大小字典。
    /// 键：模型标识符（支持通配符后缀 *），值：上下文窗口大小（Token 数）。
    /// </summary>
    public static readonly Dictionary<string, int> KnownWindows = new()
    {
        // OpenAI models / OpenAI 模型
        ["gpt-4o"] = 128000,
        ["gpt-4o-mini"] = 128000,
        ["gpt-4-turbo"] = 128000,
        ["gpt-4"] = 8192,
        ["gpt-3.5-turbo"] = 16384,
        ["o1*"] = 200000,
        ["o3-mini*"] = 200000,

        // Anthropic Claude models / Anthropic Claude 模型
        ["claude-sonnet-4-5-20250929"] = 200000,
        ["claude-sonnet-4*"] = 200000,
        ["claude-opus-4*"] = 200000,
        ["claude-3.5-sonnet*"] = 200000,
        ["claude-3-haiku*"] = 200000,

        // DeepSeek models / DeepSeek 模型
        ["deepseek-chat"] = 65536,
        ["deepseek-reasoner"] = 65536,

        // Gemini models / Gemini 模型
        ["gemini-2.0-flash*"] = 1048576,
        ["gemini-1.5-pro*"] = 1048576,
        ["gemini-1.5-flash*"] = 1048576,

        // DashScope / Qwen models / DashScope / Qwen 模型
        ["qwen-turbo"] = 131072,
        ["qwen-plus"] = 131072,
        ["qwen-max"] = 131072,

        // Ollama common models / Ollama 常用模型
        ["llama3*"] = 8192,
        ["mistral*"] = 8192,
        ["qwen2*"] = 32768,
    };

    /// <summary>
    /// Gets the context window size for the specified model.
    /// Returns null for unknown models.
    /// Supports wildcard matching (patterns ending with *).
    /// 获取指定模型的上下文窗口大小，未知模型返回 null。
    /// 支持通配符匹配（以 * 结尾的模式）。
    /// </summary>
    /// <param name="modelId">Model identifier (e.g., "gpt-4o", "claude-3") / 模型标识符。</param>
    /// <returns>Context window size in tokens, or null if unknown / 上下文窗口大小（Token 数），未知则返回 null。</returns>
    public static int? GetWindowSize(string modelId)
    {
        if (string.IsNullOrEmpty(modelId)) return null;

        // Try exact match first / 先尝试精确匹配
        if (KnownWindows.TryGetValue(modelId, out var size))
        {
            return size;
        }

        // Try wildcard pattern matching / 尝试通配符模式匹配
        foreach (var (pattern, window) in KnownWindows)
        {
            if (pattern.EndsWith("*") && modelId.StartsWith(pattern.TrimEnd('*')))
            {
                return window;
            }
        }

        return null;
    }
}
