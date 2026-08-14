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
/// 各厂商模型上下文窗口大小映射。
/// 对标 Java ModelContextWindows。
/// 已知模型的上下文窗口大小（Token 数）。
/// key: 模型标识符（支持通配符后缀 *），value: 上下文窗口大小
/// </summary>
public static class ModelContextWindows
{
    public static readonly Dictionary<string, int> KnownWindows = new()
    {
        // OpenAI
        ["gpt-4o"] = 128000,
        ["gpt-4o-mini"] = 128000,
        ["gpt-4-turbo"] = 128000,
        ["gpt-4"] = 8192,
        ["gpt-3.5-turbo"] = 16384,
        ["o1*"] = 200000,
        ["o3-mini*"] = 200000,

        // Anthropic
        ["claude-sonnet-4-5-20250929"] = 200000,
        ["claude-sonnet-4*"] = 200000,
        ["claude-opus-4*"] = 200000,
        ["claude-3.5-sonnet*"] = 200000,
        ["claude-3-haiku*"] = 200000,

        // DeepSeek
        ["deepseek-chat"] = 65536,
        ["deepseek-reasoner"] = 65536,

        // Gemini
        ["gemini-2.0-flash*"] = 1048576,
        ["gemini-1.5-pro*"] = 1048576,
        ["gemini-1.5-flash*"] = 1048576,

        // DashScope / Qwen
        ["qwen-turbo"] = 131072,
        ["qwen-plus"] = 131072,
        ["qwen-max"] = 131072,

        // Ollama 常用模型
        ["llama3*"] = 8192,
        ["mistral*"] = 8192,
        ["qwen2*"] = 32768,
    };

    /// <summary>
    /// 获取指定模型的上下文窗口大小，未知模型返回 null。
    /// 支持通配符匹配（以 * 结尾）。
    /// </summary>
    public static int? GetWindowSize(string modelId)
    {
        if (string.IsNullOrEmpty(modelId)) return null;

        if (KnownWindows.TryGetValue(modelId, out var size))
        {
            return size;
        }

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
