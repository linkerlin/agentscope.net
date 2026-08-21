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
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using AgentScope.Core.Agent;
using AgentScope.Core.Memory;
using AgentScope.Core.Message;
using AgentScope.Core.Model;
using AgentScope.Core.Tool;

namespace AgentScope.Core;

/// <summary>
/// ReAct (Reasoning and Acting) Agent implementation.
/// ReAct Agent 实现 - 结合推理和行动的迭代循环
/// 
/// 注意：建议升级到 EnhancedReActAgent，此类仅作向后兼容保留
/// </summary>
[Obsolete("请使用 EnhancedReActAgent 替代")]
public class ReActAgent : AgentBase
{
    /// <summary>
    /// JSON 序列化选项：不转义非 ASCII 字符，保证中文等原文可见。
    /// </summary>
    private static readonly JsonSerializerOptions NonEscapingJsonOptions = new()
    {
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    private readonly IModel _model;
    private readonly IMemory _memory;
    private readonly Dictionary<string, ITool> _tools;
    private readonly ToolGroupManager? _toolGroupManager;
    private readonly string _systemPrompt;
    private readonly int _maxIterations;

    internal ReActAgent(string name, IModel model, string systemPrompt, 
                        IMemory? memory = null, List<ITool>? tools = null,
                        ToolGroupManager? toolGroupManager = null,
                        int maxIterations = 10)
        : base(name, $"ReActAgent: {systemPrompt}")
    {
        _model = model ?? throw new ArgumentNullException(nameof(model));
        _systemPrompt = systemPrompt ?? "You are a helpful AI assistant.";
        _memory = memory ?? new MemoryBase();
        _tools = tools?.ToDictionary(t => t.Name) ?? new Dictionary<string, ITool>();
        _toolGroupManager = toolGroupManager;
        _maxIterations = maxIterations > 0 ? maxIterations : 10;
    }

    /// <summary>
    /// 实现 DoCallAsync（新 AgentBase 的抽象方法）
    /// </summary>
    protected override async Task<Msg> DoCallAsync(IReadOnlyList<Msg> messages)
    {
        var msg = messages.Count > 0 ? messages[messages.Count - 1]
            : Msg.Builder().Role("user").TextContent("").Build();

        _memory.Add(msg);
        Msg response;
        if (GetAvailableTools().Count > 0)
        {
            response = await ProcessWithReActLoopAsync(msg);
        }
        else
        {
            response = await ProcessSimpleAsync(msg);
        }
        _memory.Add(response);
        return response;
    }

    /// <summary>
    /// 简单模式：无工具时的直接调用
    /// </summary>
    private async Task<Msg> ProcessSimpleAsync(Msg userMessage)
    {
        var messages = BuildMessageHistory();
        var request = new ModelRequest { Messages = messages };
        var response = await _model.GenerateAsync(request);

        if (!response.Success)
        {
            return Msg.Builder()
                .Name(Name)
                .Role("assistant")
                .TextContent($"Error: {response.Error}")
                .Build();
        }

        return Msg.Builder()
            .Name(Name)
            .Role("assistant")
            .TextContent(response.Text ?? string.Empty)
            .Build();
    }

    /// <summary>
    /// ReAct 循环：推理-行动-观察
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

            // 阶段 1: Reasoning（推理）
            var reasoningResult = await ReasoningPhaseAsync(userMessage, thoughtHistory, iteration);
            
            if (reasoningResult.IsError)
            {
                return CreateErrorResponse(reasoningResult.ErrorMessage!);
            }

            thoughtHistory.Add($"Thought {iteration}: {reasoningResult.Thought}");

            // 阶段 2: Acting（行动）
            var actionResult = await ActingPhaseAsync(reasoningResult);
            
            if (actionResult.IsFinish)
            {
                finalResponse = actionResult.FinalAnswer!;
                continueLoop = false;
            }
            else if (actionResult.IsToolCall)
            {
                // 阶段 3: Observation（观察）
                var observation = $"Tool '{actionResult.ToolName}' result: {actionResult.ToolResult}";
                thoughtHistory.Add($"Observation {iteration}: {observation}");
            }
            else if (actionResult.IsError)
            {
                return CreateErrorResponse(actionResult.ErrorMessage!);
            }
        }

