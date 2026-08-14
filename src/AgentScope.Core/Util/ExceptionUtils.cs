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

namespace AgentScope.Core.Util;

/// <summary>
/// 异常工具类：提取根因、堆栈摘要与友好消息。
/// 对应 Java: io.agentscope.core.util.ExceptionUtils
/// </summary>
public static class ExceptionUtils
{
    /// <summary>递归获取根因异常。</summary>
    public static System.Exception? GetRootCause(System.Exception? ex)
    {
        while (ex?.InnerException != null)
        {
            ex = ex.InnerException;
        }

        return ex;
    }

    /// <summary>获取根因消息（无异常返回 null）。</summary>
    public static string? GetRootMessage(System.Exception? ex) => GetRootCause(ex)?.Message;

    /// <summary>将异常（含所有内部异常）格式化为可读字符串。</summary>
    public static string ToLongString(System.Exception? ex)
    {
        if (ex == null) return "";
        var sb = new StringBuilder();
        var depth = 0;
        for (var current = ex; current != null; current = current.InnerException)
        {
            if (depth > 0) sb.Append(" -> ");
            sb.Append('[').Append(current.GetType().Name).Append("] ").Append(current.Message);
            depth++;
        }

        return sb.ToString();
    }

    /// <summary>堆栈摘要（前 maxLines 行）。</summary>
    public static string StackSummary(System.Exception? ex, int maxLines = 5)
    {
        if (ex?.StackTrace == null) return "";
        var lines = ex.StackTrace.Split('\n');
        var take = System.Math.Min(maxLines, lines.Length);
        var sb = new StringBuilder();
        for (var i = 0; i < take; i++)
        {
            sb.Append(lines[i].Trim()).Append('\n');
        }

        return sb.ToString();
    }
}
