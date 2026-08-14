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

namespace AgentScope.Harness.Filesystem;

/// <summary>
/// 文件系统工具类：路径规范化、相对路径解析、大小格式化等通用辅助。
/// 对应 Java: io.agentscope.harness.agent.filesystem.util.FilesystemUtils
/// </summary>
public static class FilesystemUtils
{
    /// <summary>把路径规范化为正斜杠、去除冗余分隔。</summary>
    public static string Normalize(string path)
    {
        if (string.IsNullOrEmpty(path)) return path;
        return path.Replace('\\', '/').TrimEnd('/');
    }

    /// <summary>判断 path 是否以 baseDir 为根（跨平台）。</summary>
    public static bool IsWithin(string path, string baseDir)
    {
        var n = Normalize(path);
        var b = Normalize(baseDir);
        return n.StartsWith(b + "/", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(n, b, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>把相对路径拼接在 root 下，并规范化。</summary>
    public static string Resolve(string root, string relative)
    {
        if (string.IsNullOrEmpty(relative)) return Normalize(root);
        var rel = Normalize(relative);
        if (Path.IsPathRooted(relative)) return rel; // 绝对路径直接返回
        return Normalize($"{Normalize(root)}/{rel}");
    }

    /// <summary>把字节数格式化为人类可读字符串。</summary>
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
