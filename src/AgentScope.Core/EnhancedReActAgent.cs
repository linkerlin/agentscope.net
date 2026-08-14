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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using AgentScope.Core.Agent;
using AgentScope.Core.Events;
using AgentScope.Core.Hook;
using AgentScope.Core.Interruption;
using AgentScope.Core.Memory;
using AgentScope.Core.Message;
using AgentScope.Core.Model;
using AgentScope.Core.Permission;
using AgentScope.Core.State;
using AgentScope.Core.Tool;
using AgentEvent = AgentScope.Core.Events.Event;
using AgentEventType = AgentScope.Core.Events.EventType;

namespace AgentScope.Core;

/// <summary>
/// 增强版 ReAct (Reasoning and Acting) Agent 实现
/// Enhanced ReAct Agent with complete tool execution loop and hook support
/// 
/// ReAct 循环：
/// 1. Reasoning（推理）：Agent 分析当前情况，决定下一步行动
/// 2. Acting（行动）：Agent 执行工具或返回最终答案
/// 3. Observation（观察）：获取行动结果，继续循环或结束
/// </summary>
public class EnhancedReActAgent : InterruptibleAgentBase, IStreamableAgent, IStateModule, IStructuredOutputCapableAgent
{
    private const string AgentMetaStateKeyPrefix = "state::enhanced_react::agent_meta::";
    private const string MemoryStateKeyPrefix = "state::enhanced_react::memory::";
    private const string ToolkitStateKeyPrefix = "state::enhanced_react::toolkit::";

    private readonly IModel _model;
    private readonly IMemory _memory;
    private readonly Dictionary<string, ITool> _tools;
    private readonly ToolGroupManager? _toolGroupManager;
    private string _systemPrompt;
    private readonly StatePersistence _statePersistence;
    private readonly int _maxIterations;
    private readonly HookManager _hookManager;
    private readonly IPermissionEngine? _permission;
    private readonly bool _verbose;

    internal EnhancedReActAgent(
        string name, 
        IModel model, 
        string systemPrompt, 
        IMemory? memory = null, 
        Dictionary<string, ITool>? tools = null, 
        ToolGroupManager? toolGroupManager = null,
        StatePersistence? statePersistence = null,
        int maxIterations = 10,
        HookManager? hookManager = null,
        IPermissionEngine? permission = null,
        bool verbose = false)
        : base(name, $"EnhancedReActAgent: {systemPrompt}")
    {
        _model = model;
        _systemPrompt = systemPrompt;
        _memory = memory ?? new MemoryBase();
        _tools = tools ?? new Dictionary<string, ITool>();
        _toolGroupManager = toolGroupManager;
        _statePersistence = statePersistence ?? StatePersistence.All;
        _maxIterations = maxIterations;
        _hookManager = hookManager ?? new HookManager();
        _permission = permission;
        _verbose = verbose;
    }

    /// <summary>
    /// 系统提示词。开放读写以支持中间件在回合开始前注入上下文
    /// （对标 Java <c>Middleware.onSystemPrompt</c> 的可改写语义）。
    /// </summary>
    public string SystemPrompt
    {
        get => _systemPrompt;
        set => _systemPrompt = value ?? string.Empty;
    }

    /// <summary>
    /// HITL 确认回调。当 <see cref="IPermissionEngine"/> 判定为
    /// <see cref="PermissionBehavior.Ask"/> 时被调用，返回用户决策。
    /// <para>
    /// 为 null 时行为取决于 <see cref="AutoApproveOnAsk"/>：
    /// 默认（false）走内置控制台交互；宿主为非交互进程时可设为 true 直接放行。
    /// </para>
    /// </summary>
    public Func<RequireUserConfirmEvent, Task<ConfirmResult>>? ConfirmCallback { get; set; }

    /// <summary>
    /// 未配置 <see cref="ConfirmCallback"/> 且无可用控制台输入时，是否自动批准。
    /// 默认 false（更安全：无法确认则拒绝）。
    /// </summary>
    public bool AutoApproveOnAsk { get; set; }

    /// <summary>
    /// 发起一次用户确认。优先使用注入的 <see cref="ConfirmCallback"/>，
    /// 否则回退到控制台交互；两者都不可用时按 <see cref="AutoApproveOnAsk"/> 决定。
    /// </summary>
    private async Task<ConfirmResult> RequestUserConfirmAsync(
        string toolName, Dictionary<string, object>? arguments, string? reason)
    {
        var evt = new RequireUserConfirmEvent(Guid.NewGuid().ToString("N"), toolName, arguments);

        if (ConfirmCallback != null)
        {
            try
            {
                return await ConfirmCallback(evt).ConfigureAwait(false);
            }
            catch (System.Exception ex)
            {
                return ConfirmResult.Deny($"确认回调异常: {ex.Message}");
            }
        }

        return ConsoleConfirm(toolName, arguments, reason);
    }

    /// <summary>内置控制台确认。重定向输入（无交互终端）时按 AutoApproveOnAsk 处理。</summary>
    private ConfirmResult ConsoleConfirm(
        string toolName, Dictionary<string, object>? arguments, string? reason)
    {
        if (Console.IsInputRedirected)
        {
            return AutoApproveOnAsk
                ? ConfirmResult.Approve()
                : ConfirmResult.Deny("无交互终端且未配置确认回调");
        }

        var argText = arguments is { Count: > 0 }
            ? string.Join(", ", arguments.Select(kv => $"{kv.Key}={kv.Value}"))
            : "(无参数)";

        Console.WriteLine();
        Console.WriteLine($"[需要确认] 工具 '{toolName}' 请求执行");
        Console.WriteLine($"  参数: {argText}");
        if (!string.IsNullOrWhiteSpace(reason)) Console.WriteLine($"  原因: {reason}");
        Console.Write("  批准执行？(y/N): ");

        var answer = Console.ReadLine()?.Trim();
        var approved = string.Equals(answer, "y", StringComparison.OrdinalIgnoreCase)
                       || string.Equals(answer, "yes", StringComparison.OrdinalIgnoreCase);

        return approved ? ConfirmResult.Approve() : ConfirmResult.Deny("用户在控制台拒绝");
    }

