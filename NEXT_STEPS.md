# 下一步实施指南 (Next Steps Guide)

## 当前位置

**项目进度**: 23/54 功能点 (42.6%)  
**当前任务**: Step 1.1 Phase 2 - OpenAI MessageConverter  
**最后更新**: 2026-02-17

## Phase 1 完成 ✅

已完成的工作：
- ✅ OpenAI DTO 模型（OpenAIMessage, OpenAIRequest, OpenAIResponse, OpenAITool）
- ✅ 20+ 数据结构
- ✅ 构建验证通过
- ✅ Java源码深度研究

## Phase 2-6 实施准备就绪

### Java 源码已研究完毕

关键文件位置（在 `/tmp/agentscope-java`）：
```
agentscope-core/src/main/java/io/agentscope/core/formatter/openai/
├── OpenAIMessageConverter.java (472行) ⭐ 核心转换器
├── OpenAIConverterUtils.java (115行) ⭐ 工具类
└── dto/
    ├── OpenAIContentPart.java (215行) ⭐ 多模态内容
    ├── OpenAIImageUrl.java
    ├── OpenAIVideoUrl.java
    ├── OpenAIInputAudio.java
    └── ... (其他DTO)
```

### Phase 2: OpenAI MessageConverter（立即开始）

#### 文件创建清单

1. **OpenAIContentPart.cs** (优先级1)
```csharp
// 位置: src/AgentScope.Core/Formatter/OpenAI/Dto/OpenAIContentPart.cs
// 参考: OpenAIContentPart.java (215行)
// 
// 需要实现:
// - Text content part
// - Image URL content part  
// - Video URL content part
// - Input audio content part
// - 静态工厂方法
// - Builder 模式
```

2. **OpenAIConverterUtils.cs** (优先级2)
```csharp
// 位置: src/AgentScope.Core/Formatter/OpenAI/OpenAIConverterUtils.cs
// 参考: OpenAIConverterUtils.java (115行)
//
// 需要实现:
// - ConvertImageSourceToUrl(Source source)
// - ConvertVideoSourceToUrl(Source source)
// - DetectAudioFormat(string mediaType)
```

3. **OpenAIMessageConverter.cs** (优先级3)
```csharp
// 位置: src/AgentScope.Core/Formatter/OpenAI/OpenAIMessageConverter.cs
// 参考: OpenAIMessageConverter.java (472行)
//
// 需要实现:
// - ConvertToMessage(Msg msg, bool hasMediaContent)
// - ConvertSystemMessage(Msg msg)
// - ConvertUserMessage(Msg msg, bool hasMediaContent)
// - ConvertAssistantMessage(Msg msg)
// - ConvertToolMessage(Msg msg)
// - ConvertContentBlocks(List<ContentBlock> blocks)
// - HasMediaContent(List<ContentBlock> blocks)
```

#### 关键实现细节

**多模态内容支持**:
- Text: 纯文本
- Image: URL 或 Base64 data URI
- Video: URL 或 Base64 data URI
- Audio: Base64 音频数据（input_audio格式）

**消息角色映射**:
- SYSTEM → "system"
- USER → "user"
- ASSISTANT → "assistant"
- TOOL → "tool"

**工具调用处理**:
- 从 ToolUseBlock 提取工具ID和名称
- 序列化参数为JSON
- 处理 thought_signature（Gemini需要）
- 构建 OpenAIToolCall 对象

**工具结果处理**:
- 从 ToolResultBlock 提取结果
- 支持多模态工具输出
- 设置正确的 tool_call_id

#### 单元测试计划

创建文件: `tests/AgentScope.Core.Tests/Formatter/OpenAI/MessageConverterTests.cs`

测试用例（15+）:
```csharp
// 基础消息转换
- TestConvertSystemMessage_WithTextContent
- TestConvertUserMessage_WithTextOnly
- TestConvertAssistantMessage_WithTextOnly

// 多模态消息
- TestConvertUserMessage_WithImageURL
- TestConvertUserMessage_WithImageBase64
- TestConvertUserMessage_WithVideoURL
- TestConvertUserMessage_WithAudioBase64

// 工具相关
- TestConvertAssistantMessage_WithToolCalls
- TestConvertToolMessage_WithTextResult
- TestConvertToolMessage_WithMultimodalResult

// 特殊情况
- TestConvertUserMessage_WithMixedContent
- TestConvertAssistantMessage_WithReasoningContent
- TestConvertMessage_WithEmptyContent
- TestConvertMessage_WithNullContent
- TestConvertMessage_WithUnsupportedContentBlock
```

