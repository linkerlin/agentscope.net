using AgentScope.Core;
using AgentScope.Core.Agent;
using AgentScope.Core.Events;
using AgentScope.Core.Message;
using AgentScope.Core.Model;
using AgentScope.Core.Model.OpenAI;
using AgentScope.Core.Session;
using AgentScope.Core.State;
using AgentScope.Core.Tool;
using AgentScope.Harness;
using AgentScope.Harness.Middleware;
using AgentScope.Harness.Subagent;
using DotNetEnv;

namespace AgentScope.Lab;

/// <summary>
/// 子 Agent 演示：创建专用子 Agent、注册到管理器、主 Agent 编排调用。
/// </summary>
public class SubagentDemo
{
    private IModel? _model;

    public SubagentDemo()
    {
        var localEnv = Path.Combine(Directory.GetCurrentDirectory(), ".env");
        var rootEnv = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "..", "..", ".env"));
        if (File.Exists(localEnv)) Env.Load(localEnv);
        else if (File.Exists(rootEnv)) Env.Load(rootEnv);

        Console.WriteLine("AgentScope.Lab — 子 Agent 演示");
        Console.WriteLine("====================================\n");

        IModel model = new OpenAIModel(
            "DeepSeek-V4-Flash",
            "none",
            "http://10.193.41.51:8198/v1");
        _model = model;
    }

    public async Task ChatStream()
    {
        var sessionManager = new SessionManager();

        // === 1. 创建专用子 Agent + 封装为 SubAgentTool ===
        // "研究员" 智能体：专注信息检索与分析
        var researcher = new HarnessAgentBuilder()
            .WithName("researcher")
            .WithSystemPrompt("你是一个研究员智能体。你擅长分析问题、查找信息、给出详细的分析报告。回答请简洁但信息丰富。")
            .WithModel(_model!)
            .WithWorkspaceRoot(Path.GetFullPath(".agentscope/workspace"))
            .Build();
        var researcherTool = new SubAgentToolWrapper(
            researcher,
            sessionManager.CreateSession("researcher"),
            "call_researcher",
            "调用研究员子 Agent 分析问题或查找信息。参数: message(必填) 发送给研究员的消息, session_id(可选) 会话ID");

        // "写作助手" 智能体：专注文字润色与格式化输出
        var writer = new HarnessAgentBuilder()
            .WithName("writer")
            .WithSystemPrompt("你是一个写作助手智能体。你擅长将复杂的内容整理成清晰、结构化的文字。回答请使用 Markdown 格式。")
            .WithModel(_model!)
            .WithWorkspaceRoot(Path.GetFullPath(".agentscope/workspace"))
            .Build();
        var writerTool = new SubAgentToolWrapper(
            writer,
            sessionManager.CreateSession("writer"),
            "call_writer",
            "调用写作助手子 Agent 整理和润色文字。参数: message(必填) 发送给写作助手的内容, session_id(可选) 会话ID");

        // === 2. 将子 Agent 工具注册到 Toolkit ===
        var toolkit = new Toolkit();
        toolkit.AddTool(researcherTool);
        toolkit.AddTool(writerTool);

        // === 3. 构建编排主 Agent（注入 Toolkit） ===
        HarnessAgent orchestrator = new HarnessAgentBuilder()
            .WithName("orchestrator")
            .WithSystemPrompt(
                "你是一个团队协调员。你有以下工具可以调用子 Agent 来协作完成任务：\n" +
                "- call_researcher：调用研究员子 Agent 分析问题、查找信息\n" +
                "- call_writer：调用写作助手子 Agent 整理和润色文字\n" +
                "对于复杂任务，先用 call_researcher 做分析，再用 call_writer 整理结果。")
            .WithModel(_model!)
            .WithToolkit(toolkit)
            .WithWorkspaceRoot(Path.GetFullPath(".agentscope/workspace"))
            .Build();

        // === 4. 模型连通性自检 ===
        var pingResp = await _model!.GenerateAsync(new ModelRequest
        {
            Messages = new List<Msg>
            {
                Msg.Builder().Role("user").TextContent("ping").Build()
            }
        });
        Console.WriteLine($"模型连通性自检: [{pingResp.Text}]\n");

        // === 5. 对话：主 Agent 调度子 Agent ===
        RuntimeContext ctx = RuntimeContext.Empty
            .WithUserId("alice")
            .WithSessionId("subagent-demo");

        Msg first = Msg.Builder()
            .Role("user")
            .TextContent("请分析一下 .NET 平台在 AI 应用开发中的优势和劣势。")
            .Build();

        Console.WriteLine("用户: 请分析一下 .NET 平台在 AI 应用开发中的优势和劣势。\n");
        Console.WriteLine("===== 主 Agent 回复 =====\n");

        // 只输出最终回复（跳过内部 ReAct 思考过程），保留工具调用标记
        var finalText = new System.Text.StringBuilder();
        await foreach (var ev in orchestrator.StreamEventsAsync(first, ctx))
        {
            if (ev.Type == EventType.ReasoningChunk && ev.Message != null)
            {
                finalText.Append(ev.Message.GetTextContent());
            }
            else if (ev.Type == EventType.ToolCallStart)
            {
                finalText.Append("\n[工具调用] 子 Agent 已被调用...\n");
            }
        }
        Console.WriteLine(finalText + "\n");

        // === 6. 第二轮：确认记忆 ===
        Msg second = Msg.Builder()
            .Role("user")
            .TextContent("之前我们讨论了什么话题？")
            .Build();

        Console.WriteLine("用户: 之前我们讨论了什么话题？\n");
        Console.WriteLine("===== 主 Agent 回复 =====\n");

        var finalText2 = new System.Text.StringBuilder();
        await foreach (var ev in orchestrator.StreamEventsAsync(second, ctx))
        {
            if (ev.Type == EventType.ReasoningChunk && ev.Message != null)
            {
                finalText2.Append(ev.Message.GetTextContent());
            }
        }
        Console.WriteLine(finalText2.ToString().Trim());
    }

    /// <summary>
    /// 演示 SubAgentTool：将子 Agent 作为工具暴露给主 Agent 调用。
    /// </summary>
    public async Task ChatWithTool()
    {
        var sessionManager = new SessionManager();
        var session = sessionManager.CreateSession(name: "subagent-tool-demo");

        // 创建专用子 Agent
        var researcher = new HarnessAgentBuilder()
            .WithName("researcher")
            .WithSystemPrompt("你是一个研究员智能体。你擅长分析问题、给出详细的分析报告。")
            .WithModel(_model!)
            .WithWorkspaceRoot(Path.GetFullPath(".agentscope/workspace"))
            .Build();

        var subAgentTool = new SubAgentToolWrapper(
            researcher, session, "call_researcher",
            "调用研究员子 Agent 分析问题。参数: message(必填) 发送给研究员的消息, session_id(可选) 会话ID续接上下文");

        var toolkit = new Toolkit();
        toolkit.AddTool(subAgentTool);

        // 构建主 Agent 并注入 toolkit（含 subAgentTool）
        HarnessAgent orchestrator = new HarnessAgentBuilder()
            .WithName("orchestrator-tool")
            .WithSystemPrompt("你是一个团队协调员。你有 call_researcher 工具可以用来调用研究员子 Agent 分析复杂问题。")
            .WithModel(_model!)
            .WithToolkit(toolkit)
            .WithWorkspaceRoot(Path.GetFullPath(".agentscope/workspace"))
            .Build();

        var pingResp = await _model!.GenerateAsync(new ModelRequest
        {
            Messages = new List<Msg>
            {
                Msg.Builder().Role("user").TextContent("ping").Build()
            }
        });
        Console.WriteLine($"模型连通性自检: [{pingResp.Text}]\n");

        RuntimeContext ctx = RuntimeContext.Empty
            .WithUserId("bob")
            .WithSessionId("subagent-tool");

        Msg msg = Msg.Builder()
            .Role("user")
            .TextContent("请帮我分析 C# 中的 async/await 机制的原理和最佳实践。")
            .Build();

        Console.WriteLine("用户: 请帮我分析 C# 中的 async/await 机制的原理和最佳实践。\n");
        Console.WriteLine("===== 主 Agent 回复 =====\n");

        await foreach (var ev in orchestrator.StreamEventsAsync(msg, ctx))
        {
            if (ev.Type == EventType.ReasoningChunk && ev.Message != null)
            {
                Console.Write(ev.Message.GetTextContent());
            }
            else if (ev.Type == EventType.ToolCallStart)
            {
                Console.WriteLine("\n[工具调用] 主 Agent 正在调用研究员子 Agent...\n");
            }
            else if (ev.IsLast)
            {
                Console.WriteLine("\n\n[done]");
            }
        }
    }
}

