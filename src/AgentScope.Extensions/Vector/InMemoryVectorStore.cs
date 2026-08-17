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

using System.Collections.Concurrent;
using System.Runtime.CompilerServices;

namespace AgentScope.Extensions.Vector;

/// <summary>
/// 内存向量存储。对标 Java InMemoryStore。
/// 纯进程内实现，适用于测试与单机部署。
/// </summary>
public sealed class InMemoryVectorStore(int dimension) : IVectorStore
{
    private readonly ConcurrentDictionary<string, Document> _docs = new();

    public int Dimension => dimension;

    public ValueTask UpsertAsync(string collection, string id, float[] vector,
        IDictionary<string, object>? payload = null, CancellationToken ct = default)
    {
        _docs[id] = new Document(vector, payload);
        return ValueTask.CompletedTask;
    }

    public async IAsyncEnumerable<SearchHit> SearchAsync(string collection, float[] query,
        int topK = 5, [EnumeratorCancellation] CancellationToken ct = default)
    {
        var scored = _docs
            .Select(kv => new { kv.Key, Score = CosineSimilarity(query, kv.Value.Vector) })
            .OrderByDescending(x => x.Score)
            .Take(topK);

        foreach (var hit in scored)
        {
            ct.ThrowIfCancellationRequested();
            var doc = _docs[hit.Key];
            yield return new SearchHit(hit.Key, (float)hit.Score,
                doc.Payload?.ToDictionary(k => k.Key, v => v.Value));
        }
    }

    public ValueTask DisposeAsync() { _docs.Clear(); return ValueTask.CompletedTask; }

    private static double CosineSimilarity(float[] a, float[] b)
    {
        double dot = 0, na = 0, nb = 0;
        for (int i = 0; i < a.Length; i++)
        {
            dot += a[i] * b[i];
            na += a[i] * a[i];
            nb += b[i] * b[i];
        }
        return dot / (Math.Sqrt(na) * Math.Sqrt(nb) + 1e-10);
    }

    private sealed record Document(float[] Vector, IDictionary<string, object>? Payload);
}
