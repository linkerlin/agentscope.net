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
