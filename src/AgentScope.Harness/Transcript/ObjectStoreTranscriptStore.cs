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
using System.Text.Json;
namespace AgentScope.Harness.Transcript;

public sealed class ObjectStoreTranscriptStore : ITranscriptStore
{
    private readonly ConcurrentDictionary<string, List<TranscriptSegment>> _store = new();

    public Task AppendSegmentAsync(string sessionId, TranscriptSegment segment, CancellationToken ct = default)
    {
        _store.AddOrUpdate(sessionId,
            _ => new List<TranscriptSegment> { segment },
            (_, list) => { list.Add(segment); return list; });
        return Task.CompletedTask;
    }

    public async IAsyncEnumerable<TranscriptSegment> ListSegmentsAsync(string sessionId, CancellationToken ct = default)
    {
        if (_store.TryGetValue(sessionId, out var list))
            foreach (var seg in list)
                yield return seg;
    }

    public async Task CompactAsync(string sessionId, CancellationToken ct = default)
    {
        // 对标 Java ObjectStoreTranscriptStore.compact：合并分段为单段并删除旧段
        if (!_store.TryGetValue(sessionId, out var list) || list.Count <= 1)
            return;

        var ordered = list.OrderBy(s => s.SequenceStart).ThenBy(s => s.SequenceEnd).ToList();
        var merged = new System.Text.StringBuilder();
        foreach (var seg in ordered)
        {
            merged.Append(seg.Content);
            if (merged.Length > 0 && merged[^1] != '\n')
                merged.Append('\n');
        }

        var compacted = new TranscriptSegment(
            SequenceStart: ordered[0].SequenceStart,
            SequenceEnd: ordered[^1].SequenceEnd,
            WriterId: "compacted",
            Content: merged.ToString(),
            CreatedAt: DateTime.UtcNow);

        _store[sessionId] = new List<TranscriptSegment> { compacted };
    }

    public Task DeleteAsync(string sessionId, CancellationToken ct = default)
    {
        _store.TryRemove(sessionId, out _);
        return Task.CompletedTask;
    }
}
