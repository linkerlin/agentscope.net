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

namespace AgentScope.Harness.Memory;

/// <summary>
/// Memory management configuration. Counterpart of Java MemoryConfig.<br />
/// 内存管理配置。对标 Java MemoryConfig。
/// </summary>
public sealed record MemoryConfig
{
    /// <summary>Model name used for memory-related LLM calls / 用于记忆相关 LLM 调用的模型名称</summary>
    public string? ModelName { get; init; }

    /// <summary>Prompt template for extracting key facts, decisions and preferences / 提取关键事实、决策与偏好的提示词模板</summary>
    public string FlushPrompt { get; init; } = "提取对话中的关键事实、决策与偏好:";

    /// <summary>Prompt template for consolidating daily records into long-term summary / 将日记录合并为长期记忆摘要的提示词模板</summary>
    public string ConsolidationPrompt { get; init; } = "将以下每日记录合并为长期记忆摘要:";

    /// <summary>Maximum tokens for a consolidated summary / 合并摘要的最大 token 数</summary>
    public int ConsolidationMaxTokens { get; init; } = 2048;

    /// <summary>Minimum time gap between consolidations / 两次合并之间的最小时间间隔</summary>
    public TimeSpan? ConsolidationMinGap { get; init; } = TimeSpan.FromHours(1);

    /// <summary>Number of days to retain daily log files / 日记录文件的保留天数</summary>
    public int DailyFileRetentionDays { get; init; } = 30;

    /// <summary>Number of days to retain session data / 会话数据的保留天数</summary>
    public int SessionRetentionDays { get; init; } = 7;

    /// <summary>Flush trigger mode / 刷出触发模式</summary>
    public FlushTriggerMode FlushTrigger { get; init; } = FlushTriggerMode.Always;

    /// <summary>
    /// Determines when memory flush operations are triggered / 决定何时触发记忆刷出操作
    /// </summary>
    public enum FlushTriggerMode
    {
        /// <summary>Always flush / 始终刷出</summary>
        Always,
        /// <summary>Never flush / 从不刷出</summary>
        Never,
        /// <summary>Throttled flush based on conditions / 根据条件限流刷出</summary>
        Throttled
    }
}
