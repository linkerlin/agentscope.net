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
using System.Text.Json;
using System.Threading.Tasks;
using AgentScope.Core.Agent;
using AgentScope.Core.Hook;
using AgentScope.Core.Memory;
using AgentScope.Core.Message;
using AgentScope.Core.Model;
using AgentScope.Core.Tool;

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
public class EnhancedReActAgent : AgentBase
{
    private readonly IModel _model;
    private readonly IMemory _memory;
    private readonly Dictionary<string, ITool> _tools;
    private readonly string _systemPrompt;
    private readonly int _maxIterations;
    private readonly HookManager _hookManager;
    private readonly bool _verbose;

    internal EnhancedReActAgent(
        string name, 
        IModel model, 
        string systemPrompt, 
        IMemory? memory = null, 
        Dictionary<string, ITool>? tools = null, 
        int maxIterations = 10,
        HookManager? hookManager = null,
        bool verbose = false)
        : base(name)
    {
        _model = model;
        _systemPrompt = systemPrompt;
        _memory = memory ?? new MemoryBase();
        _tools = tools ?? new Dictionary<string, ITool>();
        _maxIterations = maxIterations;
        _hookManager = hookManager ?? new HookManager();
        _verbose = verbose;
    }

    public override IObservable<Msg> Call(Msg message)
    {
        return Observable.FromAsync(async () =>
        {
            _memory.Add(message);
            var response = await ProcessWithReActLoopAsync(message);
            _memory.Add(response);
            return response;
        });
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

        return Msg.Builder()
            .Name(Name)
            .Role("assistant")
            .TextContent(finalResponse)
            .AddMetadata("iterations", iteration)
            .AddMetadata("thoughts", string.Join("\n", thoughtHistory))
            .Build();
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
                return ReasoningResult.Error("Reasoning stopped by hook");
            }

            // 构建推理提示词
            var prompt = BuildReasoningPrompt(userMessage, thoughtHistory, iteration);
            
            var messages = new List<Msg> {prompt};
            var request = new ModelRequest { Messages = messages };
            
            var response = await _model.GenerateAsync(request);
            
            if (!response.Success)
            {
                return ReasoningResult.Error(response.Error ?? "Model error");
            }

            var thought = ParseThought(response.Text ?? "");

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

            return ReasoningResult.Success(thought);
        }
        catch (System.Exception ex)
        {
            return ReasoningResult.Error($"Reasoning error: {ex.Message}");
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
            var actionIntent = ParseActionIntent(reasoning.Thought!);

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
                return ActionResult.Error("Action stopped by hook");
            }

            ActionResult result;

            if (actionIntent.Action == "finish")
            {
                result = ActionResult.Finish(actionIntent.Parameters?.ToString() ?? "Done");
            }
            else if (_tools.ContainsKey(actionIntent.Action))
            {
                // 执行工具
                var tool = _tools[actionIntent.Action];
                var parameters = actionIntent.Parameters as Dictionary<string, object> 
                    ?? new Dictionary<string, object>();
                var toolResult = await tool.ExecuteAsync(parameters);
                
                result = ActionResult.ToolCall(
                    actionIntent.Action, 
                    toolResult.Success, 
                    toolResult.Result?.ToString() ?? toolResult.Error ?? "");
            }
            else
            {
                result = ActionResult.Error($"Unknown action: {actionIntent.Action}");
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
            return ActionResult.Error($"Acting error: {ex.Message}");
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
        var promptText = $@"{_systemPrompt}

用户问题: {userMessage.GetTextContent()}

你可以使用以下工具:
{string.Join("\n", _tools.Values.Select(t => $"- {t.Name}: {t.Description}"))}

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

    private ActionIntent ParseActionIntent(string thought)
    {
        try
        {
            var lines = thought.Split('\n');
            var actionLine = lines.FirstOrDefault(l => l.StartsWith("Action:", StringComparison.OrdinalIgnoreCase));
            var inputLine = lines.FirstOrDefault(l => l.StartsWith("Action Input:", StringComparison.OrdinalIgnoreCase));

            var action = actionLine?.Substring("Action:".Length).Trim().ToLower() ?? "finish";
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

            return new ActionIntent { Action = action, Parameters = parameters };
        }
        catch
        {
            return new ActionIntent { Action = "finish", Parameters = thought };
        }
    }

    private Msg CreateErrorResponse(string error)
    {
        return Msg.Builder()
            .Name(Name)
            .Role("assistant")
            .TextContent($"错误 Error: {error}")
            .Build();
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

    public static ReasoningResult Success(string thought) => 
        new() { Thought = thought };

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
    private int _maxIterations = 10;
    private HookManager? _hookManager;
    private bool _verbose = false;

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

    public EnhancedReActAgentBuilder Verbose(bool verbose = true)
    {
        _verbose = verbose;
        return this;
    }

    public EnhancedReActAgent Build()
    {
        if (_model == null)
        {
            throw new InvalidOperationException("Model is required");
        }

        return new EnhancedReActAgent(
            _name, _model, _sysPrompt, _memory, _tools, 
            _maxIterations, _hookManager, _verbose);
    }
}
