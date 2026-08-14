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
/// 工具危险路径常量：定义文件/命令类工具中需额外审批或拒绝的危险路径与关键字。
/// 对应 Java: io.agentscope.core.tool.ToolDangerousPathConstants
/// </summary>
public static class ToolDangerousPathConstants
{
    /// <summary>系统级危险目录前缀（写入/删除需审批）。</summary>
    public static readonly IReadOnlyCollection<string> SystemSensitivePaths = new[]
    {
        "/etc", "/usr", "/bin", "/sbin", "/boot", "/proc", "/sys", "/dev",
        "C:\\Windows", "C:\\Program Files", "C:\\Program Files (x86)",
        "/System", "/Library"
    };

    /// <summary>敏感配置/密钥文件名（读写需审批）。</summary>
    public static readonly IReadOnlyCollection<string> SensitiveFileNames = new[]
    {
        ".env", "id_rsa", "id_dsa", "id_ed25519", ".npmrc", ".pypirc", ".aws/credentials",
        "credentials", "secrets.json", ".git-credentials", ".netrc"
    };

    /// <summary>危险命令关键字（Shell 执行需审批）。</summary>
    public static readonly IReadOnlyCollection<string> DangerousCommandKeywords = new[]
    {
        "rm -rf", "mkfs", "dd if=", ":(){:|:&};:", "chmod -R", "shutdown", "reboot",
        "shutdown.exe", "format ", "del /f", "rmdir /s", "reg delete"
    };

    /// <summary>判断路径是否落在系统敏感目录下。</summary>
    public static bool IsSystemSensitive(string path)
    {
        if (string.IsNullOrEmpty(path)) return false;
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

    /// <summary>判断文件名是否为敏感密钥/凭据文件。</summary>
    public static bool IsSensitiveFile(string fileName)
    {
        if (string.IsNullOrEmpty(fileName)) return false;
        foreach (var sensitive in SensitiveFileNames)
        {
            if (fileName.IndexOf(sensitive, System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>判断命令字符串是否包含危险关键字。</summary>
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
