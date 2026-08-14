using AgentScope.Core.Message;

namespace AgentScope.Core.A2A.Client.Message;

/// <summary>
/// ContentBlock → A2A Part 解析器。对标 Java ContentBlockParser。
/// </summary>
public interface IContentBlockParser<in T> where T : ContentBlock
{
    object Parse(T block);
}

/// <summary>
/// ContentBlockParser 路由器。对标 Java ContentBlockParserRouter。
/// 使用策略模式将 ContentBlock 按类型分发到对应解析器。
/// </summary>
public sealed class ContentBlockParserRouter
{
    private readonly Dictionary<string, Func<ContentBlock, object>> _parsers = new();

    public ContentBlockParserRouter Register<T>(string blockType, IContentBlockParser<T> parser)
        where T : ContentBlock
    {
        _parsers[blockType] = block => parser.Parse((T)block);
        return this;
    }

    public object Parse(ContentBlock block)
    {
        if (_parsers.TryGetValue(block.Type, out var parser))
            return parser(block);
        return new { type = "text", text = block.ToString() };
    }

    public static ContentBlockParserRouter CreateDefault()
    {
        var router = new ContentBlockParserRouter();
        router.Register("text", new TextBlockParser());
        router.Register("thinking", new ThinkingBlockParser());
        router.Register("image", new ImageBlockParser());
        router.Register("audio", new AudioBlockParser());
        router.Register("video", new VideoBlockParser());
        router.Register("tool_use", new ToolUseBlockParser());
        router.Register("tool_result", new ToolResultBlockParser());
        return router;
    }
}
