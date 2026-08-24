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

namespace AgentScope.Harness.Memory.Compaction;

/// <summary>
/// Character-based token estimation utility, suitable for compaction trigger decisions.<br />
/// 基于字符的 token 估算工具，适用于压缩触发判断。
/// </summary>
public static class TokenCounterUtil
{
    /// <summary>
    /// Estimated characters per token / 每个 token 对应的字符数估算值
    /// </summary>
    private const double CharsPerToken = 2.5;

    /// <summary>
    /// Estimates token count for a single text string.<br />
    /// 估算单段文本的 token 数。
    /// </summary>
    /// <param name="text">Text to estimate / 待估算的文本</param>
    /// <returns>Estimated token count / 估算的 token 数</returns>
    public static int EstimateTokenCount(string? text)
    {
        if (string.IsNullOrEmpty(text)) return 0;
        return (int)Math.Ceiling(text.Length / CharsPerToken);
    }

    /// <summary>
    /// Estimates total token count for a collection of texts.<br />
    /// 估算文本集合的总 token 数。
    /// </summary>
    /// <param name="texts">Texts to estimate / 待估算的文本集合</param>
    /// <returns>Estimated total token count / 估算的总 token 数</returns>
    public static int EstimateTokenCount(IEnumerable<string> texts)
    {
        int total = 0;
        foreach (var t in texts) total += EstimateTokenCount(t);
        return total;
    }

    /// <summary>
    /// Truncates text to fit within a target token limit (estimated by characters).<br />
    /// 将文本截断到目标 token 数以内（按字符估算）。
    /// </summary>
    /// <param name="text">Text to truncate / 待截断的文本</param>
    /// <param name="maxTokens">Maximum allowed tokens / 允许的最大 token 数</param>
    /// <returns>Truncated text / 截断后的文本</returns>
    public static string TruncateToTokenLimit(string text, int maxTokens)
    {
        var maxChars = (int)(maxTokens * CharsPerToken);
        return text.Length <= maxChars ? text : text[..maxChars];
    }
}
