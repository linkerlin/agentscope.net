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

using System.Text;
using System.Text.RegularExpressions;
using AgentScope.Core.Message;
using AgentScope.Harness.Workspace;

namespace AgentScope.Harness.Middleware;

/// <summary>
/// <c>@path</c> 展开中间件：把用户消息里形如 <c>@src/Foo.cs</c> 的引用读成附件正文，
/// 追加到该条消息末尾的 <c>&lt;attached_file&gt;</c> 标签里。
/// <para>对标 Java: io.agentscope.harness.agent.middleware.AtPathExpansionMiddleware</para>
/// <para>
/// 正则要求 token 必须含 <c>/</c>、<c>.</c> 或 <c>~</c>，从而不会误吞 <c>@alice</c> 这类提及。
/// 读取失败（不存在 / 越权 / 二进制）时静默跳过，原始 <c>@path</c> 文本保持不变。
/// </para>
/// </summary>
public sealed partial class AtPathExpansionMiddleware(WorkspaceManager workspaceManager) : IHarnessMiddleware
{
    /// <summary>单个附件最多附带的行数。对标 Java <c>MAX_ATTACHED_LINES</c>。</summary>
    public const int MaxAttachedLines = 1000;

    public int Order => 20;

    public ValueTask OnAgentAsync(MiddlewareContext ctx, Func<ValueTask> next, CancellationToken ct = default)
    {
        try
        {
            Expand(ctx);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"@path 展开失败: {ex.Message}");
        }
        return next();
    }

    public ValueTask OnModelCallAsync(MiddlewareContext ctx, Func<ValueTask> next, CancellationToken ct = default)
        => next();

    public ValueTask OnToolExecutionAsync(MiddlewareContext ctx, Func<ValueTask> next, CancellationToken ct = default)
        => next();

    private void Expand(MiddlewareContext ctx)
    {
        for (var i = 0; i < ctx.Messages.Count; i++)
        {
            var msg = ctx.Messages[i];
            if (!string.Equals(msg.Role, "user", StringComparison.OrdinalIgnoreCase)) continue;

            var text = msg.GetTextContent();
            if (string.IsNullOrEmpty(text) || !text.Contains('@')) continue;

            // LinkedHashMap 语义：同一引用只读一次，且保持首次出现顺序
            var attachments = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (Match m in AtPathRegex().Matches(text))
            {
                var reference = m.Groups["path"].Value;
                if (string.IsNullOrEmpty(reference) || attachments.ContainsKey(reference)) continue;

                var content = TryRead(reference);
                if (content != null) attachments[reference] = content;
            }

            if (attachments.Count == 0) continue;

            var sb = new StringBuilder(text);
            foreach (var (reference, content) in attachments)
            {
                sb.Append("\n\n<attached_file path=\"").Append(reference).Append("\">\n");
                sb.Append(content);
                if (!content.EndsWith('\n')) sb.Append('\n');
                sb.Append("</attached_file>");
            }

            ctx.Messages[i] = new Msg(msg.Name, sb.ToString(), msg.Role)
            {
                Id = msg.Id,
                Timestamp = msg.Timestamp,
                Metadata = msg.Metadata,
                Url = msg.Url
            };
        }
    }

    /// <summary>读取引用文件；不存在、越权或疑似二进制时返回 null。</summary>
    private string? TryRead(string reference)
    {
        try
        {
            var path = reference;
            if (path.StartsWith("~/", StringComparison.Ordinal) || path == "~")
            {
                var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                path = path.Length > 1 ? Path.Combine(home, path[2..]) : home;
            }

            var full = Path.IsPathRooted(path)
                ? path
                : Path.Combine(workspaceManager.WorkspaceRoot, path);

            if (!File.Exists(full)) return null;

            var lines = File.ReadLines(full).Take(MaxAttachedLines).ToList();
            var content = string.Join("\n", lines);

            // 含 NUL 字节视为二进制，跳过
            return content.Contains('\0') ? null : content;
        }
        catch
        {
            // 安全策略拒绝或 IO 异常：静默跳过，保持原文
            return null;
        }
    }

    /// <summary>
    /// 匹配 <c>@</c> 引用。要求 token 含 <c>/</c>、<c>.</c> 或 <c>~</c>，
    /// 且前一个字符不是标识符字符（避免命中邮箱等）。
    /// </summary>
    [GeneratedRegex(@"(?<![A-Za-z0-9_])@(?<path>[A-Za-z]:[\\/][\w\\./\-~]*|[~./][\w./\-~]*|/[\w./\-~]+|[\w\-]+[/.][\w./\-~]*)")]
    private static partial Regex AtPathRegex();
}
