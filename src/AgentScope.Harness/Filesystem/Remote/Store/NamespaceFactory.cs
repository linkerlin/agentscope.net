// Copyright 2024-2026 the original author or authors.
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

namespace AgentScope.Harness.Filesystem.Remote.Store;

/// <summary>
/// 命名空间工厂：把（命名空间, key）映射为存储层全局唯一键，避免多租户/多会话键冲突。
/// 对应 Java: io.agentscope.harness.agent.filesystem.remote.store.NamespaceFactory
/// </summary>
public static class NamespaceFactory
{
    /// <summary>分隔符。</summary>
    public const string Separator = "::";

    /// <summary>构造命名空间下的键。</summary>
    public static string Key(string ns, string key)
    {
        if (string.IsNullOrEmpty(ns)) return key;
        return $"{ns}{Separator}{key}";
    }

    /// <summary>从全局键解析命名空间（返回 null 表示无命名空间）。</summary>
    public static string? Namespace(string fullKey)
    {
        var idx = fullKey?.IndexOf(Separator, StringComparison.Ordinal);
        if (idx is <= 0) return null;
        return fullKey![..idx.Value];
    }

    /// <summary>从全局键解析原始 key。</summary>
    public static string BareKey(string fullKey)
    {
        var idx = fullKey.IndexOf(Separator, StringComparison.Ordinal);
        return idx < 0 ? fullKey : fullKey[(idx + Separator.Length)..];
    }
}
