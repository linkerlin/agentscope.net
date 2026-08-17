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

using System;
using System.IO;
using System.Linq;

namespace AgentScope.Core.Skill;

/// <summary>
/// File filter for skill discovery: filters candidate skill files by extension, directory exclusion rules, and file size.
/// 技能文件过滤器：在文件系统扫描阶段按扩展名、目录排除规则和文件大小筛选候选技能文件。
/// Corresponds to Java: io.agentscope.core.skill.SkillFileFilter
/// </summary>
public class SkillFileFilter
{
    /// <summary>
    /// Allowed skill file extensions (lowercase, with dot). Default is ".md".
    /// 允许的技能文件扩展名（小写，含点），默认 .md。
    /// </summary>
    public ISet<string> AllowedExtensions { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".md" };

    /// <summary>
    /// Directory names to exclude (e.g., node_modules, .git).
    /// 排除的目录名（如 node_modules、.git）。
    /// </summary>
    public ISet<string> ExcludedDirectories { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "node_modules", ".git", "bin", "obj", "target", "dist"
    };

    /// <summary>
    /// Maximum file size in bytes. Files larger than this are skipped.
    /// 最大文件大小（字节），超过则跳过。
    /// </summary>
    public long MaxFileSizeBytes { get; set; } = 512 * 1024;

    /// <summary>
    /// Determines whether the given file path is a candidate skill file.
    /// 判断给定文件路径是否为候选技能文件。
    /// </summary>
    /// <param name="path">The file path to check. / 要检查的文件路径。</param>
    /// <returns>True if the file is accepted as a skill file; otherwise false. / 如果文件被接受为技能文件则返回 true，否则返回 false。</returns>
    public bool Accepts(string path)
    {
        if (string.IsNullOrEmpty(path)) return false;

        var fileName = Path.GetFileName(path);
        var ext = Path.GetExtension(path);
        if (!AllowedExtensions.Contains(ext)) return false;

        // Reject if any path segment is in the excluded directories list
        // 路径中任一段落在排除目录则拒绝
        var dirParts = path.Replace('\\', '/').Split('/', StringSplitOptions.RemoveEmptyEntries);
        foreach (var part in dirParts.Take(dirParts.Length - 1)) // exclude the file name itself / 排除文件名本身
        {
            if (ExcludedDirectories.Contains(part)) return false;
        }

        try
        {
            var info = new FileInfo(path);
            if (info.Exists && info.Length > MaxFileSizeBytes) return false;
        }
        catch
        {
            // If file info is inaccessible, pass through (will be re-evaluated during scan)
            // 无法访问文件信息时按通过处理（扫描阶段再判断）
        }

        // Skip hidden files starting with '.' or '_'
        // 跳过以 . 或 _ 开头的隐藏文件
        if (fileName.StartsWith('.') || fileName.StartsWith('_')) return false;

        return true;
    }
}
