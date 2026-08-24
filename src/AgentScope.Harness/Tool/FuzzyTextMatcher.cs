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

namespace AgentScope.Harness.Tool;

/// <summary>
/// 模糊文本匹配器：基于子序列/编辑距离的相似度评分与检索。
/// 对应 Java: io.agentscope.harness.agent.tool.FuzzyTextMatcher
/// </summary>
public static class FuzzyTextMatcher
{
    /// <summary>计算 query 在 target 中的模糊匹配得分（0~1，1 为完全匹配）。</summary>
    public static double Score(string? query, string? target)
    {
        if (string.IsNullOrEmpty(query) || string.IsNullOrEmpty(target)) return 0;
        if (query.Equals(target, StringComparison.OrdinalIgnoreCase)) return 1;

        // 子序列匹配（fuzzy）加分
        var subsequenceRatio = SubsequenceRatio(query, target, out var matchLen);
        // 包含关系加分
        var containsBonus = target.Contains(query, StringComparison.OrdinalIgnoreCase) ? 0.3 : 0;
        // 大小写不敏感相等
        var lowerScore = target.ToLowerInvariant().Contains(query.ToLowerInvariant()) ? 0.2 : 0;

        var score = subsequenceRatio + containsBonus + lowerScore;
        return System.Math.Min(1.0, score);
    }

    /// <summary>在候选集合中检索匹配 query 的前 limit 个（按得分降序）。</summary>
    public static List<(string Item, double Score)> Search(
        string query, IEnumerable<string> candidates, double threshold = 0.3, int limit = 10)
    {
        if (string.IsNullOrEmpty(query)) return new();
        return candidates
            .Select(c => (Item: c, Score: Score(query, c)))
            .Where(x => x.Score >= threshold)
            .OrderByDescending(x => x.Score)
            .Take(limit)
            .ToList();
    }

    private static double SubsequenceRatio(string query, string target, out int matchLen)
    {
        matchLen = 0;
        var qi = 0;
        var qLower = query.ToLowerInvariant();
        var tLower = target.ToLowerInvariant();

        for (var ti = 0; ti < tLower.Length && qi < qLower.Length; ti++)
        {
            if (tLower[ti] == qLower[qi])
            {
                matchLen++;
                qi++;
            }
        }

        return qi == qLower.Length ? (double)matchLen / System.Math.Max(qLower.Length, tLower.Length) : 0;
    }
}
