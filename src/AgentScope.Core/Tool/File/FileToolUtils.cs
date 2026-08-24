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

namespace AgentScope.Core.Tool.File;

/// <summary>
/// 文件工具安全工具：路径沙箱，限制访问范围，防止越权。
/// </summary>
public static class FileToolUtils
{
    private static readonly string[] DefaultAllowedRoots = { Environment.CurrentDirectory, Path.GetTempPath() };

    /// <summary>
    /// 允许的根目录列表（子路径可访问）。默认包含当前目录与临时目录。
    /// </summary>
    public static IReadOnlyList<string> AllowedRoots { get; set; } = DefaultAllowedRoots;

    /// <summary>
    /// 检查路径是否在允许的沙箱内（规范化为绝对路径后，必须位于某 AllowedRoots 之下）。
    /// </summary>
    public static bool IsPathAllowed(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return false;
        try
        {
            var full = Path.GetFullPath(path.Trim());
            foreach (var root in AllowedRoots)
            {
                if (string.IsNullOrWhiteSpace(root))
                    continue;
                var rootFull = Path.GetFullPath(root.Trim());
                if (full.Equals(rootFull, StringComparison.OrdinalIgnoreCase) ||
                    full.StartsWith(rootFull.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 获取规范化的绝对路径；若不在沙箱内则返回 null。
    /// </summary>
    public static string? GetAllowedFullPath(string path)
    {
        if (!IsPathAllowed(path))
            return null;
        try
        {
            return Path.GetFullPath(path.Trim());
        }
        catch
        {
            return null;
        }
    }
}
