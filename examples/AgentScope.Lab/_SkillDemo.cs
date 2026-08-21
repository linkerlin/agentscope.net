using AgentScope.Core;
using AgentScope.Core.Agent;
using AgentScope.Core.Events;
using AgentScope.Core.Message;
using AgentScope.Core.Model;
using AgentScope.Core.Model.OpenAI;
using AgentScope.Core.Skill;
using AgentScope.Core.Tool;
using AgentScope.Harness;
using AgentScope.Harness.Middleware;
using AgentScope.Harness.Workspace;
using DotNetEnv;

namespace AgentScope.Lab;

/// <summary>
/// 技能加载演示：从 workspace skills/ 目录自动发现并注册技能，
/// 使 Agent 能够感知和使用 workspace 中定义的技能。
/// </summary>
public class SkillDemo
{
    private IModel? _model;
    private WorkspaceManager? _ws;

    public SkillDemo()
    {
        // 加载 .env 中的 API Key（优先当前目录，其次仓库根目录）
        var localEnv = Path.Combine(Directory.GetCurrentDirectory(), ".env");
        var rootEnv = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "..", "..", ".env"));
        if (File.Exists(localEnv)) Env.Load(localEnv);
        else if (File.Exists(rootEnv)) Env.Load(rootEnv);

        Console.WriteLine("AgentScope.Lab — 技能加载演示");
        Console.WriteLine("====================================\n");

        // 模型：私有化部署的 OpenAI 兼容端点（无需真实 Key，填 "none"）
        IModel model = new OpenAIModel(
            "DeepSeek-V4-Flash",
            "none",
            "http://10.193.41.51:8198/v1");
        this._model = model;
    }

    public async Task ChatStream()
    {
        // === 1. 创建工作区 ===
        _ws = new WorkspaceManager(".agentscope/workspace", sandboxed: true);

        // === 2. 从 workspace skills/ .skills/ .skill/ 目录发现并加载技能 ===
        var toolkit = new Toolkit();
        var wsFullPath = Path.GetFullPath(_ws.WorkspaceRoot);
        var skillRoots = new[] { "skills", ".skills", ".skill" };

        Console.WriteLine($"扫描工作区技能目录: {wsFullPath}");
        var parser = new MarkdownSkillParser();
        var loadedSkills = new List<ISkill>();
        var totalFound = 0;

        foreach (var subDir in skillRoots)
        {
            var dir = Path.Combine(wsFullPath, subDir);
            if (!Directory.Exists(dir)) continue;

            var files = Directory.GetFiles(dir, "SKILL.md",
                SearchOption.AllDirectories);
            totalFound += files.Length;

            foreach (var file in files)
            {
                try
                {
                    var registered = parser.ParseFile(file);
                    var skillFolder = Path.GetFileName(
                        Path.GetDirectoryName(file) ?? "");

                    Console.WriteLine($"  [{subDir}/{skillFolder}] {registered.Name}: {registered.Description}");

                    loadedSkills.Add(new MarkdownSkill(registered, isActive: true));
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"  [!] 解析失败: {file} - {ex.Message}");
                }
            }
        }

        if (loadedSkills.Count > 0)
        {
            SkillToolFactory.RegisterSkills(toolkit, loadedSkills);
            Console.WriteLine($"\n共发现 {totalFound} 个技能文件，成功注册 {loadedSkills.Count} 个到 Toolkit\n");
        }
        else
        {
            Console.WriteLine(totalFound == 0
                ? "  未发现任何技能文件\n"
                : $"  发现 {totalFound} 个技能文件，但均注册失败\n");
        }

        // === 3. 模型连通性自检 ===
        var pingResp = await _model!.GenerateAsync(new ModelRequest
        {
            Messages = new List<Msg>
            {
                Msg.Builder().Role("user").TextContent("ping").Build()
            }
        });
        Console.WriteLine($"模型连通性自检: [{pingResp.Text}]\n");

        // === 4. 构建 HarnessAgent（Builder 自动注册文件工具 + workspace 中间件） ===
        HarnessAgent agent = new HarnessAgentBuilder()
            .WithName("skill-agent")
            .WithModel(_model)
            .WithWorkspace(_ws)
            .WithToolkit(toolkit)       // 注入包含已注册技能的 Toolkit
            .WithMiddleware(new CompactionMiddleware(maxContextLength: 4096))
            .Build();

        // 运行时上下文
        RuntimeContext ctx = RuntimeContext.Empty
            .WithUserId("alice")
            .WithSessionId("skill-demo");

        // === 5. 第一轮：询问技能 ===
        Msg first = Msg.Builder()
            .Role("user")
            .TextContent("你有什么技能可用？")
            .Build();

        Console.WriteLine("第一轮 (问：你有什么技能可用？):\n");
        await foreach (var ev in agent.StreamEventsAsync(first, ctx))
        {
            if (ev.Type == EventType.ReasoningChunk && ev.Message != null)
            {
                Console.Write(ev.Message.GetTextContent());
            }
            else if (ev.Type == EventType.ToolCallStart)
            {
                Console.WriteLine("\n[工具] 模型请求调用工具");
            }
            else if (ev.IsLast)
            {
                Console.WriteLine("\n[done]\n");
            }
        }

        // === 6. 第二轮：身份确认 ===
        Msg second = Msg.Builder()
            .Role("user")
            .TextContent("你是谁？")
            .Build();

        Console.WriteLine("第二轮 (问：你是谁？):\n");
        await foreach (var ev in agent.StreamEventsAsync(second, ctx))
        {
            if (ev.Type == EventType.ReasoningChunk && ev.Message != null)
            {
                Console.Write(ev.Message.GetTextContent());
            }
            else if (ev.Type == EventType.ToolCallStart)
            {
                Console.WriteLine("\n[工具] 模型请求调用工具");
            }
            else if (ev.IsLast)
            {
                Console.WriteLine("\n[done]\n");
            }
        }
    }
}
