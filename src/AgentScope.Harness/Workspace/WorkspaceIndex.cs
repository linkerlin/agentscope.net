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

using System.Collections.Concurrent;

namespace AgentScope.Harness.Workspace;

/// <summary>SQLite 文件索引，对应 Java WorkspaceIndex</summary>
public sealed class WorkspaceIndex
{
    private readonly ConcurrentDictionary<string, HashSet<string>> _index = new();

    public void Add(string filePath, string? tag = null)
    {
        var key = tag ?? "default";
        _index.AddOrUpdate(key,
            _ => new HashSet<string> { filePath },
            (_, set) => { set.Add(filePath); return set; });
    }

    public void Remove(string filePath, string? tag = null)
    {
        var key = tag ?? "default";
        if (_index.TryGetValue(key, out var set))
            set.Remove(filePath);
    }

    public IReadOnlySet<string> GetByTag(string? tag = null)
    {
        var key = tag ?? "default";
        return _index.TryGetValue(key, out var set)
            ? set.ToHashSet()
            : new HashSet<string>();
    }

    public bool Contains(string filePath)
    {
        return _index.Values.Any(set => set.Contains(filePath));
    }

    public void Clear() => _index.Clear();
}
