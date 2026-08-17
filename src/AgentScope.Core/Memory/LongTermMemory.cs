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
/// 长期记忆用于存储和检索事实的策略模式。
/// Corresponds to Java: io.agentscope.memory.LongTermMemoryMode
/// </summary>
public enum LongTermMemoryMode
{
    /// <summary>
    /// Plaintext substring matching mode (no embedding required).
    /// 纯文本子串匹配模式（无需嵌入向量）。
    /// </summary>
    Plaintext,

    /// <summary>
    /// Semantic search mode using embedding cosine similarity.
    /// 使用嵌入向量余弦相似度的语义搜索模式。
    /// </summary>
    Semantic,

    /// <summary>
    /// Hybrid mode combining vector search and substring matching.
    /// 结合向量搜索和子串匹配的混合模式。
    /// </summary>
    Hybrid
}

/// <summary>
/// Abstraction over a persistent, agent-wide long-term memory store.
/// 持久化的、Agent 级别的长期记忆存储抽象。
/// Corresponds to Java: io.agentscope.memory.LongTermMemory
/// </summary>
public interface ILongTermMemory
{
    /// <summary>
    /// Adds a text fact to long-term memory.
    /// 向长期记忆中添加一条文本事实。
    /// </summary>
    /// <param name="text">The text content to store. / 要存储的文本内容。</param>
    /// <param name="metadata">Optional metadata associated with the text. / 可选的关联元数据。</param>
    Task AddAsync(string text, Dictionary<string, object>? metadata = null);

    /// <summary>
    /// Searches for relevant facts in long-term memory.
    /// 在长期记忆中搜索相关事实。
    /// </summary>
    /// <param name="query">The search query. / 搜索查询。</param>
    /// <param name="topK">Maximum number of results. / 最大结果数。</param>
    /// <returns>List of matching text results. / 匹配的文本结果列表。</returns>
    Task<List<string>> SearchAsync(string query, int topK = 5);

    /// <summary>
    /// Summarizes all stored facts into a single string.
    /// 将所有存储的事实汇总为单个字符串。
    /// </summary>
    /// <returns>A summary string of all facts. / 所有事实的汇总字符串。</returns>
    Task<string> SummarizeAsync();
}

/// <summary>
/// Simple in-memory implementation of <see cref="ILongTermMemory"/>.
/// Thread-safe via a private lock object.
///
/// ILongTermMemory 的简单内存实现。通过私有锁对象保证线程安全。
/// Corresponds to Java: io.agentscope.memory.InMemoryLongTermMemory
///
/// When <see cref="IEmbeddingGenerator"/> is injected and mode is Semantic/Hybrid,
/// retrieval uses embedding cosine similarity. Hybrid mode uses "vector recall ∪ substring recall" fusion.
/// When no embedding is injected (or Plaintext mode), falls back to substring matching for backward compatibility.
///
/// 当注入 IEmbeddingGenerator 且模式为 Semantic/Hybrid 时，
/// 检索走 embedding 余弦相似度；Hybrid 为「向量召回 ∪ 子串召回」融合。
/// 未注入 embedding 时（或 Plaintext 模式）回退为子串匹配，保持向后兼容。
/// </summary>
public class InMemoryLongTermMemory : ILongTermMemory
{
    /// <summary>
    /// The retrieval mode for this memory instance.
    /// 此记忆实例的检索模式。
    /// </summary>
    private readonly LongTermMemoryMode _mode;

    /// <summary>
    /// Optional embedding generator for semantic search.
    /// 可选的嵌入向量生成器，用于语义搜索。
    /// </summary>
    private readonly IEmbeddingGenerator? _embedding;

    /// <summary>
    /// Lock object for thread-safe access.
    /// 用于线程安全访问的锁对象。
    /// </summary>
    private readonly object _lock = new();

    /// <summary>
    /// Internal list of stored entries.
    /// 存储条目的内部列表。
    /// </summary>
    private readonly List<Entry> _entries = new();

    /// <summary>
    /// Internal record representing a stored fact with optional embedding vector.
    /// 表示一条存储事实及其可选嵌入向量的内部记录。
    /// </summary>
    private sealed record Entry(string Text, float[]? Vector);

    /// <summary>
    /// Initializes a new instance of InMemoryLongTermMemory.
    /// 初始化 InMemoryLongTermMemory 的新实例。
    /// </summary>
    /// <param name="mode">The retrieval mode. Default is Plaintext. / 检索模式，默认为 Plaintext。</param>
    /// <param name="embedding">Optional embedding generator. / 可选的嵌入向量生成器。</param>
    public InMemoryLongTermMemory(
        LongTermMemoryMode mode = LongTermMemoryMode.Plaintext,
        IEmbeddingGenerator? embedding = null)
    {
        _mode = mode;
        _embedding = embedding;
    }

    /// <inheritdoc />
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

    /// <inheritdoc />
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

            // Hybrid: vector recall ∪ substring recall, vector first, deduplicated, then take topK.
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

        // Plaintext or no embedding injected: substring matching.
        // Plaintext 或 未注入 embedding：子串匹配。
        return snapshot
            .Where(e => e.Text.Contains(query, StringComparison.OrdinalIgnoreCase))
            .Select(e => e.Text)
            .Take(topK)
            .ToList();
    }

    /// <inheritdoc />
    public Task<string> SummarizeAsync()
    {
        List<Entry> snapshot;
        lock (_lock)
        {
            snapshot = _entries.ToList();
        }

        return Task.FromResult(string.Join("\n", snapshot.Select(e => e.Text)));
    }

    /// <summary>
    /// Computes cosine similarity between two vectors.
    /// 计算两个向量之间的余弦相似度。
    /// </summary>
    /// <param name="a">First vector. / 第一个向量。</param>
    /// <param name="b">Second vector. / 第二个向量。</param>
    /// <returns>Cosine similarity score between 0 and 1. / 0 到 1 之间的余弦相似度分数。</returns>
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
