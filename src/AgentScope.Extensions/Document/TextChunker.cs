namespace AgentScope.Extensions.Document;

/// <summary>
/// 文本分块工具。对标 Java TextChunker。
/// </summary>
public static class TextChunker
{
    public static IReadOnlyList<string> Split(string text, int chunkSize = 1000,
        int overlap = 200, SplitStrategy strategy = SplitStrategy.Paragraph)
    {
        var segments = strategy switch
        {
            SplitStrategy.Line => text.Split(['\n'], StringSplitOptions.RemoveEmptyEntries),
            SplitStrategy.Character => ChunkByCharacter(text, chunkSize, overlap),
            SplitStrategy.Token => ChunkByToken(text, chunkSize, overlap),
            _ => SplitByParagraph(text, chunkSize, overlap)
        };
        return segments;
    }

    private static IReadOnlyList<string> SplitByParagraph(string text, int size, int overlap)
    {
        var paragraphs = text.Split(["\n\n", "\r\n\r\n"], StringSplitOptions.RemoveEmptyEntries);
        return MergeChunks(paragraphs, size, overlap);
    }

    private static IReadOnlyList<string> ChunkByCharacter(string text, int size, int overlap)
    {
        var result = new List<string>();
        for (int i = 0; i < text.Length; i += size - overlap)
            result.Add(text.Substring(i, Math.Min(size, text.Length - i)));
        return result;
    }

    private static IReadOnlyList<string> ChunkByToken(string text, int size, int overlap)
    {
        // 近似：token ≈ 4 chars
        var charSize = size * 4;
        var charOverlap = overlap * 4;
        return ChunkByCharacter(text, charSize, charOverlap);
    }

    private static IReadOnlyList<string> MergeChunks(string[] parts, int size, int overlap)
    {
        var result = new List<string>();
        var current = new List<string>();
        var len = 0;

        foreach (var part in parts)
        {
            current.Add(part);
            len += part.Length;

            while (len > size && current.Count > 1)
            {
                len -= current[0].Length;
                current.RemoveAt(0);
            }

            if (len >= size)
            {
                result.Add(string.Concat(current));
                var keep = Math.Max(0, current.Count - overlap);
                if (keep < current.Count)
                {
                    var removed = current.Take(current.Count - keep).ToList();
                    len -= removed.Sum(x => x.Length);
                    current.RemoveRange(0, current.Count - keep);
                }
            }
        }

        if (current.Count > 0)
            result.Add(string.Concat(current));

        return result;
    }
}
