namespace AgentScope.Extensions.Document;

/// <summary>
/// 文本分块策略。对标 Java SplitStrategy。
/// </summary>
public enum SplitStrategy
{
    Character,
    Paragraph,
    Line,
    Token,
    Semantic
}