### Phase 3-6 概要

**Phase 3: ResponseParser** (0.5天)
- 解析 OpenAIResponse → ChatResponse
- 提取工具调用
- 提取Reasoning内容
- Token统计

**Phase 4: BaseFormatter** (0.5天)
- 抽象格式化器基类
- 通用参数处理
- 工具模式转换

**Phase 5: ChatFormatter** (0.5天)
- 具体 Chat Completions 实现
- 完整参数映射
- 流式响应支持

**Phase 6: Integration Tests** (0.5天)
- 端到端测试
- 性能测试
- 与Java对比验证

## 实施建议

### 方法1: 分步增量（推荐）⭐

1. **第一步**: 创建 OpenAIContentPart.cs
   - 实现基础结构
   - 添加静态工厂方法
   - 构建验证

2. **第二步**: 创建 OpenAIConverterUtils.cs
   - 实现3个工具方法
   - 添加单元测试
   - 验证通过

3. **第三步**: 创建 OpenAIMessageConverter.cs（核心）
   - 先实现构造函数和基础框架
   - 逐个实现转换方法
   - 每个方法完成后立即测试

4. **第四步**: 完善测试
   - 添加所有测试用例
   - 确保覆盖率80%+
   - 边界情况测试

5. **第五步**: 集成验证
   - 与现有Agent集成
   - 运行集成测试
   - 性能验证

### 方法2: 完整实施（如果时间充足）

在单次会话中完成整个Phase 2-6，但需要：
- 3-5小时不间断时间
- 深入理解Java源码
- 快速调试能力
- 完整的测试验证

### 方法3: 团队协作

多人并行实施不同Phase：
- 人员1: Phase 2 (MessageConverter)
- 人员2: Phase 3 (ResponseParser)
- 人员3: Phase 4-5 (Formatter)
- 人员4: Phase 6 (Integration Tests)

## 代码模板

### OpenAIContentPart.cs 骨架

```csharp
// Copyright 2024-2026 the original author or authors.
// Licensed under the Apache License, Version 2.0

using System.Text.Json.Serialization;

namespace AgentScope.Core.Formatter.OpenAI.Dto;

/// <summary>
/// OpenAI content part DTO for multimodal messages
/// OpenAI 多模态消息的内容部分 DTO
/// 
/// 参考: io.agentscope.core.formatter.openai.dto.OpenAIContentPart
/// </summary>
public record OpenAIContentPart
{
    /// <summary>
    /// Content type: "text", "image_url", "video_url", or "input_audio"
    /// 内容类型："text"、"image_url"、"video_url" 或 "input_audio"
    /// </summary>
    [JsonPropertyName("type")]
    public required string Type { get; init; }

    /// <summary>
    /// Text content (for type="text")
    /// 文本内容（当 type="text" 时）
    /// </summary>
    [JsonPropertyName("text")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Text { get; init; }

    /// <summary>
    /// Image URL object (for type="image_url")
    /// 图片 URL 对象（当 type="image_url" 时）
    /// </summary>
    [JsonPropertyName("image_url")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public OpenAIImageUrl? ImageUrl { get; init; }

    /// <summary>
    /// Video URL object (for type="video_url")
    /// 视频 URL 对象（当 type="video_url" 时）
    /// </summary>
    [JsonPropertyName("video_url")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public OpenAIVideoUrl? VideoUrl { get; init; }

    /// <summary>
    /// Input audio object (for type="input_audio")
    /// 输入音频对象（当 type="input_audio" 时）
    /// </summary>
    [JsonPropertyName("input_audio")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public OpenAIInputAudio? InputAudio { get; init; }

    // 静态工厂方法
    public static OpenAIContentPart Text(string text) => new()
    {
        Type = "text",
        Text = text
    };

    public static OpenAIContentPart ImageUrl(string url) => new()
    {
        Type = "image_url",
        ImageUrl = new OpenAIImageUrl { Url = url }
    };

    // ... 其他工厂方法
}
```

### OpenAIConverterUtils.cs 骨架