    [Obsolete("使用 StreamEventsAsync 替代")]
    public async IAsyncEnumerable<AgentEvent> StreamAsync(IEnumerable<Msg> messages, StreamOptions options)
    {
        options ??= new StreamOptions();
        options.CancellationToken.ThrowIfCancellationRequested();

        var list = messages as IList<Msg> ?? messages.ToList();
        if (list.Count == 0)
        {
            yield return new AgentEvent(AgentEventType.ReasoningFinish, null, true);
            yield break;
        }

        var userMessage = list[list.Count - 1];
        _memory.Add(userMessage);

        Msg? finalMessage = null;
        await foreach (var ev in ProcessWithReActLoopStreamAsync(userMessage, options).ConfigureAwait(false))
        {
            if (ev.IsLast && ev.Message != null)
            {
                finalMessage = ev.Message;
            }

            yield return ev;
        }

        if (finalMessage != null)
        {
            _memory.Add(finalMessage);
        }
    }

    [Obsolete("使用 StreamEventsAsync 替代")]
    public async IAsyncEnumerable<AgentEvent> StreamAsync(Msg message, StreamOptions? options = null)
    {
        options ??= new StreamOptions();
        await foreach (var ev in StreamAsync(new[] { message }, options).ConfigureAwait(false))
        {
            yield return ev;
        }
    }

    /// <summary>
    /// 实现 AgentBase.DoCallAsync
    /// </summary>
    protected override async Task<Msg> DoCallAsync(IReadOnlyList<Msg> messages)
    {
        var msg = messages.Count > 0 ? messages[messages.Count - 1]
            : Msg.Builder().Role("user").TextContent("").Build();
        return await ProcessWithReActLoopAsync(msg);
    }

    /// <summary>
    /// IStreamableAgent.StreamEventsAsync 实现
    /// </summary>
#pragma warning disable CS0618
    public override async IAsyncEnumerable<Event> StreamEventsAsync(IReadOnlyList<Msg> messages, RuntimeContext? context = null)
    {
        var options = new StreamOptions();
        await foreach (var ev in StreamAsync(messages, options))
        {
            yield return ev;
        }
    }
#pragma warning restore CS0618

    public override async IAsyncEnumerable<Event> StreamEventsAsync(Msg message, RuntimeContext? context = null)
    {
        await foreach (var ev in StreamEventsAsync(new[] { message }, context))
        {
            yield return ev;
        }
    }

    /// <summary>
    /// 实现 InterruptibleAgentBase.ExecuteAsync，通过 CancellationToken 支持中断
    /// </summary>
    protected override async Task<Msg> ExecuteAsync(IReadOnlyList<Msg> messages, CancellationToken ct)
    {
        var msg = messages.Count > 0 ? messages[messages.Count - 1] : Msg.Builder().Role("user").TextContent("").Build();
        _memory.Add(msg);
        var response = await ProcessWithReActLoopAsync(msg);
        _memory.Add(response);
        return response;
    }

    /// <summary>
    /// 实现 IStructuredOutputCapableAgent：使用系统提示约束模型输出为 JSON 格式并反序列化
    /// </summary>
    public async Task<T> GenerateStructuredOutputAsync<T>(IEnumerable<Msg> messages)
    {
        var msgList = messages.ToList();
        var jsonPrompt = Msg.Builder()
            .Role("system")
            .TextContent($"你必须输出合法的 JSON，且仅输出 JSON，可直接被 System.Text.Json 反序列化为 {typeof(T).Name}。不要包含 markdown 代码块标记。")
            .Build();

        var allMessages = new List<Msg> { jsonPrompt };
        allMessages.AddRange(msgList);

        var request = new ModelRequest { Messages = allMessages };
        var response = await _model.GenerateAsync(request);

        if (!response.Success)
        {
            throw new ModelException($"结构化输出生成失败: {response.Error}");
        }

        var text = response.Text ?? throw new ModelException("模型返回空响应");

        // 尝试提取 JSON（移除可能的 markdown 标记）
        var jsonStart = text.IndexOf('{');
        var jsonEnd = text.LastIndexOf('}');
        if (jsonStart >= 0 && jsonEnd > jsonStart)
        {
            text = text[jsonStart..(jsonEnd + 1)];
        }

        return System.Text.Json.JsonSerializer.Deserialize<T>(text)
            ?? throw new ModelException($"无法反序列化为 {typeof(T).Name}");
    }

    /// <summary>
    /// 实现 IStructuredOutputCapableAgent：流式版本
    /// </summary>
    public async IAsyncEnumerable<AgentEvent> StreamStructuredOutputAsync<T>(
        IEnumerable<Msg> messages, StreamOptions options)
    {
        var result = await GenerateStructuredOutputAsync<T>(messages);
        yield return new AgentEvent(
            AgentEventType.ReasoningFinish,
            Msg.Builder().Role("assistant").TextContent(
                System.Text.Json.JsonSerializer.Serialize(result)).Build(),
            true);
    }

    /// <summary>
    /// 使用 ReAct 循环处理消息
    /// Process message with ReAct loop
    /// </summary>
    private async Task<Msg> ProcessWithReActLoopAsync(Msg userMessage)
    {
        var iteration = 0;
        var continueLoop = true;
        var finalResponse = "";
        var thoughtHistory = new List<string>();

        while (continueLoop && iteration < _maxIterations)
        {
            CheckCancellation();
            iteration++;
            
            if (_verbose)
            {
                Console.WriteLine($"\n=== ReAct 迭代 Iteration {iteration}/{_maxIterations} ===");
            }

            // 阶段 1: Reasoning（推理）
            var reasoning = await ReasoningPhaseAsync(userMessage, thoughtHistory, iteration);
            
            if (reasoning.IsError)
            {
                return CreateErrorResponse(reasoning.ErrorMessage!);
            }

            thoughtHistory.Add($"Thought {iteration}: {reasoning.Thought}");

            // 阶段 2: Acting（行动）
            var action = await ActingPhaseAsync(reasoning);
            
            if (action.IsFinish)
            {
                finalResponse = action.FinalAnswer!;
                continueLoop = false;
            }
            else if (action.IsToolCall)
            {
                // 阶段 3: Observation（观察）
                var observation = await ObservationPhaseAsync(action);
                thoughtHistory.Add($"Observation {iteration}: {observation}");
            }
            else if (action.IsError)
            {
                return CreateErrorResponse(action.ErrorMessage!);
            }
        }

        if (iteration >= _maxIterations && string.IsNullOrEmpty(finalResponse))
        {
            finalResponse = "达到最大迭代次数，无法得出结论。Reached maximum iterations without conclusion.";
        }

        finalResponse = await ExecuteSummaryPhaseAsync(
            BuildAssistantChunkMessage(finalResponse),
            finalResponse).ConfigureAwait(false);

        return CreateFinalResponse(finalResponse, iteration, thoughtHistory);
    }

