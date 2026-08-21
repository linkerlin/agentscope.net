using AgentScope.Core.Agent;
using AgentScope.Core.Events;
using AgentScope.Core.Message;
using AgentScope.Core.Model;
using AgentScope.Core.Model.OpenAI;
using AgentScope.Harness;
using AgentScope.Harness.Middleware;
using AgentScope.Harness.Workspace;
using DotNetEnv;


public class WorkspaceDemo
{ 
    private IModel? _model;


    public WorkspaceDemo()
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


    public async Task  ChatStream()
    {
        // 模型连通性自检
        var pingResp = await _model!.GenerateAsync(new ModelRequest
        {
            Messages = new List<Msg> { Msg.Builder().Role("user").TextContent("ping").Build() }
        });
        Console.WriteLine($"模型连通性自检: [{pingResp.Text}]\n");
      
        var ws = new WorkspaceManager(".agentscope/workspace", sandboxed: true);   // IAsyncDisposable

        // 构建 HarnessAgent（工作区 + 上下文压缩中间件）
        HarnessAgent agent = new HarnessAgentBuilder()
            .WithName("note-taker")
            // .WithSystemPrompt("你是一个帮助用户做笔记的助手。")
            .WithModel(_model)
            .WithWorkspace(ws)
            // .WithWorkspaceRoot(Path.GetFullPath(".agentscope/workspace"))
            .WithMiddleware(new CompactionMiddleware(maxContextLength: 4096))
            .Build();

        // 运行时上下文：同一 (userId, sessionId) 跨调用恢复状态
        RuntimeContext ctx = RuntimeContext.Empty
            .WithUserId("alice")
            .WithSessionId("demo-session");

        // 第一轮：自我介绍 + 当天的事
        Msg first = Msg.Builder()
            .Role("user")
            .TextContent("你有什么技能可用？请读一下 生物 技能的内容")
            .Build();

        Console.WriteLine($"第一轮: \n");
        await foreach (var ev in agent.StreamEventsAsync(first, ctx))
        {
            // Console.WriteLine($"[{ev.Type}] {ev.Message?.GetTextContent()}");

            if(ev.Type == EventType.ReasoningFinish&& ev.Message != null)
            {
                Console.Write(ev.Message.GetTextContent());
            }

            // if (ev.Type == EventType.ReasoningChunk && ev.Message != null)
            // {
            //     // 模型输出的流式文本片段
            //     Console.Write(ev.Message.GetTextContent());
            // }
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
            .TextContent("你刚才做了什么？")
            .Build();

        // StreamEventsAsync：流式逐事件输出（SSE 风格）
        Console.WriteLine($"第二轮: ");
        await foreach (var ev in agent.StreamEventsAsync(second, ctx))
        {
           // Console.WriteLine($"[{ev.Type}] {ev.Message?.GetTextContent()}");

            if(ev.Type == EventType.ReasoningFinish&& ev.Message != null)
            {
                Console.Write(ev.Message.GetTextContent());
            }

            // if (ev.Type == EventType.ReasoningChunk && ev.Message != null)
            // {
            //     // 模型输出的流式文本片段
            //     Console.Write(ev.Message.GetTextContent());
            // }
            else if (ev.Type == EventType.ToolCallStart)
            {
                Console.WriteLine("\n[tool] 模型请求调用工具");
            }
            else if (ev.IsLast)
            {
                Console.WriteLine("\n[done]");
            }
        }
    }
  
    public async Task WorkspaceFeature()
    {
        var ws = new WorkspaceManager(".agentscope/workspace", sandboxed: true);   // IAsyncDisposable

        // 读 / 写（相对根目录；sandboxed 模式下锚定根目录，拒绝 .. 遍历）
        string? content = await ws.ReadAsync("AGENTS.md");
        await ws.WriteAsync("notes/todo.md", "- 完成文档\n");

        // 内置约定文件
        string? agentsMd = await ws.ReadAgentsMdAsync();
        string? memoryMd = await ws.ReadMemoryMdAsync();
        string? knowledgeMd = await ws.ReadKnowledgeMdAsync();

        // 查询
        bool exists = ws.Exists("notes/todo.md");
        var files = ws.ListFiles("notes", pattern: "*.md");
        var knowledge = ws.ListKnowledgeFiles();
        DateTime? lastWrite = ws.GetLastWriteTimeUtc("AGENTS.md");

        // 管理
        ws.Move("notes/todo.md", "notes/done.md");
        ws.Delete("notes/done.md");
        await ws.DisposeAsync();
    }
}