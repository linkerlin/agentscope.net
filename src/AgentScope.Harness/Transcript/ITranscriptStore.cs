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

using System.Runtime.CompilerServices;

namespace AgentScope.Harness.Transcript;

/// <summary>
/// 会话转录存储。对标 Java TranscriptStore。
/// 以不可变分段（Immutable Segment）方式记录 Agent 执行过程。
/// </summary>
public interface ITranscriptStore
{
    Task AppendSegmentAsync(string sessionId, TranscriptSegment segment, CancellationToken ct = default);
    IAsyncEnumerable<TranscriptSegment> ListSegmentsAsync(string sessionId, CancellationToken ct = default);
    Task CompactAsync(string sessionId, CancellationToken ct = default);
    Task DeleteAsync(string sessionId, CancellationToken ct = default);
}

/// <summary>
/// 转录分段。对标 Java SegmentInfo。
/// </summary>
public readonly record struct TranscriptSegment(
    long SequenceStart,
    long SequenceEnd,
    string WriterId,
    string Content,
    DateTime CreatedAt);

/// <summary>
/// 文件系统转录存储。对标 Java FilesystemTranscriptStore。
/// 存储在 {baseDir}/{sessionId}/events/{seqStart}-{seqEnd}-{writerId}.jsonl
/// </summary>
public sealed class FilesystemTranscriptStore(string baseDir) : ITranscriptStore
{
    public async Task AppendSegmentAsync(string sessionId, TranscriptSegment segment, CancellationToken ct = default)
    {
        var dir = Path.Combine(baseDir, sessionId, "events");
        Directory.CreateDirectory(dir);
        var path = Path.Combine(dir, $"{segment.SequenceStart}-{segment.SequenceEnd}-{segment.WriterId}.jsonl");
        var json = System.Text.Json.JsonSerializer.Serialize(segment);
        await File.WriteAllTextAsync(path, json + '\n', ct);
    }

    public IAsyncEnumerable<TranscriptSegment> ListSegmentsAsync(string sessionId, CancellationToken ct = default)
    {
        return ListAsync(sessionId, ct);
    }

    private async IAsyncEnumerable<TranscriptSegment> ListAsync(string sessionId,
        [EnumeratorCancellation] CancellationToken ct)
    {
        var dir = Path.Combine(baseDir, sessionId, "events");
        if (!Directory.Exists(dir)) yield break;

        foreach (var file in Directory.GetFiles(dir, "*.jsonl").Order())
        {
            ct.ThrowIfCancellationRequested();
            var json = await File.ReadAllTextAsync(file, ct);
            var segment = System.Text.Json.JsonSerializer.Deserialize<TranscriptSegment?>(json);
            if (segment.HasValue) yield return segment.Value;
        }
    }

    public async Task CompactAsync(string sessionId, CancellationToken ct = default)
    {
        // 对标 Java FilesystemTranscriptStore.compact：合并所有分段为单段并删除旧文件
        var dir = Path.Combine(baseDir, sessionId, "events");
        if (!Directory.Exists(dir)) return;

        var files = Directory.GetFiles(dir, "*.jsonl").Order().ToList();
        if (files.Count <= 1) return;

        var segments = new List<TranscriptSegment>();
        foreach (var file in files)
        {
            ct.ThrowIfCancellationRequested();
            var json = await File.ReadAllTextAsync(file, ct);
            var segment = System.Text.Json.JsonSerializer.Deserialize<TranscriptSegment?>(json);
            if (segment.HasValue) segments.Add(segment.Value);
        }
        if (segments.Count <= 1) return;

        var ordered = segments.OrderBy(s => s.SequenceStart).ThenBy(s => s.SequenceEnd).ToList();
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

        // 写单段，删除旧分段文件
        var compactedPath = Path.Combine(dir, $"{compacted.SequenceStart}-{compacted.SequenceEnd}-compacted.jsonl");
        await File.WriteAllTextAsync(
            compactedPath,
            System.Text.Json.JsonSerializer.Serialize(compacted) + '\n', ct);

        foreach (var file in files)
        {
            if (string.Equals(file, compactedPath, StringComparison.Ordinal)) continue;
            try { File.Delete(file); }
            catch { /* 删除失败仅记录，不影响 compact 结果 */ }
        }
    }

    public Task DeleteAsync(string sessionId, CancellationToken ct = default)
    {
        var dir = Path.Combine(baseDir, sessionId);
        if (Directory.Exists(dir)) Directory.Delete(dir, true);
        return Task.CompletedTask;
    }
}
