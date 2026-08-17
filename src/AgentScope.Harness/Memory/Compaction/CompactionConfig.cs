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

namespace AgentScope.Harness.Memory.Compaction;

/// <summary>
/// Conversation compaction configuration controlling when and how to compact conversation history.<br />
/// 对话压缩配置，控制何时以及如何压缩对话历史。
/// </summary>
public sealed record CompactionConfig
{
    /// <summary>Message count threshold to trigger compaction / 触发压缩的消息数阈值</summary>
    public int TriggerMessageCount { get; init; } = 50;

    /// <summary>Token count threshold to trigger compaction / 触发压缩的 token 数阈值</summary>
    public int TriggerTokenCount { get; init; } = 8000;

    /// <summary>Maximum message count to retain after compaction / 压缩后保留的最大消息数</summary>
    public int TargetMessageCount { get; init; } = 20;

    /// <summary>Maximum token count to retain after compaction / 压缩后保留的最大 token 数</summary>
    public int TargetTokenCount { get; init; } = 3000;

    /// <summary>Compaction mode / 压缩模式</summary>
    public CompactionMode Mode { get; init; } = CompactionMode.Adaptive;

    /// <summary>Tool argument truncation configuration / 工具参数截断配置</summary>
    public TruncateArgsConfig? TruncateArgs { get; init; }

    /// <summary>Tool result pruning configuration / 工具结果剪枝配置</summary>
    public PruneConfig? Prune { get; init; }

    /// <summary>LLM summarization configuration / LLM 汇总配置</summary>
    public SummarizeConfig? Summarize { get; init; }
}

/// <summary>
/// Defines the compaction strategy mode / 定义压缩策略模式
/// </summary>
public enum CompactionMode
{
    /// <summary>Truncate arguments only / 仅截断参数</summary>
    TruncateOnly,
    /// <summary>Prune tool results only / 仅剪枝工具结果</summary>
    PruneOnly,
    /// <summary>LLM summarization only / 仅 LLM 汇总</summary>
    SummarizeOnly,
    /// <summary>Adaptive combination of strategies / 自适应组合策略</summary>
    Adaptive
}

/// <summary>
/// Configuration for truncating overly long tool call arguments.<br />
/// 参数截断配置：截断过长的工具调用参数。
/// </summary>
public sealed record TruncateArgsConfig
{
    /// <summary>Whether truncation is enabled / 是否启用截断</summary>
    public bool Enabled { get; init; } = true;

    /// <summary>Maximum argument string length / 参数字符串最大长度</summary>
    public int MaxArgLength { get; init; } = 500;

    /// <summary>Maximum arguments per tool call / 每次工具调用的最大参数数</summary>
    public int MaxArgsPerCall { get; init; } = 20;
}

/// <summary>
/// Configuration for pruning large tool execution results.<br />
/// 工具结果剪枝配置：移除或缩减大型工具输出。
/// </summary>
public sealed record PruneConfig
{
    /// <summary>Whether pruning is enabled / 是否启用剪枝</summary>
    public bool Enabled { get; init; } = true;

    /// <summary>Maximum result string length / 结果字符串最大长度</summary>
    public int MaxResultLength { get; init; } = 1000;

    /// <summary>Maximum results per message / 每条消息的最大结果数</summary>
    public int MaxResultsPerMessage { get; init; } = 10;

    /// <summary>Whether to keep error results even when pruning / 是否在剪枝时保留错误结果</summary>
    public bool KeepErrorResults { get; init; } = true;
}

/// <summary>
/// Configuration for using LLM to generate conversation summaries instead of raw messages.<br />
/// LLM 汇总配置：使用 LLM 生成对话摘要代替原始消息。
/// </summary>
public sealed record SummarizeConfig
{
    /// <summary>Whether summarization is enabled / 是否启用汇总</summary>
    public bool Enabled { get; init; } = false;

    /// <summary>Model name to use for summarization / 用于汇总的模型名称</summary>
    public string? ModelName { get; init; }

    /// <summary>Maximum tokens for the summary / 摘要的最大 token 数</summary>
    public int MaxSummaryTokens { get; init; } = 500;

    /// <summary>Prompt template for summarization / 汇总提示词模板</summary>
    public string SummaryPrompt { get; init; } =
        "请将以下对话压缩为简洁的摘要，保留关键信息、决策和工具调用结果。";
}
