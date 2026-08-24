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

using AgentScope.Core.Message;

namespace AgentScope.Core.A2A.Client.Message;

/// <summary>
/// ContentBlock to A2A Part parser. Corresponds to Java ContentBlockParser.
/// ContentBlock → A2A Part 解析器。对标 Java ContentBlockParser。
/// </summary>
public interface IContentBlockParser<in T> where T : ContentBlock
{
    /// <summary>
    /// Parses a ContentBlock into an A2A Part object.
    /// 将 ContentBlock 解析为 A2A Part 对象
    /// </summary>
    object Parse(T block);
}

/// <summary>
/// ContentBlockParser router. Corresponds to Java ContentBlockParserRouter.
/// Uses the strategy pattern to dispatch ContentBlocks to the appropriate parser by type.
/// ContentBlockParser 路由器。对标 Java ContentBlockParserRouter。
/// 使用策略模式将 ContentBlock 按类型分发到对应解析器。
/// </summary>
public sealed class ContentBlockParserRouter
{
    private readonly Dictionary<string, Func<ContentBlock, object>> _parsers = new();

    /// <summary>
    /// Registers a parser for the specified block type.
    /// 注册指定块类型的解析器
    /// </summary>
    public ContentBlockParserRouter Register<T>(string blockType, IContentBlockParser<T> parser)
        where T : ContentBlock
    {
        _parsers[blockType] = block => parser.Parse((T)block);
        return this;
    }

    /// <summary>
    /// Parses a ContentBlock by dispatching to the registered parser for its type.
    /// Falls back to a plain text Part if no parser is registered.
    /// 根据块类型分发到已注册的解析器。未注册时回退为纯文本 Part。
    /// </summary>
    public object Parse(ContentBlock block)
    {
        if (_parsers.TryGetValue(block.Type, out var parser))
            return parser(block);
        return new { type = "text", text = block.ToString() };
    }

    /// <summary>
    /// Creates a default router with built-in parsers for all known block types.
    /// 创建包含所有已知块类型内置解析器的默认路由器
    /// </summary>
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