```csharp
// Copyright 2024-2026 the original author or authors.
// Licensed under the Apache License, Version 2.0

namespace AgentScope.Core.Formatter.OpenAI;

/// <summary>
/// Utility class for OpenAI message conversion
/// OpenAI 消息转换工具类
/// 
/// 参考: io.agentscope.core.formatter.openai.OpenAIConverterUtils
/// </summary>
public static class OpenAIConverterUtils
{
    /// <summary>
    /// Convert image source to URL string
    /// 将图片源转换为 URL 字符串
    /// </summary>
    public static string ConvertImageSourceToUrl(ISource source)
    {
        // TODO: 实现
        throw new NotImplementedException();
    }

    /// <summary>
    /// Convert video source to URL string
    /// 将视频源转换为 URL 字符串
    /// </summary>
    public static string ConvertVideoSourceToUrl(ISource source)
    {
        // TODO: 实现
        throw new NotImplementedException();
    }

    /// <summary>
    /// Detect audio format from media type
    /// 从媒体类型检测音频格式
    /// </summary>
    public static string DetectAudioFormat(string? mediaType)
    {
        // TODO: 实现
        throw new NotImplementedException();
    }
}
```

## 验证清单

完成Phase 2后，检查：

### 功能验证
- [ ] 所有消息类型都能正确转换
- [ ] 多模态内容正确处理
- [ ] 工具调用格式正确
- [ ] 工具结果正确封装
- [ ] 空/null内容安全处理

### 测试验证
- [ ] 所有单元测试通过
- [ ] 测试覆盖率 >= 80%
- [ ] 边界情况有测试
- [ ] 错误场景有测试

### 代码质量
- [ ] 构建无警告
- [ ] 符合C#编码规范
- [ ] 中英文注释完整
- [ ] 标注Java源码位置

### 1:1对比
- [ ] API结构与Java一致
- [ ] 转换逻辑对应
- [ ] 错误处理方式相同
- [ ] 特殊情况处理对等

## 常见问题

**Q: ContentBlock是什么？**
A: ContentBlock是AgentScope中的内容块基类，包括TextBlock、ImageBlock、VideoBlock、AudioBlock、ToolUseBlock、ToolResultBlock等。需要先了解这些类的结构。

**Q: 如何处理Java中不存在的C#特性？**
A: 利用C#的优势，如：
- 使用record代替繁琐的POJO
- 使用模式匹配代替instanceof
- 使用扩展方法增强可读性

**Q: 如何保证与Java版本1:1对应？**
A: 
- 每个方法都标注Java源码位置
- 逐行对比核心逻辑
- 运行相同测试用例
- 对比API请求格式

**Q: 遇到不懂的Java代码怎么办？**
A: 
- 查看Java项目中的测试代码
- 搜索相关API文档
- 参考现有的C#实现
- 在注释中标记疑问，后续验证

## 资源链接

**项目文档**:
- [改进计划.md](./改进计划.md) - 完整实施计划
- [FEATURE_COMPARISON.md](./FEATURE_COMPARISON.md) - 功能对比
- [CURRENT_STATUS.md](./CURRENT_STATUS.md) - 当前状态

**Java源码** (在 `/tmp/agentscope-java`):
- OpenAIMessageConverter.java
- OpenAIContentPart.java
- OpenAIConverterUtils.java

**已完成的C#代码**:
- src/AgentScope.Core/Formatter/OpenAI/Dto/OpenAIMessage.cs
- src/AgentScope.Core/Formatter/OpenAI/Dto/OpenAIRequest.cs
- src/AgentScope.Core/Formatter/OpenAI/Dto/OpenAIResponse.cs
- src/AgentScope.Core/Formatter/OpenAI/Dto/OpenAITool.cs

## 预计时间

**Phase 2 详细时间分配**:
- OpenAIContentPart.cs: 1-2小时
- OpenAIConverterUtils.cs: 0.5-1小时
- OpenAIMessageConverter.cs: 2-3小时
- 单元测试: 1-2小时
- 调试和验证: 1小时

**总计**: 5.5-9小时（约1个工作日）

## 成功标准

Phase 2 完成的标志：
1. ✅ 所有3个核心文件创建并实现
2. ✅ 15+ 单元测试全部通过
3. ✅ 构建无警告
4. ✅ 与Java版本功能对等验证通过
5. ✅ 代码审查通过
6. ✅ 文档更新（改进计划.md标记Phase 2完成）

达到这些标准后，即可进入Phase 3。

---

**准备就绪！下次会话可以直接开始编码。** 🚀

**当前进度**: 23/54 (42.6%)  
**下一个里程碑**: 完成Step 1.1 → 进度将达到 29/54 (53.7%)
