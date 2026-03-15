// Copyright 2024-2026 the original author or authors.
// Licensed under the Apache License, Version 2.0

namespace AgentScope.Core.Tool.Coding;

/// <summary>
/// Unix/Linux 命令校验器：禁止明显危险模式（如重定向到敏感路径、sudo、rm -rf / 等）。
/// 可配置允许的命令白名单；未配置时仅做黑名单校验。
/// </summary>
public class UnixCommandValidator : ICommandValidator
{
    private static readonly string[] DangerousPatterns =
    {
        "sudo", "rm -rf /", "rm -rf /*", ":(){ :|:& };:", "mkfs.", "dd if=",
        "> /dev/sd", ">/dev/sd", "chmod 777 ", "wget ", "curl | sh", "curl | bash"
    };

    /// <summary>
    /// 允许的命令白名单（不含参数）。若为空则仅做黑名单校验。
    /// 例如 ["ls","cat","pwd","echo"]。
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
            if (string.IsNullOrEmpty(first) || !AllowedCommands.Contains(first))
                return false;
        }
        return true;
    }
}
