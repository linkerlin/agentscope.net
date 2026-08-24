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

using System.Threading.Tasks;

namespace AgentScope.Core.Hook;

/// <summary>
/// Hook interface for extending agent behavior at various lifecycle stages.
/// 用于在 Agent 生命周期的各个阶段扩展行为的 Hook 接口。
/// 
/// Hooks follow the pipeline pattern: pre-reasoning → reasoning → post-reasoning → 
/// pre-acting → acting → post-acting → pre-summary → summary → post-summary.
/// Hook 遵循管道模式：推理前 → 推理 → 推理后 → 行动前 → 行动 → 行动后 → 摘要前 → 摘要 → 摘要后。
/// 
/// Corresponds to Java: io.agentscope.core.hook.IHook
/// 对应 Java: io.agentscope.core.hook.IHook
/// </summary>
public interface IHook
{
    /// <summary>
    /// Gets the unique name of this hook for identification and ordering.
    /// 获取此 Hook 的唯一名称，用于标识和排序。
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Called before the reasoning phase begins.
    /// 在推理阶段开始之前调用。
    /// Use this to inject context, modify input, or set up preconditions.
    /// 用于注入上下文、修改输入或设置前置条件。
    /// </summary>
    /// <param name="event">The pre-reasoning event data. 推理前事件数据。</param>
    Task OnPreReasoningAsync(PreReasoningEvent @event);

    /// <summary>
    /// Called after the reasoning phase completes.
    /// 在推理阶段完成之后调用。
    /// Use this to inspect or modify reasoning results.
    /// 用于检查或修改推理结果。
    /// </summary>
    /// <param name="event">The post-reasoning event data. 推理后事件数据。</param>
    Task OnPostReasoningAsync(PostReasoningEvent @event);

    /// <summary>
    /// Called before the acting (tool execution) phase begins.
    /// 在行动（工具执行）阶段开始之前调用。
    /// Use this to validate or modify tool call arguments.
    /// 用于验证或修改工具调用参数。
    /// </summary>
    /// <param name="event">The pre-acting event data. 行动前事件数据。</param>
    Task OnPreActingAsync(PreActingEvent @event);

    /// <summary>
    /// Called after the acting (tool execution) phase completes.
    /// 在行动（工具执行）阶段完成之后调用。
    /// Use this to inspect or modify tool execution results.
    /// 用于检查或修改工具执行结果。
    /// </summary>
    /// <param name="event">The post-acting event data. 行动后事件数据。</param>
    Task OnPostActingAsync(PostActingEvent @event);

    /// <summary>
    /// Called before the summary (final response generation) phase begins.
    /// 在摘要（最终响应生成）阶段开始之前调用。
    /// Use this to inject final instructions or modify context before response generation.
    /// 用于在生成响应前注入最终指令或修改上下文。
    /// </summary>
    /// <param name="event">The pre-summary event data. 摘要前事件数据。</param>
    Task OnPreSummaryAsync(PreSummaryEvent @event);

    /// <summary>
    /// Called after the summary (final response generation) phase completes.
    /// 在摘要（最终响应生成）阶段完成之后调用。
    /// Use this to post-process or log the final response.
    /// 用于后处理或记录最终响应。
    /// </summary>
    /// <param name="event">The post-summary event data. 摘要后事件数据。</param>
    Task OnPostSummaryAsync(PostSummaryEvent @event);

    /// <summary>
    /// Called for each reasoning chunk during streaming output.
    /// 在流式输出期间为每个推理块调用。
    /// Use this to stream intermediate thinking content to the client.
    /// 用于将中间思考内容流式传输到客户端。
    /// </summary>
    /// <param name="event">The reasoning chunk event data. 推理块事件数据。</param>
    Task OnReasoningChunkAsync(ReasoningChunkEvent @event);

    /// <summary>
    /// Called for each acting chunk during streaming output.
    /// 在流式输出期间为每个行动块调用。
    /// Use this to stream intermediate tool call information to the client.
    /// 用于将中间工具调用信息流式传输到客户端。
    /// </summary>
    /// <param name="event">The acting chunk event data. 行动块事件数据。</param>
    Task OnActingChunkAsync(ActingChunkEvent @event);

    /// <summary>
    /// Called for each summary chunk during streaming final response.
    /// 在流式最终响应期间为每个摘要块调用。
    /// Use this to stream the final response content to the client.
    /// 用于将最终响应内容流式传输到客户端。
    /// </summary>
    /// <param name="event">The summary chunk event data. 摘要块事件数据。</param>
    Task OnSummaryChunkAsync(SummaryChunkEvent @event);

    /// <summary>
    /// Called when an error occurs during any phase of agent execution.
    /// 在 Agent 执行的任何阶段发生错误时调用。
    /// Use this for error logging, fallback handling, or notification.
    /// 用于错误日志记录、回退处理或通知。
    /// </summary>
    /// <param name="event">The error hook event data. 错误 Hook 事件数据。</param>
    Task OnErrorAsync(ErrorHookEvent @event);
}
