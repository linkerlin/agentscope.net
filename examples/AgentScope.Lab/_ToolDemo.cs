






using AgentScope.Core;
using AgentScope.Core.Agent;
using AgentScope.Core.Events;
using AgentScope.Core.Message;
using AgentScope.Core.Model;
using AgentScope.Core.Model.OpenAI;
using AgentScope.Core.Tool;
using AgentScope.Core.MCP;
using AgentScope.Harness;
using AgentScope.Harness.Middleware;
using DotNetEnv;


namespace AgentScope.Lab;

public class ToolDemo
{
    private IModel? _model;
    private Toolkit _toolkit = new Toolkit();

    public ToolDemo()
    {
        // 加载 .env 中的 API Key（优先当前目录，其次仓库根目录）
        var localEnv = Path.Combine(Directory.GetCurrentDirectory(), ".env");
        var rootEnv = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "..", "..", ".env"));
        if (File.Exists(localEnv)) Env.Load(localEnv);
        else if (File.Exists(rootEnv)) Env.Load(rootEnv);

        Console.WriteLine("AgentScope.Lab — 框架用法实验工程");
        Console.WriteLine("====================================");

        // 模型：私有化部署的 OpenAI 兼容端点（无需真实 Key，填 "none"）
        IModel model = new OpenAIModel(
            "DeepSeek-V4-Flash",
            "none",
            "http://10.193.41.51:8198/v1");
        this._model = model;
        // Console.WriteLine(model);
    }

    public void RegisterTool()
    {

        _toolkit.RegisterTool(new WeatherService());   // 扫描实例上的 [Tool] 方法
                                                       // _toolkit.RegisterTool<MathTools>();            // 扫描类型 T 的静态 [Tool] 方法

    }
    
    public async Task RegisterMcp()
    {
        // Streamable HTTP / SSE（远程服务）
        IMcpClient http = McpClientBuilder.Create()
            .Named("excel-mcp-http")
            .UseStreamableHttp("http://10.193.41.51:9151/mcp")
            // .WithApiKey("YOUR_KEY") // Authorization: Bearer
            .WithRequestTimeout(TimeSpan.FromSeconds(60))
            .Build();

        // 注册到 McpManager 并发现工具
        var manager = new McpManager();
        manager.RegisterClient(http);
        IReadOnlyList<ITool> tools = await manager.CreateToolsAsync(); // 自动初始化并发现
 
        foreach (var tool in tools)
            _toolkit.AddTool(tool);
    }

    public async Task Chat()
    {
        // 模型连通性自检
        var pingResp = await this._model!.GenerateAsync(new ModelRequest
        {
            Messages = new List<Msg> { Msg.Builder().Role("user").TextContent("ping").Build() }
        });
        Console.WriteLine($"模型连通性自检: [{pingResp.Text}]\n");

        // 构建 HarnessAgent（工作区 + 上下文压缩中间件）
        HarnessAgent agent = new HarnessAgentBuilder()
            .WithName("note-taker")
            .WithSystemPrompt("你是一个帮助用户做笔记的助手。")
            .WithModel(_model)
            .WithWorkspaceRoot(Path.GetFullPath(".agentscope/workspace"))
            .WithMiddleware(new CompactionMiddleware(maxContextLength: 4096))
            .Build();

        // 运行时上下文：同一 (userId, sessionId) 跨调用恢复状态
        RuntimeContext ctx = RuntimeContext.Empty
            .WithUserId("alice")
            .WithSessionId("demo-session");

        // 第一轮：自我介绍 + 当天的事
        Msg first = Msg.Builder()
            .Role("user")
            .TextContent("我叫超级买卖无敌汉堡王，今天准备一个关于 铅基反应堆 的技术分享。")
            .Build();
        var reply1 = await agent.CallAsync(first, ctx);
        Console.WriteLine($"第一轮: [{reply1.GetTextContent()}]\n");

        // 第二轮：同 sessionId，自动恢复上一轮状态后回答
        Msg second = Msg.Builder()
            .Role("user")
            .TextContent("我叫什么？我今天要干什么？")
            .Build();
        var reply2 = await agent.CallAsync(second, ctx);
        Console.WriteLine($"第二轮: [{reply2.GetTextContent()}]");
    }


    public async Task ChatStream()
    {
        // 模型连通性自检
        var pingResp = await _model!.GenerateAsync(new ModelRequest
        {
            Messages = new List<Msg> { Msg.Builder().Role("user").TextContent("ping").Build() }
        });
        Console.WriteLine($"模型连通性自检: [{pingResp.Text}]\n");

        await RegisterMcp();
        // 构建 HarnessAgent（工作区 + 上下文压缩中间件）
        HarnessAgent agent = new HarnessAgentBuilder()
            .WithName("note-taker")
            .WithSystemPrompt("你是一个帮助用户做笔记的助手。")
            .WithModel(_model)
            .WithWorkspaceRoot(Path.GetFullPath(".agentscope/workspace"))
            .WithMiddleware(new CompactionMiddleware(maxContextLength: 4096))
            .WithToolkit(_toolkit)  // 注册工具集
            .Build();

        // 运行时上下文：同一 (userId, sessionId) 跨调用恢复状态
        RuntimeContext ctx = RuntimeContext.Empty
            .WithUserId("alice")
            .WithSessionId("demo-session");

        // 第一轮：自我介绍 + 当天的事
        Msg first = Msg.Builder()
            .Role("user")
            .TextContent("你有什么MCP工具")
            .Build();

        Console.WriteLine($"第一轮: \n");
        await foreach (var ev in agent.StreamEventsAsync(first, ctx))
        {
            Console.WriteLine($"[{ev.Type}] {ev.Message?.GetTextContent()}");

            if (ev.Type == EventType.ReasoningChunk && ev.Message != null)
            {
                // 模型输出的流式文本片段
                Console.Write(ev.Message.GetTextContent());
            }
            else if (ev.Type == EventType.ToolCallStart)
            {
                Console.WriteLine("\n[tool] 模型请求调用工具");
            }
            else if (ev.IsLast)
            {
                Console.WriteLine("\n[done]");
            }

        }
        // var reply1 = await agent.CallAsync(first, ctx);

        // 第二轮：同 sessionId，自动恢复上一轮状态后回答
        Msg second = Msg.Builder()
            .Role("user")
            // .TextContent("我叫什么？我今天要干什么？")
            .TextContent("列出你的MCP工具？")
            .Build();

        // StreamEventsAsync：流式逐事件输出（SSE 风格）
        Console.WriteLine($"第二轮: ");
        await foreach (var ev in agent.StreamEventsAsync(second, ctx))
        {
            Console.WriteLine($"[{ev.Type}] {ev.Message?.GetTextContent()}");
            if (ev.Type == EventType.ToolCallStart)
            {
                Console.WriteLine("\n[tool] 模型请求调用工具");
            }
        }
    }
}



public class WeatherService
{
    [Tool(Name = "get_weather", Description = "获取指定城市的天气")]
    public string GetWeather(
        [ToolParam(Name = "city", Description = "城市名")] string city,
        [ToolParam(Description = "天数", Required = false)] int days = 3)
        => $"{city} 未来 {days} 天晴。";
}