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

namespace AgentScope.Harness.Workspace;

/// <summary>
/// 路径安全策略。对标 Java PathPolicy。
/// 用于 LocalFsMode.Rooted 模式，限制允许的绝对路径根目录。
/// </summary>
public sealed class PathPolicy(IReadOnlySet<string> allowedRoots, IReadOnlySet<string>? denied = null)
{
    public IReadOnlySet<string> AllowedRoots { get; } = allowedRoots;
    public IReadOnlySet<string> Denied { get; } = denied ?? new HashSet<string>();

    public void EnsureAllowed(string path)
    {
        var full = Path.GetFullPath(path);

        if (Denied.Any(d => full.StartsWith(d, StringComparison.OrdinalIgnoreCase)))
            throw new UnauthorizedAccessException($"路径被策略禁止: {path}");

        if (!AllowedRoots.Any(r => full.StartsWith(r, StringComparison.OrdinalIgnoreCase)))
            throw new UnauthorizedAccessException($"路径超出允许根目录: {path}");
    }

    public static PathPolicy FromWorkspace(string workspaceRoot) =>
        new(new HashSet<string>([workspaceRoot]));
}
