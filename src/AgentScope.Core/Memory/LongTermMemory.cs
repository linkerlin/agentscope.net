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
using System.Threading.Tasks;
using AgentScope.Core.RAG;

namespace AgentScope.Core.Memory;

/// <summary>
/// Strategy used by long-term memory to store and retrieve facts.
/// </summary>
public enum LongTermMemoryMode
{
    Plaintext,
    Semantic,
    Hybrid
}

/// <summary>
/// Abstraction over a persistent, agent-wide long-term memory store.
/// </summary>
public interface ILongTermMemory
{
    Task AddAsync(string text, Dictionary<string, object>? metadata = null);
    Task<List<string>> SearchAsync(string query, int topK = 5);
    Task<string> SummarizeAsync();
}

/// <summary>
/// Simple in-memory implementation of <see cref="ILongTermMemory"/>.
/// Thread-safe via a private lock object.
///
/// 当注入 <see cref="IEmbeddingGenerator"/> 且模式为 Semantic/Hybrid 时，
/// 检索走 embedding 余弦相似度；Hybrid 为「向量召回 ∪ 子串召回」融合。
/// 未注入 embedding 时（或 Plaintext 模式）回退为子串匹配，保持向后兼容。
/// </summary>
public class InMemoryLongTermMemory : ILongTermMemory
{
    private readonly LongTermMemoryMode _mode;
    private readonly IEmbeddingGenerator? _embedding;
    private readonly object _lock = new();
    private readonly List<Entry> _entries = new();

    private sealed record Entry(string Text, float[]? Vector);

    public InMemoryLongTermMemory(
        LongTermMemoryMode mode = LongTermMemoryMode.Plaintext,
        IEmbeddingGenerator? embedding = null)
    {
        _mode = mode;
        _embedding = embedding;
    }

    public async Task AddAsync(string text, Dictionary<string, object>? metadata = null)
    {
        float[]? vector = null;
        if (_embedding != null && _mode != LongTermMemoryMode.Plaintext)
        {
            vector = await _embedding.GenerateEmbeddingAsync(text);
        }

        lock (_lock)
        {
            _entries.Add(new Entry(text, vector));
        }
    }

    public async Task<List<string>> SearchAsync(string query, int topK = 5)
    {
        List<Entry> snapshot;
        lock (_lock)
        {
            snapshot = _entries.ToList();
        }

        if (_embedding != null && _mode != LongTermMemoryMode.Plaintext)
        {
            var qVec = await _embedding.GenerateEmbeddingAsync(query);
            var vectorHits = snapshot
                .Where(e => e.Vector != null)
                .Select(e => (e.Text, Score: Cosine(qVec, e.Vector!)))
                .OrderByDescending(x => x.Score)
                .Select(x => x.Text)
                .ToList();

            if (_mode == LongTermMemoryMode.Semantic)
            {
                return vectorHits.Take(topK).ToList();
            }

            // Hybrid：向量召回 ∪ 子串召回，向量优先，去重后取 topK。
            var substringHits = snapshot
                .Where(e => e.Text.Contains(query, StringComparison.OrdinalIgnoreCase))
                .Select(e => e.Text)
                .ToList();

            return vectorHits
                .Concat(substringHits)
                .Distinct()
                .Take(topK)
                .ToList();
        }

        // Plaintext 或 未注入 embedding：子串匹配。
        return snapshot
            .Where(e => e.Text.Contains(query, StringComparison.OrdinalIgnoreCase))
            .Select(e => e.Text)
            .Take(topK)
            .ToList();
    }

    public Task<string> SummarizeAsync()
    {
        List<Entry> snapshot;
        lock (_lock)
        {
            snapshot = _entries.ToList();
        }

        return Task.FromResult(string.Join("\n", snapshot.Select(e => e.Text)));
    }

    private static double Cosine(float[] a, float[] b)
    {
        if (a.Length != b.Length)
        {
            return 0d;
        }

        double dot = 0, na = 0, nb = 0;
        for (int i = 0; i < a.Length; i++)
        {
            dot += a[i] * b[i];
            na += a[i] * a[i];
            nb += b[i] * b[i];
        }

        return (na == 0 || nb == 0) ? 0 : dot / (Math.Sqrt(na) * Math.Sqrt(nb));
    }
}
