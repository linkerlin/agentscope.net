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

using System.Globalization;
using System.Text;
using AgentScope.Harness.Workspace;

namespace AgentScope.Harness.Middleware;

/// <summary>
/// 工作区上下文中间件：在系统提示词末尾追加会话环境、工作区说明、记忆与领域知识。
/// <para>对标 Java: io.agentscope.harness.agent.middleware.WorkspaceContextMiddleware</para>
/// <para>
/// 注入结构（与 Java 一致）：
/// <c>## AgentStateStore Context</c> → 指引段（Domain Knowledge / Memory Recall / Memory Persistence）
/// → <c>## Workspace</c> → <c>&lt;loaded_context&gt;</c>（agents / memory / domain_knowledge / 附加文件）。
/// </para>
/// <para>
/// 记忆内容受 token 预算约束：<c>estimateTokens = len/4</c>，超出部分截断并追加提示。
/// </para>
/// </summary>
public sealed class WorkspaceContextMiddleware(
    WorkspaceManager workspaceManager,
    string agentName = "HarnessAgent",
    string? environmentMemory = null,
    int maxContextTokens = WorkspaceContextMiddleware.DefaultMaxContextTokens,
    bool disableMemoryTools = false,
    bool disableMemoryHooks = false) : IHarnessMiddleware
{
    /// <summary>默认上下文 token 预算。对标 Java <c>DEFAULT_MAX_CONTEXT_TOKENS</c>。</summary>
    public const int DefaultMaxContextTokens = 8000;

    /// <summary>额外注入的工作区文件（相对路径）。对标 Java <c>setAdditionalContextFiles</c>。</summary>
    public IReadOnlyList<string> AdditionalContextFiles { get; set; } = [];

    public int Order => 25;

    public ValueTask OnAgentAsync(MiddlewareContext ctx, Func<ValueTask> next, CancellationToken ct = default)
        => next();

    public ValueTask OnModelCallAsync(MiddlewareContext ctx, Func<ValueTask> next, CancellationToken ct = default)
        => next();

    public ValueTask OnToolExecutionAsync(MiddlewareContext ctx, Func<ValueTask> next, CancellationToken ct = default)
        => next();

    /// <inheritdoc />
    public async ValueTask<string> OnSystemPromptAsync(MiddlewareContext ctx, string prompt, CancellationToken ct = default)
    {
        var section = await BuildWorkspaceSectionAsync(ctx, ct).ConfigureAwait(false);
        if (string.IsNullOrEmpty(section)) return prompt;

        var separator = string.IsNullOrEmpty(prompt) || prompt.EndsWith('\n') ? "" : "\n\n";
        return prompt + separator + section;
    }

    /// <summary>是否需要注入记忆上下文：工具与钩子全禁用时不注入。</summary>
    private bool IncludeMemoryContext => !(disableMemoryTools && disableMemoryHooks);

    private async Task<string> BuildWorkspaceSectionAsync(MiddlewareContext ctx, CancellationToken ct)
    {
        var agents = (await workspaceManager.ReadAgentsMdAsync(ct).ConfigureAwait(false))?.Trim() ?? "";
        var memory = IncludeMemoryContext
            ? (await workspaceManager.ReadMemoryMdAsync(ct).ConfigureAwait(false))?.Trim() ?? ""
            : "";
        var knowledgeMd = (await workspaceManager.ReadKnowledgeMdAsync(ct).ConfigureAwait(false))?.Trim() ?? "";

        var knowledgeBlock = BuildKnowledgeBlock(knowledgeMd);
        var additionalBlocks = await ReadAdditionalFilesAsync(ct).ConfigureAwait(false);
        var additionalText = string.Join("\n", additionalBlocks.Values);

        var sessionContext = BuildSessionContext(ctx);

        // Token 预算：约 4 字符 / token，记忆内容优先被截断
        memory = ApplyMemoryBudget(memory, sessionContext, agents, knowledgeBlock, additionalText);

        var sb = new StringBuilder();
        sb.Append(sessionContext).Append("\n\n");
        sb.Append(BuildGuidance()).Append('\n');
        sb.Append(BuildWorkspaceParagraph()).Append('\n');
        sb.Append(BuildLoadedContext(agents, memory, knowledgeBlock, additionalBlocks));
        return sb.ToString();
    }

    private static int EstimateTokens(string text) => string.IsNullOrEmpty(text) ? 0 : text.Length / 4;

    private string ApplyMemoryBudget(string memory, string session, string agents, string knowledge, string additional)
    {
        if (string.IsNullOrEmpty(memory)) return memory;

        var used = EstimateTokens(session) + EstimateTokens(agents)
                   + EstimateTokens(knowledge) + EstimateTokens(additional);
        var available = maxContextTokens - used;
        if (available <= 0) return "";
        if (EstimateTokens(memory) <= available) return memory;

        var keep = Math.Max(0, Math.Min(memory.Length, available * 4));
        var notice = disableMemoryTools
            ? "\n\n... (memory truncated) ...\n"
            : "\n\n... (memory truncated — use memory_search for older entries) ...\n";
        return memory[..keep] + notice;
    }

    private string BuildSessionContext(MiddlewareContext ctx)
    {
        var sb = new StringBuilder();
        sb.Append("## AgentStateStore Context\n\n");
        sb.Append($"You are {agentName}.\n");
        sb.Append($"Today is {DateTime.Now.ToString("dddd MMM d, yyyy", CultureInfo.InvariantCulture)}.\n");
        sb.Append($"Operating system: {Environment.OSVersion.Platform} {Environment.OSVersion.Version}\n");
        sb.Append($"Workspace directory: {workspaceManager.WorkspaceRoot}\n");
        sb.Append($"Temp directory: {Path.GetTempPath()}\n");

        var dynamic = new List<string>();
        var sessionId = ctx.SessionId;
        if (!string.IsNullOrWhiteSpace(sessionId)) dynamic.Add($"AgentStateStore ID: {sessionId}");
        if (!string.IsNullOrWhiteSpace(environmentMemory)) dynamic.Add(environmentMemory!);
        if (dynamic.Count > 0) sb.Append(string.Join("\n", dynamic)).Append('\n');

        return sb.ToString().TrimEnd();
    }

    private string BuildGuidance()
    {
        var sb = new StringBuilder();

        sb.Append("## Domain Knowledge\n");
        sb.Append("Domain knowledge below is authoritative for this workspace. ");
        sb.Append("Prefer it over general assumptions, and cite the source file when you rely on it.\n");

        if (!disableMemoryTools)
        {
            sb.Append("\n## Memory Recall\n");
            sb.Append("Use `memory_search` to look up older entries not shown below, ");
            sb.Append("and `memory_get` to read a specific entry in full. ");
            sb.Append("When you use a recalled item, reference it as `Source: <path#line>`.\n");
        }

        if (!disableMemoryTools)
        {
            sb.Append("\n## Memory Persistence\n");
            sb.Append("Use `memory_save` to persist durable facts, decisions and user preferences. ");
            sb.Append("Never use `write_file` or `edit_file` on `MEMORY.md` or anything under `memory/` — ");
            sb.Append("those paths are managed by the memory subsystem.\n");
        }
        else if (!disableMemoryHooks)
        {
            sb.Append("\n## Memory Persistence (hooks only)\n");
            sb.Append("Memory tools are disabled. Do not use `write_file` or `edit_file` on ");
            sb.Append("`MEMORY.md` or anything under `memory/`.\n");
        }

        if (!disableMemoryHooks)
        {
            sb.Append("Memory is also automatically extracted at conversation end.\n");
        }

        return sb.ToString();
    }

    private string BuildWorkspaceParagraph()
    {
        var sb = new StringBuilder();
        sb.Append("\n## Workspace\n");
        sb.Append($"Your working directory is: {workspaceManager.WorkspaceRoot}\n");
        sb.Append("Files you create should stay inside this directory unless the user says otherwise.\n");
        sb.Append("`AGENTS.md` in the workspace root records the conventions you must follow here.\n");
        return sb.ToString();
    }

    private string BuildKnowledgeBlock(string knowledgeMd)
    {
        var files = workspaceManager.ListKnowledgeFiles();
        if (string.IsNullOrEmpty(knowledgeMd) && files.Count == 0) return "";

        var sb = new StringBuilder();
        if (!string.IsNullOrEmpty(knowledgeMd)) sb.Append(knowledgeMd).Append('\n');
        if (files.Count > 0)
        {
            sb.Append("Knowledge files:\n");
            foreach (var f in files) sb.Append("- ").Append(f).Append('\n');
        }
        return sb.ToString().TrimEnd();
    }

    private async Task<Dictionary<string, string>> ReadAdditionalFilesAsync(CancellationToken ct)
    {
        var result = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var rel in AdditionalContextFiles)
        {
            try
            {
                var content = await workspaceManager.ReadAsync(rel, ct).ConfigureAwait(false);
                if (!string.IsNullOrWhiteSpace(content)) result[TagFor(rel)] = content.Trim();
            }
            catch
            {
                // 附加上下文文件读取失败不影响主流程
            }
        }
        return result;
    }

    /// <summary>相对路径 → XML 标签名：<c>/</c> 与 <c>.</c> 均替换为 <c>_</c> 后小写。</summary>
    private static string TagFor(string relativePath)
        => relativePath.Replace('/', '_').Replace('\\', '_').Replace('.', '_').ToLowerInvariant();

    private string BuildLoadedContext(
        string agents, string memory, string knowledge, Dictionary<string, string> additional)
    {
        var sb = new StringBuilder();
        sb.Append('\n');
        sb.Append(IncludeMemoryContext
            ? "The following workspace context (conventions, memory and domain knowledge) is already loaded. Do not re-read these files.\n"
            : "The following workspace context (conventions and domain knowledge) is already loaded. Do not re-read these files.\n");

        sb.Append("<loaded_context>\n");
        AppendTag(sb, "agents_context", agents);
        if (IncludeMemoryContext) AppendTag(sb, "memory_context", memory);
        AppendTag(sb, "domain_knowledge_context", knowledge);
        foreach (var (tag, content) in additional) AppendTag(sb, tag, content);
        sb.Append("</loaded_context>\n");
        return sb.ToString();
    }

    private static void AppendTag(StringBuilder sb, string tag, string content)
    {
        if (string.IsNullOrEmpty(content))
        {
            sb.Append("  <").Append(tag).Append(" />\n");
            return;
        }
        sb.Append("  <").Append(tag).Append(">\n");
        foreach (var line in content.Split('\n'))
            sb.Append("  ").Append(line.TrimEnd('\r')).Append('\n');
        sb.Append("  </").Append(tag).Append(">\n");
    }
}