        if (iteration >= _maxIterations && string.IsNullOrEmpty(finalResponse))
        {
            finalResponse = "达到最大迭代次数但未得出结论。";
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
    /// </summary>
    private async Task<ReasoningResult> ReasoningPhaseAsync(
        Msg userMessage, 
        List<string> thoughtHistory, 
        int iteration)
    {
        try
        {
            var prompt = BuildReActPrompt(userMessage, thoughtHistory, iteration);
            var request = new ModelRequest { Messages = new List<Msg> { prompt } };
            var response = await _model.GenerateAsync(request);

            if (!response.Success)
            {
                return ReasoningResult.Error(response.Error ?? "模型错误");
            }

            var rawResponse = response.Text ?? "";
            var thought = ParseThought(rawResponse);
            return ReasoningResult.Success(thought, response, rawResponse);
        }
        catch (System.Exception ex)
        {
            return ReasoningResult.Error($"推理错误：{ex.Message}");
        }
    }

    /// <summary>
    /// 行动阶段：根据推理结果执行行动
    /// </summary>
    private async Task<ActionResult> ActingPhaseAsync(ReasoningResult reasoning)
    {
        try
        {
            var responseText = reasoning.RawResponseText ?? reasoning.Thought ?? "";
            var intent = ParseActionIntent(responseText);

            if (intent.Action == "finish")
            {
                var finalAnswer = intent.Parameters?.ToString();
                // Parameters 为 Dictionary（JSON 格式）时序列化为可读文本
                if (intent.Parameters is Dictionary<string, object> paramsDict)
                {
                    finalAnswer = JsonSerializer.Serialize(paramsDict, NonEscapingJsonOptions);
                }
                if (string.IsNullOrWhiteSpace(finalAnswer) || IsCompletionMarker(finalAnswer))
                {
                    finalAnswer = ExtractFinalAnswerForFinish(responseText, intent.HasActionLine);
                }
                // 仍为空时回退到原始响应文本，避免返回空答案
                if (string.IsNullOrWhiteSpace(finalAnswer))
                {
                    finalAnswer = responseText;
                }

                return ActionResult.Finish(finalAnswer ?? string.Empty);
            }
            else if (GetAvailableTools().TryGetValue(intent.Action, out var tool))
            {
                var parameters = intent.Parameters as Dictionary<string, object> 
                    ?? new Dictionary<string, object>();
                var toolResult = await tool.ExecuteAsync(parameters);
                
                return ActionResult.ToolCall(
                    intent.Action, 
                    toolResult.Success, 
                    toolResult.Result?.ToString() ?? toolResult.Error ?? "");
            }
            else
            {
                // 未知行动，当作完成处理
                return ActionResult.Finish(string.Empty);
            }
        }
        catch (System.Exception ex)
        {
            return ActionResult.Error($"行动错误：{ex.Message}");
        }
    }

    private Msg BuildReActPrompt(Msg userMessage, List<string> thoughtHistory, int iteration)
    {
        var toolDescriptions = BuildAvailableToolDescriptions();

        // 构建对话历史（排除当前消息，当前消息单独作为 User Question 传入）
        var history = _memory.GetAll();
        var priorMessages = history.Count > 0 && ReferenceEquals(history[^1], userMessage)
            ? history.Take(history.Count - 1).ToList()
            : history;
        var historyText = string.Join("\n",
            priorMessages.Select(m => $"{m.Role}: {m.GetTextContent()}"));

        var promptText = $@"{_systemPrompt}

User Question: {userMessage.GetTextContent()}

Conversation History:
{historyText}

Available Tools:
{toolDescriptions}

Previous Thoughts:
{string.Join("\n", thoughtHistory)}

Current Iteration: {iteration}/{_maxIterations}

Respond in this format:
Thought: [Your reasoning process]
Action: [finish or tool name]
Action Input: [Final answer if finish, or JSON parameters if tool]";

        return Msg.Builder()
            .Role("user")
            .TextContent(promptText)
            .Build();
    }

    private List<Msg> BuildMessageHistory()
    {
        var messages = new List<Msg>();
        
        if (!string.IsNullOrEmpty(_systemPrompt))
        {
            messages.Add(Msg.Builder()
                .Role("system")
                .TextContent(_systemPrompt)
                .Build());
        }

        messages.AddRange(_memory.GetAll());
        return messages;
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

    private string ParseThought(string response)
    {
        var lines = response.Split('\n');
        var thoughtLine = lines.FirstOrDefault(l => 
            l.StartsWith("Thought:", StringComparison.OrdinalIgnoreCase));
        return thoughtLine?.Substring("Thought:".Length).Trim() ?? response;
    }

    private ActionIntent ParseActionIntent(string thought)
    {
        try
        {
            var lines = thought.Split('\n');
            var actionLine = lines.FirstOrDefault(l => 
                l.StartsWith("Action:", StringComparison.OrdinalIgnoreCase));
            var inputLine = lines.FirstOrDefault(l => 
                l.StartsWith("Action Input:", StringComparison.OrdinalIgnoreCase));

            // 如果没有找到 Action 行，说明模型没有按照 ReAct 格式回复
            // 此时将整个回复作为最终答案
            if (actionLine == null)
            {
                return new ActionIntent { Action = "finish", Parameters = thought.Trim(), HasActionLine = false };
            }

            var action = actionLine.Substring("Action:".Length).Trim().ToLower();
            var input = inputLine?.Substring("Action Input:".Length).Trim() ?? "";

            object? parameters = null;
            if (!string.IsNullOrEmpty(input))
            {
                try
                {
                    // 注意：JsonSerializer.Deserialize<Dictionary<string, object>>
                    // 默认将字符串值反序列化为 JsonElement 而非 string，
                    // 导致工具的参数类型检查失败。这里用 JsonNode 解析确保证值类型正确。
                    // Note: JsonSerializer.Deserialize<Dictionary<string, object>>
                    // produces JsonElement for string values by default, breaking
                    // tool parameter type checks. Use JsonNode to get correct types.
                    var node = JsonNode.Parse(input);
                    if (node is JsonObject jsonObj)
                    {
                        parameters = jsonObj.ToDictionary(
                            kvp => kvp.Key,
                            kvp => JsonValueToObject(kvp.Value));
                    }
                    else
                    {
                        parameters = input;
                    }
                }
                catch
                {
                    parameters = input;
                }
            }

            return new ActionIntent { Action = action, Parameters = parameters, HasActionLine = actionLine != null };
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
        return string.IsNullOrWhiteSpace(value) || IsCompletionMarker(value) ? null : value;
    }

    private Msg CreateErrorResponse(string error)
    {
        return Msg.Builder()
            .Name(Name)
            .Role("assistant")
            .TextContent($"错误：{error}")
            .Build();
    }

    public static ReActAgentBuilder Builder()
    {
        return new ReActAgentBuilder();
    }

    /// <summary>
    /// 将 JsonNode 递归转换为对应的 .NET 原生类型（string/long/double/bool/null），
    /// 避免工具参数中拿到 JsonElement 导致类型检查失败。
    /// </summary>
    private static object? JsonValueToObject(JsonNode? node)
    {
        if (node == null) return null;
        if (node is JsonValue jv)
        {
            if (jv.TryGetValue<string>(out var s)) return s;
            if (jv.TryGetValue<long>(out var l)) return l;
            if (jv.TryGetValue<double>(out var d)) return d;
            if (jv.TryGetValue<bool>(out var b)) return b;
            return jv.ToString();
        }
        if (node is JsonArray arr)
        {
            return arr.Select(JsonValueToObject).ToList();
        }
        if (node is JsonObject obj)
        {
            return obj.ToDictionary(kvp => kvp.Key, kvp => JsonValueToObject(kvp.Value));
        }
        return node.ToString();
    }
}

/// <summary>
/// ReActAgent 构建器
/// </summary>
public class ReActAgentBuilder
{
    private string _name = "ReActAgent";
    private IModel? _model;
    private string _sysPrompt = "You are a helpful AI assistant.";
    private IMemory? _memory;
    private readonly List<ITool> _tools = new();
    private ToolGroupManager? _toolGroupManager;
    private int _maxIterations = 10;

    public ReActAgentBuilder Name(string name)
    {
        _name = name;
        return this;
    }

    public ReActAgentBuilder Model(IModel model)
    {
        _model = model;
        return this;
    }

    public ReActAgentBuilder SysPrompt(string prompt)
    {
        _sysPrompt = prompt;
        return this;
    }

    public ReActAgentBuilder Memory(IMemory memory)
    {
        _memory = memory;
        return this;
    }

    public ReActAgentBuilder AddTool(ITool tool)
    {
        _tools.Add(tool);
        return this;
    }

    public ReActAgentBuilder ToolGroupManager(ToolGroupManager manager)
    {
        _toolGroupManager = manager;
        return this;
    }

    public ReActAgentBuilder AddToolGroup(ToolGroup group)
    {
        _toolGroupManager ??= new ToolGroupManager();
        _toolGroupManager.RegisterGroup(group);
        return this;
    }

    public ReActAgentBuilder Tools(IEnumerable<ITool> tools)
    {
        _tools.AddRange(tools);
        return this;
    }

    public ReActAgentBuilder MaxIterations(int max)
    {
        _maxIterations = max;
        return this;
    }

#pragma warning disable CS0618
    public ReActAgent Build()
    {
        if (_model == null)
        {
            throw new InvalidOperationException("必须指定模型");
        }

        return new ReActAgent(_name, _model, _sysPrompt, _memory, _tools, _toolGroupManager, _maxIterations);
    }
#pragma warning restore CS0618
}
