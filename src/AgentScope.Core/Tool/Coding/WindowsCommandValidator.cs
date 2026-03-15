// Copyright 2024-2026 the original author or authors.
// Licensed under the Apache License, Version 2.0

namespace AgentScope.Core.Tool.Coding;

/// <summary>
/// Windows 命令校验器：禁止明显危险命令（如 format、del /f、powershell 高危用法等）。
/// 可配置允许的命令/程序白名单。
/// </summary>
public class WindowsCommandValidator : ICommandValidator
{
    private static readonly string[] DangerousPatterns =
    {
        "format ", "format.", "del /f /s /q", "rd /s /q", "rmdir /s",
        "powershell -e", "powershell -enc", "powershell -encodedcommand",
        "certutil -urlcache", "wmic ", "reg add", "reg delete"
    };

    /// <summary>
    /// 允许的命令/程序白名单（不区分大小写）。若为空则仅做黑名单校验。
    /// 例如 ["cmd","dir","type","echo","where"]。
    /// </summary>
    public IReadOnlySet<string>? AllowedCommands { get; set; }

    public bool Validate(string command)
    {
        if (string.IsNullOrWhiteSpace(command))
            return false;
        var t = command.Trim();
        foreach (var p in DangerousPatterns)
        {
            if (t.Contains(p, StringComparison.OrdinalIgnoreCase))
                return false;
        }
        if (AllowedCommands != null && AllowedCommands.Count > 0)
        {
            var first = t.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
            if (string.IsNullOrEmpty(first))
                return false;
            var name = first.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) ? first : first + ".exe";
            if (!AllowedCommands.Contains(first) && !AllowedCommands.Contains(name))
                return false;
        }
        return true;
    }
}
