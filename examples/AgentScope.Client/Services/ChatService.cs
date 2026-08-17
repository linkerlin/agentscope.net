using System.Collections.Concurrent;
using AgentScope.Core;
using AgentScope.Core.Agent;
using AgentScope.Core.MCP;
using AgentScope.Core.Memory;
using AgentScope.Core.Message;
using AgentScope.Core.Model;
using AgentScope.Core.Skill;
using AgentScope.Core.Tool;
using AgentScope.Client.Models;

namespace AgentScope.Client.Services;

public class ChatService
{
    private readonly ISessionStore _sessionStore;
    private readonly AgentConfigService _agentConfigService;
    private readonly IProviderFactory _providerFactory;
    private readonly McpConfigService _mcpConfigService;
    private readonly SkillConfigService _skillConfigService;

    /// <summary>保存 MCP 管理器实例，保证 MCP 客户端生命周期与 ChatService 一致</summary>
    private readonly ConcurrentDictionary<Guid, McpManager> _activeMcpManagers = new();

    public ChatService(
        ISessionStore sessionStore,
        AgentConfigService agentConfigService,
        IProviderFactory providerFactory,
        McpConfigService mcpConfigService,
        SkillConfigService skillConfigService)
    {
        _sessionStore = sessionStore;
        _agentConfigService = agentConfigService;
        _providerFactory = providerFactory;
        _mcpConfigService = mcpConfigService;
        _skillConfigService = skillConfigService;
    }

    public async Task<string> SendMessageAsync(Guid sessionId, string text)
    {
        var session = await _sessionStore.GetSessionAsync(sessionId)
                      ?? throw new InvalidOperationException("会话不存在");

        await _sessionStore.SaveMessageAsync(sessionId, "user", text);

        var agent = await BuildAgentAsync(session);
        var userMsg = Msg.Builder().Role("user").TextContent(text).Build();
        var response = await agent.CallAsync(userMsg);
        var reply = response.GetTextContent() ?? string.Empty;

        await _sessionStore.SaveMessageAsync(sessionId, "assistant", reply);

        if (session.Title == "新会话" || string.IsNullOrEmpty(session.Title))
        {
            var title = text.Length > 30 ? text[..30] + "..." : text;
            await _sessionStore.UpdateSessionTitleAsync(sessionId, title);
        }

        return reply;
    }

    public async IAsyncEnumerable<string> StreamMessageAsync(Guid sessionId, string text)
    {
        var session = await _sessionStore.GetSessionAsync(sessionId)
                      ?? throw new InvalidOperationException("会话不存在");

        await _sessionStore.SaveMessageAsync(sessionId, "user", text);

        var agent = await BuildAgentAsync(session);
        var userMsg = Msg.Builder().Role("user").TextContent(text).Build();
        var fullReply = new System.Text.StringBuilder();

        await foreach (var evt in agent.StreamEventsAsync(userMsg))
        {
            if (evt.Message != null)
            {
                var chunk = evt.Message.GetTextContent();
                if (!string.IsNullOrEmpty(chunk))
                {
                    fullReply.Append(chunk);
                    yield return chunk;
                }
            }
        }

        var reply = fullReply.ToString();
        await _sessionStore.SaveMessageAsync(sessionId, "assistant", reply);

        if (session.Title == "新会话" || string.IsNullOrEmpty(session.Title))
        {
            var title = text.Length > 30 ? text[..30] + "..." : text;
            await _sessionStore.UpdateSessionTitleAsync(sessionId, title);
        }
    }