    private async IAsyncEnumerable<AgentEvent> ProcessWithReActLoopStreamAsync(Msg userMessage, StreamOptions options)
    {
        var iteration = 0;
        var thoughtHistory = new List<string>();

        while (iteration < _maxIterations)
        {
            CheckCancellation();
            options.CancellationToken.ThrowIfCancellationRequested();
            iteration++;

            if (_verbose)
            {
                Console.WriteLine($"\n=== ReAct 流式迭代 Iteration {iteration}/{_maxIterations} ===");
            }

            var reasoningPhase = await ExecuteStreamingReasoningPhaseAsync(userMessage, thoughtHistory, iteration, options).ConfigureAwait(false);
            foreach (var ev in reasoningPhase.Events)
            {
                yield return ev;
            }

            if (reasoningPhase.ShouldStop)
            {
                yield break;
            }

            var reasoning = reasoningPhase.Result;
            thoughtHistory.Add($"Thought {iteration}: {reasoning.Thought}");

            var actionPhase = await ExecuteStreamingActionPhaseAsync(userMessage, reasoning, thoughtHistory, iteration, options).ConfigureAwait(false);
            foreach (var ev in actionPhase.Events)
            {
                yield return ev;
            }

            if (actionPhase.ShouldStop)
            {
                yield break;
            }

            if (!string.IsNullOrEmpty(actionPhase.Observation))
            {
                thoughtHistory.Add($"Observation {iteration}: {actionPhase.Observation}");
            }
        }

        var finalResponse = "达到最大迭代次数，无法得出结论。Reached maximum iterations without conclusion.";
        yield return new AgentEvent(
            AgentEventType.ActingFinish,
            null,
            false,
            CreatePhaseMetadata(iteration, "acting_finish"));

        foreach (var ev in await ExecuteStreamingSummaryPhaseAsync(
            BuildAssistantChunkMessage(finalResponse),
            finalResponse,
            iteration,
            thoughtHistory).ConfigureAwait(false))
        {
            yield return ev;
        }
    }

    private async Task<(ReasoningResult Result, List<AgentEvent> Events, bool ShouldStop)> ExecuteStreamingReasoningPhaseAsync(
        Msg userMessage,
        List<string> thoughtHistory,
        int iteration,
        StreamOptions options)
    {
        var events = new List<AgentEvent>();

        try
        {
            var preEvent = new PreReasoningEvent
            {
                AgentName = Name,
                CurrentMessage = userMessage,
                Context = string.Join("\n", thoughtHistory)
            };
            await _hookManager.ExecutePreReasoningHooksAsync(preEvent).ConfigureAwait(false);

            if (preEvent.ShouldStop)
            {
                const string stopMessage = "Reasoning stopped by hook";
                await EmitErrorHookAsync(userMessage, stopMessage).ConfigureAwait(false);
                events.Add(AgentEvent.ErrorEvent(CreateErrorResponse(stopMessage), stopMessage, isLast: true));
                return (ReasoningResult.Error(stopMessage), events, true);
            }

            var prompt = BuildReasoningPrompt(userMessage, thoughtHistory, iteration);
            var requestMessages = new List<Msg> { prompt };

            if (options.IncludeReasoning)
            {
                events.Add(new AgentEvent(
                    AgentEventType.ReasoningStart,
                    null,
                    false,
                    CreatePhaseMetadata(iteration, "reasoning")));
            }

            var rawResponseBuilder = new StringBuilder();
            ModelResponse? modelResponse = null;

            if (_model is IStreamingChatModel streamingModel)
            {
                await foreach (var chunk in streamingModel.GenerateStreamAsync(requestMessages, options.CancellationToken).ConfigureAwait(false))
                {
                    if (!chunk.Success)
                    {
                        var error = chunk.Error ?? "Model error";
                        await EmitErrorHookAsync(userMessage, error).ConfigureAwait(false);
                        events.Add(AgentEvent.ErrorEvent(CreateErrorResponse(error), error, isLast: true));
                        return (ReasoningResult.Error(error), events, true);
                    }

                    modelResponse = chunk;
                    var chunkText = ExtractChunkText(chunk);
                    if (string.IsNullOrEmpty(chunkText))
                    {
                        continue;
                    }

                    rawResponseBuilder.Append(chunkText);
                    await EmitReasoningChunkHookAsync(userMessage, chunkText).ConfigureAwait(false);

                    if (options.IncludeReasoning)
                    {
                        events.Add(new AgentEvent(
                            AgentEventType.ReasoningChunk,
                            BuildAssistantChunkMessage(chunkText),
                            false,
                            CreatePhaseMetadata(iteration, "reasoning_chunk")));
                    }
                }
            }
            else
            {
                var response = await _model.GenerateAsync(new ModelRequest { Messages = requestMessages }).ConfigureAwait(false);
                if (!response.Success)
                {
                    var error = response.Error ?? "Model error";
                    await EmitErrorHookAsync(userMessage, error).ConfigureAwait(false);
                    events.Add(AgentEvent.ErrorEvent(CreateErrorResponse(error), error, isLast: true));
                    return (ReasoningResult.Error(error), events, true);
                }

                modelResponse = response;
                var rawText = response.Text ?? string.Empty;
                rawResponseBuilder.Append(rawText);

                if (!string.IsNullOrEmpty(rawText))
                {
                    await EmitReasoningChunkHookAsync(userMessage, rawText).ConfigureAwait(false);

                    if (options.IncludeReasoning)
                    {
                        events.Add(new AgentEvent(
                            AgentEventType.ReasoningChunk,
                            BuildAssistantChunkMessage(rawText),
                            false,
                            CreatePhaseMetadata(iteration, "reasoning_chunk")));
                    }
                }
            }

            var rawResponse = rawResponseBuilder.ToString();
            var thought = ParseThought(rawResponse);

            var postEvent = new PostReasoningEvent
            {
                AgentName = Name,
                CurrentMessage = userMessage,
                ReasoningResult = thought
            };
            await _hookManager.ExecutePostReasoningHooksAsync(postEvent).ConfigureAwait(false);

            if (options.IncludeReasoning)
            {
                events.Add(new AgentEvent(
                    AgentEventType.ReasoningFinish,
                    string.IsNullOrEmpty(rawResponse) ? null : BuildAssistantChunkMessage(rawResponse),
                    false,
                    CreatePhaseMetadata(iteration, "reasoning_finish")));
            }

            if (_verbose)
            {
                Console.WriteLine($"Thought: {thought}");
            }

            return (ReasoningResult.Success(thought, modelResponse, rawResponse), events, false);
        }
        catch (System.Exception ex)
        {
            var error = $"Reasoning error: {ex.Message}";
            await EmitErrorHookAsync(userMessage, error, ex).ConfigureAwait(false);
            events.Add(AgentEvent.ErrorEvent(CreateErrorResponse(error), error, isLast: true));
            return (ReasoningResult.Error(error), events, true);
        }
    }

