namespace AgentScope.Harness.Memory.Compaction;

/// <summary>基于字符的 token 估算工具，适用于压缩触发判断</summary>
public static class TokenCounterUtil
{
    private const double CharsPerToken = 2.5;

    /// <summary>估算文本 token 数</summary>
    public static int EstimateTokenCount(string? text)
    {
        if (string.IsNullOrEmpty(text)) return 0;
        return (int)Math.Ceiling(text.Length / CharsPerToken);
    }

    /// <summary>估算消息列表总 token 数</summary>
    public static int EstimateTokenCount(IEnumerable<string> texts)
    {
        int total = 0;
        foreach (var t in texts) total += EstimateTokenCount(t);
        return total;
    }

    /// <summary>将文本截断到目标 token 数以内（按字符估算）</summary>
    public static string TruncateToTokenLimit(string text, int maxTokens)
    {
        var maxChars = (int)(maxTokens * CharsPerToken);
        return text.Length <= maxChars ? text : text[..maxChars];
    }
}
