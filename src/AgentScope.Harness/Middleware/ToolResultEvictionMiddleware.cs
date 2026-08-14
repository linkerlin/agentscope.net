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
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using AgentScope.Core.Message;
using AgentScope.Harness.Filesystem;
using AgentScope.Harness.Memory.Compaction;

namespace AgentScope.Harness.Middleware;

/// <summary>
/// 大工具结果驱逐中间件：在下游推理看到臃肿消息列表之前，把超长的
/// <see cref="ToolResultBlock"/> 内容写入文件系统，并用带预览的占位符替换。
/// <para>对标 Java: io.agentscope.harness.agent.middleware.ToolResultEvictionMiddleware</para>
/// <list type="number">
///   <item>把完整结果写到 <c>{evictionPath}/{agentName}/{toolCallId}-{contentHash}</c>；</item>
///   <item>用占位符（含首尾预览 + 读取指引）替换原文本；</item>
///   <item>在元数据打上 <c>agentscope.tool_result_evicted</c>，避免后续回合重复驱逐；</item>
///   <item><see cref="ToolResultEvictionConfig.ExcludedToolNames"/> 中的工具永不驱逐。</item>
/// </list>
/// </summary>
public sealed partial class ToolResultEvictionMiddleware(
    IFilesystem filesystem,
    ToolResultEvictionConfig? config = null) : IHarnessMiddleware
{
    /// <summary>已驱逐标记的元数据键。对标 Java <c>EVICTED_METADATA_KEY</c>。</summary>
    public const string EvictedMetadataKey = "agentscope.tool_result_evicted";

    private readonly ToolResultEvictionConfig _config = config ?? new ToolResultEvictionConfig();

    public int Order => 30;

    public async ValueTask OnAgentAsync(MiddlewareContext ctx, Func<ValueTask> next, CancellationToken ct = default)
    {
        if (_config.Enabled)
        {
            try
            {
                await EvictMessagesAsync(ctx.Messages, ctx.AgentName, ct).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                // 驱逐属于优化手段，任何失败都不得中断 Agent 回合
                Console.Error.WriteLine($"[{ctx.AgentName}] 工具结果驱逐失败: {ex.Message}");
            }
        }
        await next().ConfigureAwait(false);
    }

    public ValueTask OnModelCallAsync(MiddlewareContext ctx, Func<ValueTask> next, CancellationToken ct = default)
        => next();

    public ValueTask OnToolExecutionAsync(MiddlewareContext ctx, Func<ValueTask> next, CancellationToken ct = default)
        => next();

    /// <summary>
    /// 就地驱逐消息列表中的超大工具结果。相同 (id, name, text) 指纹只落盘一次。
    /// </summary>
    private async Task EvictMessagesAsync(List<Msg> messages, string agentName, CancellationToken ct)
    {
        if (messages.Count == 0) return;

        var replacements = new Dictionary<(string Id, string Name, string Text), string>();

        foreach (var msg in messages)
        {
            switch (msg.Content)
            {
                // 常规形态：内容为内容块列表
                case IList<ContentBlock> blocks:
                {
                    for (var i = 0; i < blocks.Count; i++)
                    {
                        if (blocks[i] is not ToolResultBlock tr) continue;
                        var replaced = await ResolveAsync(tr, agentName, replacements, ct).ConfigureAwait(false);
                        if (replaced != null) blocks[i] = replaced;
                    }
                    break;
                }

                // 退化形态：内容直接是单个工具结果块
                case ToolResultBlock single:
                {
                    var replaced = await ResolveAsync(single, agentName, replacements, ct).ConfigureAwait(false);
                    if (replaced != null) msg.Content = replaced;
                    break;
                }
            }
        }
    }

    /// <summary>返回替换后的块；无需驱逐时返回 null。</summary>
    private async Task<ToolResultBlock?> ResolveAsync(
        ToolResultBlock tr,
        string agentName,
        Dictionary<(string, string, string), string> replacements,
        CancellationToken ct)
    {
        if (IsEvicted(tr)) return null;

        var text = tr.ExtractText();
        var key = (tr.Id, tr.Name ?? "", text);

        if (!replacements.TryGetValue(key, out var placeholder))
        {
            placeholder = await MaybeEvictAsync(tr, text, agentName, ct).ConfigureAwait(false);
            if (placeholder == null) return null;
            replacements[key] = placeholder;
        }

        return WithPlaceholder(tr, placeholder);
    }

    /// <summary>判断是否需要驱逐；需要则落盘并返回占位符，否则返回 null。</summary>
    private async Task<string?> MaybeEvictAsync(ToolResultBlock tr, string fullText, string agentName, CancellationToken ct)
    {
        if (tr.Name != null && _config.ExcludedToolNames.Contains(tr.Name)) return null;
        if (fullText.Length <= _config.MaxResultChars) return null;

        var evictionPath = BuildEvictionPath(agentName, tr.Id, fullText);
        var writeResult = await filesystem.WriteAsync(evictionPath, fullText, ct).ConfigureAwait(false);
        if (!writeResult.Success)
        {
            Console.Error.WriteLine(
                $"[{agentName}] 驱逐工具结果失败 [tool={tr.Name}, id={tr.Id}]: {writeResult.Error}");
            return null;
        }

        return BuildPlaceholder(fullText, evictionPath);
    }

    private static bool IsEvicted(ToolResultBlock tr) =>
        tr.Metadata != null
        && tr.Metadata.TryGetValue(EvictedMetadataKey, out var v)
        && v is true;

    private static ToolResultBlock WithPlaceholder(ToolResultBlock tr, string placeholder)
    {
        var metadata = tr.Metadata != null
            ? new Dictionary<string, object>(tr.Metadata)
            : new Dictionary<string, object>();
        metadata[EvictedMetadataKey] = true;

        return tr with
        {
            Output = new List<ContentBlock> { new TextBlock { Text = placeholder } },
            Metadata = metadata
        };
    }

    private string BuildEvictionPath(string agentName, string toolCallId, string fullText)
    {
        var basePath = _config.EvictionPath.TrimEnd('/');
        var safeAgent = SanitizeRegex().Replace(agentName ?? "", "_");
        var safeId = SanitizeRegex().Replace(toolCallId ?? "", "_");
        return $"{basePath}/{safeAgent}/{safeId}-{ContentHash(fullText)}";
    }

    private static string ContentHash(string content)
    {
        var digest = SHA256.HashData(Encoding.UTF8.GetBytes(content));
        return Convert.ToHexString(digest, 0, 8).ToLowerInvariant();
    }

    private string BuildPlaceholder(string fullText, string evictionPath)
    {
        var len = fullText.Length;
        var previewLen = Math.Min(_config.PreviewChars, len / 2);

        var sb = new StringBuilder();
        sb.Append(string.Format(
            CultureInfo.InvariantCulture,
            "Tool output was too large ({0:N0} chars) and has been saved to `{1}`.{2}" +
            "To read the full output, use `read_file` with path `{1}`.{2}{2}",
            len, evictionPath, Environment.NewLine));

        if (previewLen > 0)
        {
            sb.AppendFormat(CultureInfo.InvariantCulture,
                "Preview (first {0:N0} chars):{1}", previewLen, Environment.NewLine);
            sb.Append(fullText, 0, previewLen);
            sb.AppendFormat(CultureInfo.InvariantCulture,
                "{1}{1}... and last {0:N0} chars:{1}", previewLen, Environment.NewLine);
            sb.Append(fullText, len - previewLen, previewLen);
        }

        return sb.ToString();
    }

    [GeneratedRegex("[^a-zA-Z0-9_-]")]
    private static partial Regex SanitizeRegex();
}