    private async Task<(ActionResult Result, List<AgentEvent> Events, string? Observation, bool ShouldStop)> ExecuteStreamingActionPhaseAsync(
        Msg userMessage,
        ReasoningResult reasoning,
        List<string> thoughtHistory,
        int iteration,
        StreamOptions options)
    {
        var events = new List<AgentEvent>();

        try
        {
            var responseText = reasoning.RawResponseText ?? reasoning.Thought ?? string.Empty;
            var actionIntent = ParseActionIntent(responseText);

            var preActingEvent = new PreActingEvent
            {
                AgentName = Name,
                Action = actionIntent.Action,
                ActionParameters = actionIntent.Parameters
            };
            await _hookManager.ExecutePreActingHooksAsync(preActingEvent).ConfigureAwait(false);

            if (preActingEvent.ShouldStop)
            {
                const string stopMessage = "Action stopped by hook";
                await EmitErrorHookAsync(userMessage, stopMessage).ConfigureAwait(false);
                events.Add(AgentEvent.ErrorEvent(CreateErrorResponse(stopMessage), stopMessage, isLast: true));
                return (ActionResult.Error(stopMessage), events, null, true);
            }

            if (actionIntent.Action == "finish")
            {
                var finalAnswer = actionIntent.Parameters?.ToString();
                if (string.IsNullOrWhiteSpace(finalAnswer) || IsCompletionMarker(finalAnswer))
                {
                    finalAnswer = ExtractFinalAnswerForFinish(responseText, actionIntent.HasActionLine);
                }

                var actionResult = ActionResult.Finish(finalAnswer ?? string.Empty);
                var postActingEvent = new PostActingEvent
                {
                    AgentName = Name,
                    Action = actionIntent.Action,
                    ActionResult = actionResult,
                    ActionSuccess = true
                };
                await _hookManager.ExecutePostActingHooksAsync(postActingEvent).ConfigureAwait(false);

                events.Add(new AgentEvent(
                    AgentEventType.ActingFinish,
                    null,
                    false,
                    CreatePhaseMetadata(iteration, "acting_finish")));

                events.AddRange(await ExecuteStreamingSummaryPhaseAsync(
                    BuildAssistantChunkMessage(finalAnswer ?? string.Empty),
                    finalAnswer ?? string.Empty,
                    iteration,
                    thoughtHistory).ConfigureAwait(false));

                return (actionResult, events, null, true);
            }

            if (GetAvailableTools().TryGetValue(actionIntent.Action, out var tool))
            {
                var parameters = actionIntent.Parameters as Dictionary<string, object>
                    ?? new Dictionary<string, object>();

                if (options.IncludeToolCalls)
                {
                    events.Add(new AgentEvent(
                        AgentEventType.ToolCallStart,
                        BuildAssistantChunkMessage(actionIntent.Action),
                        false,
                        CreatePhaseMetadata(iteration, "tool_call_start")));
                }

                var toolResult = await tool.ExecuteAsync(parameters).ConfigureAwait(false);
                var toolOutput = toolResult.Result?.ToString() ?? toolResult.Error ?? string.Empty;

                if (!string.IsNullOrEmpty(toolOutput))
                {
                    await EmitActingChunkHookAsync(userMessage, toolOutput).ConfigureAwait(false);
                }

                var actionResult = ActionResult.ToolCall(actionIntent.Action, toolResult.Success, toolOutput);
                var postActingEvent = new PostActingEvent
                {
                    AgentName = Name,
                    Action = actionIntent.Action,
                    ActionResult = actionResult,
                    ActionSuccess = !actionResult.IsError
                };
                await _hookManager.ExecutePostActingHooksAsync(postActingEvent).ConfigureAwait(false);

                if (options.IncludeToolCalls && !string.IsNullOrEmpty(toolOutput))
                {
                    events.Add(new AgentEvent(
                        AgentEventType.ToolCallChunk,
                        BuildAssistantChunkMessage(toolOutput),
                        false,
                        CreatePhaseMetadata(iteration, "tool_call_chunk")));
                }

                if (options.IncludeToolCalls)
                {
                    events.Add(new AgentEvent(
                        AgentEventType.ToolCallFinish,
                        string.IsNullOrEmpty(toolOutput) ? null : BuildAssistantChunkMessage(toolOutput),
                        false,
                        CreatePhaseMetadata(iteration, "tool_call_finish")));
                }

                var observation = await ObservationPhaseAsync(actionResult).ConfigureAwait(false);
                return (actionResult, events, observation, false);
            }

            var unknownActionError = $"Unknown action: {actionIntent.Action}";
            await EmitErrorHookAsync(userMessage, unknownActionError).ConfigureAwait(false);
            events.Add(AgentEvent.ErrorEvent(CreateErrorResponse(unknownActionError), unknownActionError, isLast: true));
            return (ActionResult.Error(unknownActionError), events, null, true);
        }
        catch (System.Exception ex)
        {
            var error = $"Acting error: {ex.Message}";
            await EmitErrorHookAsync(userMessage, error, ex).ConfigureAwait(false);
            events.Add(AgentEvent.ErrorEvent(CreateErrorResponse(error), error, isLast: true));
            return (ActionResult.Error(error), events, null, true);
        }
    }

