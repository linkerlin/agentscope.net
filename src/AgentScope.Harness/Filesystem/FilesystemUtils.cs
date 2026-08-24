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

namespace AgentScope.Harness.Filesystem;

/// <summary>
/// Filesystem utility class: path normalization, relative path resolution, size formatting, etc.
/// 文件系统工具类：路径规范化、相对路径解析、大小格式化等通用辅助。
/// Counterpart to Java: io.agentscope.harness.agent.filesystem.util.FilesystemUtils
/// </summary>
public static class FilesystemUtils
{
    /// <summary>
    /// Normalize path to forward slashes and remove redundant separators.
    /// 把路径规范化为正斜杠、去除冗余分隔。
    /// </summary>
    /// <param name="path">原始路径 / Raw path</param>
    /// <returns>规范化后的路径 / Normalized path</returns>
    public static string Normalize(string path)
    {
        if (string.IsNullOrEmpty(path)) return path;
        return path.Replace('\\', '/').TrimEnd('/');
    }

    /// <summary>
    /// Check whether a path is rooted under baseDir (cross-platform).
    /// 判断 path 是否以 baseDir 为根（跨平台）。
    /// </summary>
    /// <param name="path">待检查路径 / Path to check</param>
    /// <param name="baseDir">根目录 / Base directory</param>
    /// <returns>true 如果 path 在 baseDir 下 / true if path is within baseDir</returns>
    public static bool IsWithin(string path, string baseDir)
    {
        var n = Normalize(path);
        var b = Normalize(baseDir);
        return n.StartsWith(b + "/", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(n, b, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Resolve a relative path under the given root directory and normalize.
    /// 把相对路径拼接在 root 下，并规范化。
    /// </summary>
    /// <param name="root">根目录 / Root directory</param>
    /// <param name="relative">相对路径 / Relative path</param>
    /// <returns>解析后的完整路径 / Resolved full path</returns>
    public static string Resolve(string root, string relative)
    {
        if (string.IsNullOrEmpty(relative)) return Normalize(root);
        var rel = Normalize(relative);
        if (Path.IsPathRooted(relative)) return rel; // 绝对路径直接返回
        return Normalize($"{Normalize(root)}/{rel}");
    }

    /// <summary>
    /// Format byte count as a human-readable string.
    /// 把字节数格式化为人类可读字符串。
    /// </summary>
    /// <param name="bytes">字节数 / Byte count</param>
    /// <returns>可读字符串如 "1.5 MB" / Human-readable string like "1.5 MB"</returns>
    public static string HumanSize(long bytes)
    {
        string[] units = { "B", "KB", "MB", "GB", "TB" };
        double size = bytes;
        var u = 0;
        while (size >= 1024 && u < units.Length - 1)
        {
            size /= 1024;
            u++;
        }

        return $"{size:0.##} {units[u]}";
    }
}
