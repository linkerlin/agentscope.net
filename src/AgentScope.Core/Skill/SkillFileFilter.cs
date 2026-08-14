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

using System;
using System.IO;
using System.Linq;

namespace AgentScope.Core.Skill;

/// <summary>
/// 技能文件过滤器：在文件系统扫描阶段按扩展名/目录排除规则筛选候选技能文件。
/// 对应 Java: io.agentscope.core.skill.SkillFileFilter
/// </summary>
public class SkillFileFilter
{
    /// <summary>允许的技能文件扩展名（小写，含点），默认 .md。</summary>
    public ISet<string> AllowedExtensions { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".md" };

    /// <summary>排除的目录名（如 node_modules/.git）。</summary>
    public ISet<string> ExcludedDirectories { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "node_modules", ".git", "bin", "obj", "target", "dist"
    };

    /// <summary>最大文件大小（字节），超过则跳过。</summary>
    public long MaxFileSizeBytes { get; set; } = 512 * 1024;

    /// <summary>判断文件是否为候选技能文件。</summary>
    public bool Accepts(string path)
    {
        if (string.IsNullOrEmpty(path)) return false;

        var fileName = Path.GetFileName(path);
        var ext = Path.GetExtension(path);
        if (!AllowedExtensions.Contains(ext)) return false;

        // 路径中任一段落在排除目录则拒绝
        var dirParts = path.Replace('\\', '/').Split('/', StringSplitOptions.RemoveEmptyEntries);
        foreach (var part in dirParts.Take(dirParts.Length - 1)) // 排除文件名本身
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
            // 无法访问文件信息时按通过处理（扫描阶段再判断）
        }

        // 跳过以 . 或 _ 开头的隐藏文件
        if (fileName.StartsWith('.') || fileName.StartsWith('_')) return false;

        return true;
    }
}
