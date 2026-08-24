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

using System.Collections.Generic;

namespace AgentScope.Core.Tool;

/// <summary>
/// Dangerous path constants for tool safety: defines dangerous paths and keywords that require
/// additional approval or denial for file/command tools.
/// 工具危险路径常量：定义文件/命令类工具中需额外审批或拒绝的危险路径与关键字。
/// Corresponds to Java: io.agentscope.core.tool.ToolDangerousPathConstants
/// </summary>
public static class ToolDangerousPathConstants
{
    /// <summary>
    /// System-level sensitive directory prefixes. Writing to or deleting files under these paths
    /// requires additional approval.
    /// 系统级危险目录前缀（写入/删除需审批）。
    /// </summary>
    public static readonly IReadOnlyCollection<string> SystemSensitivePaths = new[]
    {
        "/etc", "/usr", "/bin", "/sbin", "/boot", "/proc", "/sys", "/dev",
        "C:\\Windows", "C:\\Program Files", "C:\\Program Files (x86)",
        "/System", "/Library"
    };

    /// <summary>
    /// Sensitive configuration/credential file names. Reading or writing these requires approval.
    /// 敏感配置/密钥文件名（读写需审批）。
    /// </summary>
    public static readonly IReadOnlyCollection<string> SensitiveFileNames = new[]
    {
        ".env", "id_rsa", "id_dsa", "id_ed25519", ".npmrc", ".pypirc", ".aws/credentials",
        "credentials", "secrets.json", ".git-credentials", ".netrc"
    };

    /// <summary>
    /// Dangerous command keywords. Shell execution containing these requires approval.
    /// 危险命令关键字（Shell 执行需审批）。
    /// </summary>
    public static readonly IReadOnlyCollection<string> DangerousCommandKeywords = new[]
    {
        "rm -rf", "mkfs", "dd if=", ":(){:|:&};:", "chmod -R", "shutdown", "reboot",
        "shutdown.exe", "format ", "del /f", "rmdir /s", "reg delete"
    };

    /// <summary>
    /// Checks whether the given path falls under any system-sensitive directory.
    /// 判断路径是否落在系统敏感目录下。
    /// </summary>
    /// <param name="path">Path to check / 待检查的路径</param>
    /// <returns>True if sensitive / 若在敏感目录内则返回 true</returns>
    public static bool IsSystemSensitive(string path)
    {
        if (string.IsNullOrEmpty(path)) return false;
        // 统一路径分隔符为 /，便于跨平台前缀匹配
        var normalized = path.Replace('\\', '/').TrimEnd('/');
        foreach (var sensitive in SystemSensitivePaths)
        {
            var s = sensitive.Replace('\\', '/');
            if (normalized.StartsWith(s, System.StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Checks whether the file name matches any sensitive credential/secret file pattern.
    /// 判断文件名是否为敏感密钥/凭据文件。
    /// </summary>
    /// <param name="fileName">File name to check / 待检查的文件名</param>
    /// <returns>True if it is a sensitive file / 若是敏感文件则返回 true</returns>
    public static bool IsSensitiveFile(string fileName)
    {
        if (string.IsNullOrEmpty(fileName)) return false;
        foreach (var sensitive in SensitiveFileNames)
        {
            // 子串匹配，可识别路径中的文件名部分
            if (fileName.IndexOf(sensitive, System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Checks whether the command string contains any dangerous keyword.
    /// 判断命令字符串是否包含危险关键字。
    /// </summary>
    /// <param name="command">Command string to check / 待检查的命令字符串</param>
    /// <returns>True if dangerous content is found / 若包含危险内容则返回 true</returns>
    public static bool ContainsDangerousCommand(string command)
    {
        if (string.IsNullOrEmpty(command)) return false;
        foreach (var keyword in DangerousCommandKeywords)
        {
            if (command.IndexOf(keyword, System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }
        }

        return false;
    }
}