    /// <summary>
    /// 推理阶段：Agent 思考下一步该做什么
    /// Reasoning phase: Agent thinks about what to do next
    /// </summary>
    private async Task<ReasoningResult> ReasoningPhaseAsync(
        Msg userMessage, 
        List<string> thoughtHistory, 
        int iteration)
    {
        try
        {
            // 触发 Pre-Reasoning Hook
            var preEvent = new PreReasoningEvent
            {
                AgentName = Name,
                CurrentMessage = userMessage,
                Context = string.Join("\n", thoughtHistory)
            };
            await _hookManager.ExecutePreReasoningHooksAsync(preEvent);

            if (preEvent.ShouldStop)
            {
                const string stopMessage = "Reasoning stopped by hook";
                await EmitErrorHookAsync(userMessage, stopMessage).ConfigureAwait(false);
                return ReasoningResult.Error(stopMessage);
            }

            // 构建推理提示词
            var prompt = BuildReasoningPrompt(userMessage, thoughtHistory, iteration);
            
            var messages = new List<Msg> {prompt};
            var request = new ModelRequest { Messages = messages };
            
            var response = await _model.GenerateAsync(request);
            
            if (!response.Success)
            {
                var error = response.Error ?? "Model error";
                await EmitErrorHookAsync(userMessage, error).ConfigureAwait(false);
                return ReasoningResult.Error(error);
            }

            var rawResponse = response.Text ?? "";
            var thought = ParseThought(rawResponse);

            if (!string.IsNullOrEmpty(rawResponse))
            {
                await EmitReasoningChunkHookAsync(userMessage, rawResponse).ConfigureAwait(false);
            }

            // 触发 Post-Reasoning Hook
            var postEvent = new PostReasoningEvent
            {
                AgentName = Name,
                CurrentMessage = userMessage,
                ReasoningResult = thought
            };
            await _hookManager.ExecutePostReasoningHooksAsync(postEvent);

            if (_verbose)
            {
                Console.WriteLine($"Thought: {thought}");
            }

            return ReasoningResult.Success(thought, response, rawResponse);
        }
        catch (System.Exception ex)
        {
            var error = $"Reasoning error: {ex.Message}";
            await EmitErrorHookAsync(userMessage, error, ex).ConfigureAwait(false);
            return ReasoningResult.Error(error);
        }
    }

    /// <summary>
    /// 行动阶段：根据推理结果执行行动
    /// Acting phase: Execute action based on reasoning
    /// </summary>
    private async Task<ActionResult> ActingPhaseAsync(ReasoningResult reasoning)
    {
        try
        {
            // 解析行动意图
            var responseText = reasoning.RawResponseText ?? reasoning.Thought ?? "";
            var actionIntent = ParseActionIntent(responseText);

            // 触发 Pre-Acting Hook
            var preEvent = new PreActingEvent
            {
                AgentName = Name,
                Action = actionIntent.Action,
                ActionParameters = actionIntent.Parameters
            };
            await _hookManager.ExecutePreActingHooksAsync(preEvent);

            if (preEvent.ShouldStop)
            {
                const string stopMessage = "Action stopped by hook";
                await EmitErrorHookAsync(reasoning.ModelResponse == null ? null : BuildAssistantChunkMessage(responseText), stopMessage).ConfigureAwait(false);
                return ActionResult.Error(stopMessage);
            }

            ActionResult result;

            if (actionIntent.Action == "finish")
            {
                // 如果 Parameters 为空，尝试从响应文本中提取最终答复
                var finalAnswer = actionIntent.Parameters?.ToString();
                if (string.IsNullOrWhiteSpace(finalAnswer) || IsCompletionMarker(finalAnswer))
                {
                    finalAnswer = ExtractFinalAnswerForFinish(responseText, actionIntent.HasActionLine);
                }
                result = ActionResult.Finish(finalAnswer ?? string.Empty);
            }
            else if (GetAvailableTools().TryGetValue(actionIntent.Action, out var tool))
            {
                // Permission 检查
                if (_permission != null)
                {
                    var decision = _permission.Evaluate(new ToolCallRequest
                    {
                        ToolName = actionIntent.Action,
                        Arguments = actionIntent.Parameters as Dictionary<string, object>
                    });
                    if (decision.Behavior == PermissionBehavior.Deny)
                    {
                        return ActionResult.ToolCall(actionIntent.Action, false, $"权限拒绝: {decision.Reason}");
                    }
                    if (decision.Behavior == PermissionBehavior.Ask)
                    {
                        // HITL：真正阻塞等待用户决策，未获批准则不执行工具。
                        var confirmArgs = actionIntent.Parameters as Dictionary<string, object>;
                        var confirm = await RequestUserConfirmAsync(
                            actionIntent.Action, confirmArgs, decision.Reason).ConfigureAwait(false);

                        if (!confirm.Approved)
                        {
                            var denyReason = confirm.Reason ?? decision.Reason ?? "用户拒绝执行";
                            return ActionResult.ToolCall(
                                actionIntent.Action, false, $"用户拒绝: {denyReason}");
                        }
                    }
                }

                // 执行工具
                var parameters = actionIntent.Parameters as Dictionary<string, object> 
                    ?? new Dictionary<string, object>();
                var toolResult = await tool.ExecuteAsync(parameters);
                var toolOutput = toolResult.Result?.ToString() ?? toolResult.Error ?? "";
                if (!string.IsNullOrEmpty(toolOutput))
                {
                    await EmitActingChunkHookAsync(reasoning.ModelResponse == null ? null : BuildAssistantChunkMessage(responseText), toolOutput).ConfigureAwait(false);
                }
                
                result = ActionResult.ToolCall(
                    actionIntent.Action, 
                    toolResult.Success, 
                    toolOutput);
            }
            else
            {
                var error = $"Unknown action: {actionIntent.Action}";
                await EmitErrorHookAsync(reasoning.ModelResponse == null ? null : BuildAssistantChunkMessage(responseText), error).ConfigureAwait(false);
                result = ActionResult.Error(error);
            }

            // 触发 Post-Acting Hook
            var postEvent = new PostActingEvent
            {
                AgentName = Name,
                Action = actionIntent.Action,
                ActionResult = result,
                ActionSuccess = !result.IsError
            };
            await _hookManager.ExecutePostActingHooksAsync(postEvent);

            if (_verbose)
            {
                Console.WriteLine($"Action: {actionIntent.Action}");
                if (result.IsToolCall)
                {
                    Console.WriteLine($"Result: {result.ToolResult}");
                }
            }

            return result;
        }
        catch (System.Exception ex)
        {
            var error = $"Acting error: {ex.Message}";
            await EmitErrorHookAsync(reasoning.ModelResponse == null ? null : BuildAssistantChunkMessage(reasoning.RawResponseText ?? reasoning.Thought ?? string.Empty), error, ex).ConfigureAwait(false);
            return ActionResult.Error(error);
        }
    }

    /// <summary>
    /// 观察阶段：处理工具执行结果
    /// Observation phase: Process tool execution result
    /// </summary>
    private Task<string> ObservationPhaseAsync(ActionResult action)
    {
        var observation = action.IsToolCall && action.ToolSuccess
            ? $"Tool '{action.ToolName}' succeeded: {action.ToolResult}"
            : $"Tool '{action.ToolName}' failed: {action.ToolResult}";

        if (_verbose)
        {
            Console.WriteLine($"Observation: {observation}");
        }

        return Task.FromResult(observation);
    }