/// <summary>
/// 将子 Agent 封装为 Tool，返回纯文本结果（避免 SubAgentTool 的字典序列化问题）。
/// </summary>
public class SubAgentToolWrapper : ToolBase
{
    private readonly IAgent _agent;
    private readonly Session _session;

    public SubAgentToolWrapper(IAgent agent, Session session, string name, string description)
        : base(name, description)
    {
        _agent = agent;
        _session = session;
    }

    public override Dictionary<string, object> GetSchema()
    {
        return new Dictionary<string, object>
        {
            ["name"] = Name,
            ["description"] = Description,
            ["parameters"] = new Dictionary<string, object>
            {
                ["message"] = new Dictionary<string, object>
                {
                    ["type"] = "string",
                    ["description"] = "发送给子 Agent 的消息",
                    ["required"] = true
                },
                ["session_id"] = new Dictionary<string, object>
                {
                    ["type"] = "string",
                    ["description"] = "会话 ID，不传则新建",
                    ["required"] = false
                }
            }
        };
    }

    public override async Task<ToolResult> ExecuteAsync(Dictionary<string, object> parameters)
    {
        if (!parameters.TryGetValue("message", out var msgObj) || msgObj == null)
            return ToolResult.Fail("缺少必需参数: message");

        var message = msgObj.ToString() ?? "";
        var sessionId = parameters.TryGetValue("session_id", out var sidObj)
            ? sidObj?.ToString() : null;

        if (sessionId != null && _agent is IStateModule sm)
            sm.LoadIfExists(_session, sessionId);

        // 指示子 Agent 直接回答，不走 ReAct 格式
        var fullMsg = message + "\n\n请直接输出回答内容，不要输出任何格式模板。";
        Msg response;
        try
        {
            response = await _agent.CallAsync(
                Msg.Builder().TextContent(fullMsg).Build());
        }
        catch (Exception ex)
        {
            return ToolResult.Fail("子 Agent 调用异常: " + ex.Message);
        }

        if (_agent is IStateModule sm2)
            sm2.SaveTo(_session, sessionId ?? Guid.NewGuid().ToString());

        var text = response?.GetTextContent() ?? "(空回复)";

        // 剥离可能的 ReAct 格式模板残留
        var idx = text.LastIndexOf("请以以下格式回答");
        if (idx >= 0) text = text[..idx].Trim();

        // 直接返回纯文本，ToolResultConverter.ToText 会正确序列化
        return ToolResult.Ok(text);
    }
}