    // ──────────────────────────────────────────────
    // 构建 Agent — 核心逻辑：集成 LLM + MCP + Skill
    // ──────────────────────────────────────────────
    private async Task<EnhancedReActAgent> BuildAgentAsync(ChatSession session)
    {
        var defaultPrompt = "你是一个有用的AI助手。";
        IModel model;
        string agentName = "Assistant";
        string systemPrompt = defaultPrompt;
        int maxIterations = 10;
        AgentConfig? agentCfg = null;

        // ---- 1. 解析 Agent 配置 ----
        if (session.AgentConfigId != null)
        {
            agentCfg = await _agentConfigService.GetAgentAsync(session.AgentConfigId.Value);
            if (agentCfg != null)
            {
                systemPrompt = agentCfg.SystemPrompt ?? defaultPrompt;
                agentName = agentCfg.Name;
                maxIterations = agentCfg.MaxIterations;

                if (agentCfg.ModelId != null)
                {
                    var llmConfigs = await _agentConfigService.GetAllLlmConfigsAsync();
                    var llm = llmConfigs.FirstOrDefault(l => l.Id == agentCfg.ModelId);
                    if (llm != null)
                    {
                        model = _providerFactory.CreateModel(llm);
                        goto modelReady;
                    }
                }
            }
        }

        // 回退到默认 LLM
        {
            var defaultLlm = await _agentConfigService.GetDefaultLlmAsync();
            model = defaultLlm != null
                ? _providerFactory.CreateModel(defaultLlm)
                : new MockModel("mock-model");
        }
        modelReady:

        // ---- 2. 开始构建 Agent ----
        var builder = EnhancedReActAgent.Builder()
            .Name(agentName)
            .SysPrompt(systemPrompt)
            .Model(model)
            .MaxIterations(maxIterations);

        var skillContext = new System.Text.StringBuilder();

        // ---- 3. 集成 MCP 工具 ----
        if (agentCfg?.McpId != null)
        {
            try
            {
                var mcpCfg = await _mcpConfigService.GetAsync(agentCfg.McpId.Value);
                if (mcpCfg != null && mcpCfg.IsEnabled)
                {
                    var mcpTools = await LoadMcpToolsAsync(mcpCfg);
                    if (mcpTools.Count > 0)
                    {
                        var group = new ToolGroup($"mcp-{mcpCfg.Name}", $"MCP: {mcpCfg.Name}");
                        foreach (var tool in mcpTools)
                        {
                            builder.AddTool(tool);
                            group.AddTool(tool.Name);
                        }
                        builder.AddToolGroup(group);

                        skillContext.AppendLine($"- MCP 服务「{mcpCfg.Name}」已连接，提供以下工具：{string.Join("、", mcpTools.Select(t => t.Name))}");
                    }
                }
            }
            catch (Exception ex)
            {
                skillContext.AppendLine($"- MCP 工具加载异常：{ex.Message}");
            }
        }

        // ---- 4. 集成 Skill 工具 ----
        if (agentCfg?.SkillId != null)
        {
            try
            {
                var skillCfg = await _skillConfigService.GetAsync(agentCfg.SkillId.Value);
                if (skillCfg != null && skillCfg.IsActive)
                {
                    var skillInfo = await LoadSkillAsync(skillCfg, builder);
                    if (!string.IsNullOrEmpty(skillInfo))
                    {
                        skillContext.AppendLine(skillInfo);
                    }
                }
            }
            catch (Exception ex)
            {
                skillContext.AppendLine($"- Skill 加载异常：{ex.Message}");
            }
        }

        // ---- 5. 注入技能上下文到 SystemPrompt ----
        if (skillContext.Length > 0)
        {
            builder.SysPrompt($@"{systemPrompt}

【已加载的工具和技能】
{skillContext}

你可以使用上述工具来帮助用户。当需要调用 MCP 工具时，按工具名称调用即可。");
        }

        return builder.Build();
    }

    // ──────────────────────────────────────────────
    // 加载 MCP 工具
    // ──────────────────────────────────────────────
    private async Task<List<ITool>> LoadMcpToolsAsync(McpConfig cfg)
    {
        var client = BuildMcpClient(cfg);
        var manager = new McpManager();

        try
        {
            manager.RegisterClient(client);
            var tools = await manager.CreateToolsAsync();

            // 存 manager 防止 GC 回收 MCP 连接
            _activeMcpManagers[cfg.Id] = manager;

            return tools.ToList();
        }
        catch
        {
            manager.Dispose();
            throw;
        }
    }

    private static IMcpClient BuildMcpClient(McpConfig cfg)
    {
        var builder = McpClientBuilder.Create().Named(cfg.Name);

        switch (cfg.TransportType.ToLowerInvariant())
        {
            case "stdio":
                builder.UseStdio(cfg.Command ?? string.Empty, cfg.Args);
                if (!string.IsNullOrEmpty(cfg.WorkingDirectory))
                    builder.WithWorkingDirectory(cfg.WorkingDirectory);
                break;

            case "http":
                builder.UseStreamableHttp(cfg.Url ?? string.Empty);
                break;

            case "sse":
                builder.UseSse(cfg.Url ?? string.Empty);
                break;
        }

        if (!string.IsNullOrEmpty(cfg.ApiKey))
            builder.WithApiKey(cfg.ApiKey);

        return builder.Build();
    }

    // ──────────────────────────────────────────────
    // 加载 Skill
    // ──────────────────────────────────────────────
    private async Task<string> LoadSkillAsync(SkillConfig cfg, EnhancedReActAgentBuilder builder)
    {
        string content;
        string sourceDesc;

        if (cfg.SourceType == "file" && !string.IsNullOrEmpty(cfg.SourcePath))
        {
            // 从 Markdown 文件加载
            var fullPath = Path.GetFullPath(cfg.SourcePath);
            if (!File.Exists(fullPath))
                return $"- Skill「{cfg.Name}」文件不存在: {fullPath}";

            content = await File.ReadAllTextAsync(fullPath);
            sourceDesc = $"文件 {cfg.SourcePath}";
        }
        else if (cfg.SourceType == "inline" && !string.IsNullOrEmpty(cfg.RawContent))
        {
            content = cfg.RawContent;
            sourceDesc = "内嵌配置";
        }
        else
        {
            return $"- Skill「{cfg.Name}」无有效内容，跳过";
        }

        // 使用 AgentScope.Core 的 MarkdownSkillParser 解析
        var parser = new MarkdownSkillParser();
        RegisteredSkill? registered = null;
        try
        {
            if (cfg.SourceType == "file" && !string.IsNullOrEmpty(cfg.SourcePath))
                registered = parser.ParseFile(Path.GetFullPath(cfg.SourcePath));
            else
                registered = parser.Parse(content, cfg.Name);
        }
        catch
        {
            // 解析失败时，直接将内容作为说明文本
        }

        var skillDesc = registered?.Description ?? cfg.Description ?? cfg.Name;
        var toolNames = registered?.ToolNames;
        var toolList = toolNames is { Count: > 0 }
            ? $"引用工具：{string.Join("、", toolNames)}"
            : "";

        // 将 Skill 内容注入 SystemPrompt 上下文
        // AgentScope.Core 的 Skill 是声明式 Markdown，真正的工具实现由 MCP/内置工具提供
        // 这里我们注入 markdown 内容作为 Agent 的知识上下文
        var skillContext = $@"
### Skill: {cfg.Name}
- 描述: {skillDesc}
- 来源: {sourceDesc}
- 内容:
```markdown
{content}
```
";
        if (!string.IsNullOrEmpty(toolList))
        {
            skillContext += $"\n- {toolList}";
        }

        return skillContext;
    }
}