    private Msg BuildReasoningPrompt(Msg userMessage, List<string> thoughtHistory, int iteration)
    {
        var toolDescriptions = BuildAvailableToolDescriptions();
        var promptText = $@"{_systemPrompt}

用户问题: {userMessage.GetTextContent()}

你可以使用以下工具:
    {toolDescriptions}

之前的思考:
{string.Join("\n", thoughtHistory)}

当前迭代: {iteration}/{_maxIterations}

请以以下格式回答:
Thought: [你的思考过程]
Action: [finish 或 工具名称]
Action Input: [如果是finish，输出最终答案；如果是工具，输出JSON格式的参数]";

        return Msg.Builder()
            .Role("system")
            .TextContent(promptText)
            .Build();
    }

    private string ParseThought(string response)
    {
        var lines = response.Split('\n');
        var thoughtLine = lines.FirstOrDefault(l => l.StartsWith("Thought:", StringComparison.OrdinalIgnoreCase));
        return thoughtLine?.Substring("Thought:".Length).Trim() ?? response;
    }

    /// <summary>
    /// 从 Thought 中提取最终答案
    /// 当 Action Input 为空时，尝试从 Thought 内容中提取实际回复
    /// </summary>
    private string ExtractFinalAnswerFromThought(string? thought)
    {
        if (string.IsNullOrWhiteSpace(thought))
        {
            return string.Empty;
        }

        // 移除 "Thought X:" 前缀
        var lines = thought.Split('\n');
        var contentLines = new List<string>();
        
        foreach (var line in lines)
        {
            var trimmedLine = line.Trim();
            // 跳过 Action 和 Action Input 行
            if (trimmedLine.StartsWith("Action:", StringComparison.OrdinalIgnoreCase) ||
                trimmedLine.StartsWith("Action Input:", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }
            // 移除 Thought 前缀
            if (trimmedLine.StartsWith("Thought", StringComparison.OrdinalIgnoreCase))
            {
                var colonIndex = trimmedLine.IndexOf(':');
                if (colonIndex > 0)
                {
                    trimmedLine = trimmedLine.Substring(colonIndex + 1).Trim();
                }
            }
            if (!string.IsNullOrWhiteSpace(trimmedLine))
            {
                contentLines.Add(trimmedLine);
            }
        }

        return string.Join(" ", contentLines);
    }

    private ActionIntent ParseActionIntent(string thought)
    {
        try
        {
            var lines = thought.Split('\n');
            var actionLine = lines.FirstOrDefault(l => l.StartsWith("Action:", StringComparison.OrdinalIgnoreCase));
            var inputLine = lines.FirstOrDefault(l => l.StartsWith("Action Input:", StringComparison.OrdinalIgnoreCase));

            // 如果没有找到 Action 行，说明模型没有按照 ReAct 格式回复
            // 此时将整个 thought 作为最终答案
            if (actionLine == null)
            {
                return new ActionIntent { Action = "finish", Parameters = null, HasActionLine = false };
            }

            var action = actionLine.Substring("Action:".Length).Trim().ToLower();
            var input = inputLine?.Substring("Action Input:".Length).Trim() ?? "";

            object? parameters = null;
            if (!string.IsNullOrEmpty(input))
            {
                try
                {
                    parameters = JsonSerializer.Deserialize<Dictionary<string, object>>(input);
                }
                catch
                {
                    parameters = input;
                }
            }

            return new ActionIntent { Action = action, Parameters = parameters, HasActionLine = true };
        }
        catch
        {
            return new ActionIntent { Action = "finish", Parameters = null, HasActionLine = false };
        }
    }

    private static bool IsCompletionMarker(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var normalized = value.Trim();
        return normalized.Equals("Done", StringComparison.OrdinalIgnoreCase)
            || normalized.Equals("完成", StringComparison.OrdinalIgnoreCase);
    }

    private string? ExtractFinalAnswerForFinish(string responseText, bool hasActionLine)
    {
        if (string.IsNullOrWhiteSpace(responseText))
        {
            return null;
        }

        if (!hasActionLine)
        {
            return null;
        }

        var lines = responseText.Split('\n');
        var actionInputLine = lines.FirstOrDefault(l =>
            l.StartsWith("Action Input:", StringComparison.OrdinalIgnoreCase));

        if (actionInputLine == null)
        {
            return null;
        }

        var value = actionInputLine.Substring("Action Input:".Length).Trim();
        if (string.IsNullOrWhiteSpace(value) || IsCompletionMarker(value))
        {
            return null;
        }

        return value;
    }

    private Msg CreateErrorResponse(string error)
    {
        return Msg.Builder()
            .Name(Name)
            .Role("assistant")
            .TextContent($"错误 Error: {error}")
            .Build();
    }

    private Msg CreateFinalResponse(string finalResponse, int iteration, List<string> thoughtHistory)
    {
        return Msg.Builder()
            .Name(Name)
            .Role("assistant")
            .TextContent(finalResponse)
            .AddMetadata("iterations", iteration)
            .AddMetadata("thoughts", string.Join("\n", thoughtHistory))
            .Build();
    }

    private Msg BuildAssistantChunkMessage(string text)
    {
        return Msg.Builder()
            .Name(Name)
            .Role("assistant")
            .TextContent(text)
            .Build();
    }

    private static Dictionary<string, object> CreatePhaseMetadata(int iteration, string phase)
    {
        return new Dictionary<string, object>
        {
            ["iteration"] = iteration,
            ["phase"] = phase
        };
    }

    private static string ExtractChunkText(ChatResponse chunk)
    {
        return chunk.Text ?? chunk.Content ?? string.Empty;
    }

    private IReadOnlyDictionary<string, ITool> GetAvailableTools()
    {
        return _toolGroupManager?.FilterActiveTools(_tools) ?? _tools;
    }

    private string BuildAvailableToolDescriptions()
    {
        var availableTools = GetAvailableTools();
        return availableTools.Count == 0
            ? "(当前没有激活的可用工具)"
            : string.Join("\n", availableTools.Values.Select(t => $"- {t.Name}: {t.Description}"));
    }

    private async Task<string> ExecuteSummaryPhaseAsync(Msg currentMessage, string summaryText)
    {
        try
        {
            var preEvent = new PreSummaryEvent
            {
                AgentName = Name,
                CurrentMessage = currentMessage,
                SummaryText = summaryText
            };
            await _hookManager.ExecutePreSummaryHooksAsync(preEvent).ConfigureAwait(false);

            var effectiveSummary = preEvent.SummaryText ?? string.Empty;
            if (!string.IsNullOrEmpty(effectiveSummary))
            {
                await EmitSummaryChunkHookAsync(BuildAssistantChunkMessage(effectiveSummary), effectiveSummary).ConfigureAwait(false);
            }

            var postEvent = new PostSummaryEvent
            {
                AgentName = Name,
                CurrentMessage = currentMessage,
                SummaryText = effectiveSummary
            };
            await _hookManager.ExecutePostSummaryHooksAsync(postEvent).ConfigureAwait(false);

            return effectiveSummary;
        }
        catch (System.Exception ex)
        {
            await EmitErrorHookAsync(currentMessage, $"Summary error: {ex.Message}", ex).ConfigureAwait(false);
            return summaryText;
        }
    }

    private async Task<List<AgentEvent>> ExecuteStreamingSummaryPhaseAsync(
        Msg currentMessage,
        string summaryText,
        int iteration,
        List<string> thoughtHistory)
    {
        var events = new List<AgentEvent>
        {
            new(
                AgentEventType.SummaryStart,
                null,
                false,
                CreatePhaseMetadata(iteration, "summary_start"))
        };

        var effectiveSummary = summaryText;

        try
        {
            var preEvent = new PreSummaryEvent
            {
                AgentName = Name,
                CurrentMessage = currentMessage,
                SummaryText = summaryText
            };
            await _hookManager.ExecutePreSummaryHooksAsync(preEvent).ConfigureAwait(false);
            effectiveSummary = preEvent.SummaryText ?? string.Empty;

            if (!string.IsNullOrEmpty(effectiveSummary))
            {
                await EmitSummaryChunkHookAsync(BuildAssistantChunkMessage(effectiveSummary), effectiveSummary).ConfigureAwait(false);
                events.Add(new AgentEvent(
                    AgentEventType.SummaryChunk,
                    BuildAssistantChunkMessage(effectiveSummary),
                    false,
                    CreatePhaseMetadata(iteration, "summary_chunk")));
            }

            var postEvent = new PostSummaryEvent
            {
                AgentName = Name,
                CurrentMessage = currentMessage,
                SummaryText = effectiveSummary
            };
            await _hookManager.ExecutePostSummaryHooksAsync(postEvent).ConfigureAwait(false);
        }
        catch (System.Exception ex)
        {
            var error = $"Summary error: {ex.Message}";
            await EmitErrorHookAsync(currentMessage, error, ex).ConfigureAwait(false);
            events.Add(AgentEvent.ErrorEvent(CreateErrorResponse(error), error, isLast: true));
            return events;
        }

        events.Add(new AgentEvent(
            AgentEventType.SummaryFinish,
            CreateFinalResponse(effectiveSummary, iteration, thoughtHistory),
            true,
            CreatePhaseMetadata(iteration, "summary_finish")));

        return events;
    }

    private async Task EmitReasoningChunkHookAsync(Msg userMessage, string chunk)
    {
        var hookEvent = new ReasoningChunkEvent
        {
            AgentName = Name,
            CurrentMessage = userMessage,
            Chunk = chunk
        };
        await _hookManager.ExecuteReasoningChunkHooksAsync(hookEvent).ConfigureAwait(false);
    }

    private async Task EmitActingChunkHookAsync(Msg? currentMessage, string chunk)
    {
        var hookEvent = new ActingChunkEvent
        {
            AgentName = Name,
            CurrentMessage = currentMessage,
            Chunk = chunk
        };
        await _hookManager.ExecuteActingChunkHooksAsync(hookEvent).ConfigureAwait(false);
    }

    private async Task EmitSummaryChunkHookAsync(Msg currentMessage, string chunk)
    {
        var hookEvent = new SummaryChunkEvent
        {
            AgentName = Name,
            CurrentMessage = currentMessage,
            Chunk = chunk
        };
        await _hookManager.ExecuteSummaryChunkHooksAsync(hookEvent).ConfigureAwait(false);
    }

    private async Task EmitErrorHookAsync(Msg? currentMessage, string errorMessage, System.Exception? exception = null)
    {
        var hookEvent = new ErrorHookEvent
        {
            AgentName = Name,
            CurrentMessage = currentMessage,
            ErrorMessage = errorMessage,
            Exception = exception
        };
        await _hookManager.ExecuteErrorHooksAsync(hookEvent).ConfigureAwait(false);
    }

    public void SaveTo(AgentScope.Core.Session.Session session, string sessionKey)
    {
        if (session == null) throw new ArgumentNullException(nameof(session));
        if (string.IsNullOrWhiteSpace(sessionKey)) throw new ArgumentNullException(nameof(sessionKey));

        session.AgentName = Name;
        session.SetContext(AgentMetaStateKeyPrefix + sessionKey, new AgentMetaState(session.Id, Name, string.Empty, _systemPrompt));

        if (_statePersistence.MemoryManaged)
            session.SetContext(MemoryStateKeyPrefix + sessionKey, _memory.GetAll());

        if (_statePersistence.ToolkitManaged && _toolGroupManager != null)
        {
            var activeGroups = _toolGroupManager.GetActiveGroupNames().ToHashSet(StringComparer.OrdinalIgnoreCase);
            session.SetContext(ToolkitStateKeyPrefix + sessionKey, new ToolkitState(activeGroups));
        }
    }

    public void LoadFrom(AgentScope.Core.Session.Session session, string sessionKey)
    {
        if (session == null) throw new ArgumentNullException(nameof(session));
        if (string.IsNullOrWhiteSpace(sessionKey)) throw new ArgumentNullException(nameof(sessionKey));

        var meta = session.GetContext<AgentMetaState>(AgentMetaStateKeyPrefix + sessionKey);
        if (meta == null)
            throw new InvalidOperationException("State not found: " + sessionKey);

        ApplyAgentMeta(meta);

        if (_statePersistence.MemoryManaged)
        {
            var messages = session.GetContext<List<Msg>>(MemoryStateKeyPrefix + sessionKey);
            if (messages != null)
                RestoreMemory(messages);
        }

        if (_statePersistence.ToolkitManaged && _toolGroupManager != null)
        {
            var toolkitState = session.GetContext<ToolkitState>(ToolkitStateKeyPrefix + sessionKey);
            if (toolkitState != null)
                _toolGroupManager.SetActiveGroups(toolkitState.ActiveGroups);
        }
    }

    public void LoadIfExists(AgentScope.Core.Session.Session session, string sessionKey)
    {
        if (session == null) throw new ArgumentNullException(nameof(session));
        if (string.IsNullOrWhiteSpace(sessionKey)) throw new ArgumentNullException(nameof(sessionKey));

        var meta = session.GetContext<AgentMetaState>(AgentMetaStateKeyPrefix + sessionKey);
        if (meta == null)
            return;

        ApplyAgentMeta(meta);

        if (_statePersistence.MemoryManaged)
        {
            var messages = session.GetContext<List<Msg>>(MemoryStateKeyPrefix + sessionKey);
            if (messages != null)
                RestoreMemory(messages);
        }

        if (_statePersistence.ToolkitManaged && _toolGroupManager != null)
        {
            var toolkitState = session.GetContext<ToolkitState>(ToolkitStateKeyPrefix + sessionKey);
            if (toolkitState != null)
                _toolGroupManager.SetActiveGroups(toolkitState.ActiveGroups);
        }
    }

    private void ApplyAgentMeta(AgentMetaState meta)
    {
        Name = meta.Name;
        _systemPrompt = meta.SystemPrompt;
    }

    private void RestoreMemory(IEnumerable<Msg> messages)
    {
        _memory.Clear();
        foreach (var message in messages)
            _memory.Add(message);
    }

    public static EnhancedReActAgentBuilder Builder()
    {
        return new EnhancedReActAgentBuilder();
    }
}

// 内部辅助类
internal class ReasoningResult
{
    public bool IsError { get; set; }
    public string? Thought { get; set; }
    public string? ErrorMessage { get; set; }
    public ModelResponse? ModelResponse { get; set; }
    public string? RawResponseText { get; set; }

    public static ReasoningResult Success(string thought, ModelResponse? modelResponse = null, string? rawResponseText = null) => 
        new() { Thought = thought, ModelResponse = modelResponse, RawResponseText = rawResponseText };

    public static ReasoningResult Error(string error) => 
        new() { IsError = true, ErrorMessage = error };
}

internal class ActionResult
{
    public bool IsFinish { get; set; }
    public bool IsToolCall { get; set; }
    public bool IsError { get; set; }
    public string? FinalAnswer { get; set; }
    public string? ToolName { get; set; }
    public bool ToolSuccess { get; set; }
    public string? ToolResult { get; set; }
    public string? ErrorMessage { get; set; }

    public static ActionResult Finish(string answer) => 
        new() { IsFinish = true, FinalAnswer = answer };

    public static ActionResult ToolCall(string toolName, bool success, string result) => 
        new() { IsToolCall = true, ToolName = toolName, ToolSuccess = success, ToolResult = result };

    public static ActionResult Error(string error) => 
        new() { IsError = true, ErrorMessage = error };
}

internal class ActionIntent
{
    public string Action { get; set; } = "";
    public object? Parameters { get; set; }
    public bool HasActionLine { get; set; }
}

/// <summary>
/// 增强版 ReActAgent 构建器
/// Builder for EnhancedReActAgent
/// </summary>
public class EnhancedReActAgentBuilder
{
    private string _name = "EnhancedReActAgent";
    private IModel? _model;
    private string _sysPrompt = "你是一个有帮助的AI助手。You are a helpful AI assistant.";
    private IMemory? _memory;
    private readonly Dictionary<string, ITool> _tools = new();
    private ToolGroupManager? _toolGroupManager;
    private StatePersistence _statePersistence = AgentScope.Core.State.StatePersistence.All;
    private int _maxIterations = 10;
    private HookManager? _hookManager;
    private IPermissionEngine? _permission;
    private bool _verbose = false;
    private Func<RequireUserConfirmEvent, Task<ConfirmResult>>? _confirmCallback;
    private bool _autoApproveOnAsk;

    public EnhancedReActAgentBuilder Name(string name)
    {
        _name = name;
        return this;
    }

    public EnhancedReActAgentBuilder Model(IModel model)
    {
        _model = model;
        return this;
    }

    public EnhancedReActAgentBuilder SysPrompt(string prompt)
    {
        _sysPrompt = prompt;
        return this;
    }

    public EnhancedReActAgentBuilder Memory(IMemory memory)
    {
        _memory = memory;
        return this;
    }

    public EnhancedReActAgentBuilder AddTool(ITool tool)
    {
        _tools[tool.Name] = tool;
        return this;
    }

    public EnhancedReActAgentBuilder ToolGroupManager(ToolGroupManager manager)
    {
        _toolGroupManager = manager;
        return this;
    }

    public EnhancedReActAgentBuilder StatePersistence(StatePersistence statePersistence)
    {
        _statePersistence = statePersistence ?? AgentScope.Core.State.StatePersistence.All;
        return this;
    }

    public EnhancedReActAgentBuilder AddToolGroup(ToolGroup group)
    {
        _toolGroupManager ??= new ToolGroupManager();
        _toolGroupManager.RegisterGroup(group);
        return this;
    }

    public EnhancedReActAgentBuilder MaxIterations(int max)
    {
        _maxIterations = max;
        return this;
    }

    public EnhancedReActAgentBuilder HookManager(HookManager manager)
    {
        _hookManager = manager;
        return this;
    }

    public EnhancedReActAgentBuilder PermissionEngine(IPermissionEngine permission)
    {
        _permission = permission;
        return this;
    }

    public EnhancedReActAgentBuilder Verbose(bool verbose = true)
    {
        _verbose = verbose;
        return this;
    }

    /// <summary>
    /// 配置 HITL 确认回调。权限判定为 Ask 时调用，返回批准/拒绝。
    /// 不配置则回退到内置控制台交互。
    /// </summary>
    public EnhancedReActAgentBuilder ConfirmCallback(
        Func<RequireUserConfirmEvent, Task<ConfirmResult>> callback)
    {
        _confirmCallback = callback;
        return this;
    }

    /// <summary>无交互终端且未配置确认回调时是否自动放行（默认 false）。</summary>
    public EnhancedReActAgentBuilder AutoApproveOnAsk(bool autoApprove = true)
    {
        _autoApproveOnAsk = autoApprove;
        return this;
    }

    public EnhancedReActAgent Build()
    {
        if (_model == null)
        {
            throw new InvalidOperationException("Model is required");
        }

        var agent = new EnhancedReActAgent(
            _name, _model, _sysPrompt, _memory, _tools, _toolGroupManager, _statePersistence,
            _maxIterations, _hookManager, _permission, _verbose);

        if (_confirmCallback != null) agent.ConfirmCallback = _confirmCallback;
        agent.AutoApproveOnAsk = _autoApproveOnAsk;

        return agent;
    }
}
