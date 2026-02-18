// Copyright 2024-2026 the original author or authors.
// Licensed under the Apache License, Version 2.0

using System.Diagnostics;
using System.Text;

namespace AgentScope.Core.Tool;

/// <summary>
/// Code execution result
/// 代码执行结果
/// </summary>
public class CodeExecutionResult
{
    /// <summary>
    /// Whether execution was successful
    /// 执行是否成功
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// Standard output
    /// 标准输出
    /// </summary>
    public string StdOut { get; init; } = string.Empty;

    /// <summary>
    /// Standard error
    /// 标准错误
    /// </summary>
    public string StdErr { get; init; } = string.Empty;

    /// <summary>
    /// Exit code
    /// 退出代码
    /// </summary>
    public int ExitCode { get; init; }

    /// <summary>
    /// Execution duration
    /// 执行持续时间
    /// </summary>
    public TimeSpan Duration { get; init; }

    /// <summary>
    /// Exception if execution failed
    /// 执行失败时的异常
    /// </summary>
    public global::System.Exception? Exception { get; init; }
}

/// <summary>
/// Supported programming languages
/// 支持的编程语言
/// </summary>
public enum CodeLanguage
{
    Python,
    JavaScript,
    TypeScript,
    Bash,
    PowerShell,
    CSharp,
    Java,
    Go,
    Rust,
    Ruby,
    PHP
}

/// <summary>
/// Tool for executing code in various languages
/// 用于执行各种语言代码的工具
/// 
/// 参考: agentscope-java 的工具概念
/// </summary>
public class CodeExecutionTool : ToolBase
{
    private readonly Dictionary<CodeLanguage, LanguageConfig> _languageConfigs = new();

    /// <summary>
    /// Maximum execution time
    /// 最大执行时间
    /// </summary>
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Whether to allow network access
    /// 是否允许网络访问
    /// </summary>
    public bool AllowNetwork { get; set; } = false;

    /// <summary>
    /// Maximum output length
    /// 最大输出长度
    /// </summary>
    public int MaxOutputLength { get; set; } = 10000;

    /// <summary>
    /// Working directory for execution
    /// 执行的工作目录
    /// </summary>
    public string? WorkingDirectory { get; set; }

    /// <summary>
    /// Creates a new code execution tool
    /// 创建新代码执行工具
    /// </summary>
    public CodeExecutionTool() 
        : base("code_execution", "Execute code in various programming languages. Supports Python, JavaScript, Bash, and more.")
    {
        InitializeLanguageConfigs();
    }

    /// <summary>
    /// Execute the tool
    /// 执行工具
    /// </summary>
    public override async Task<ToolResult> ExecuteAsync(Dictionary<string, object> parameters)
    {
        try
        {
            if (!parameters.TryGetValue("code", out var codeObj) || codeObj is not string code)
            {
                return ToolResult.Fail("Missing required parameter: code");
            }

            var languageStr = parameters.GetValueOrDefault("language", "python")?.ToString() ?? "python";
            if (!Enum.TryParse<CodeLanguage>(languageStr, true, out var language))
            {
                return ToolResult.Fail($"Unsupported language: {languageStr}");
            }

            var result = await ExecuteCodeAsync(code, language);

            var output = FormatResult(result);
            var metadata = new Dictionary<string, object>
            {
                ["language"] = language.ToString(),
                ["exit_code"] = result.ExitCode,
                ["duration_ms"] = result.Duration.TotalMilliseconds,
                ["success"] = result.Success
            };

            var success = result.Success && result.ExitCode == 0;
        var toolResult = new ToolResult 
        { 
            Success = success, 
            Result = output 
        };
        return toolResult;
        }
        catch (global::System.Exception ex)
        {
            return ToolResult.Fail($"Code execution failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Execute code in specified language
    /// 执行指定语言的代码
    /// </summary>
    public virtual async Task<CodeExecutionResult> ExecuteCodeAsync(string code, CodeLanguage language)
    {
        if (!_languageConfigs.TryGetValue(language, out var config))
        {
            return new CodeExecutionResult
            {
                Success = false,
                StdErr = $"Language {language} is not supported",
                ExitCode = -1
            };
        }

        if (!config.IsAvailable)
        {
            return new CodeExecutionResult
            {
                Success = false,
                StdErr = $"{config.Name} is not installed or not available",
                ExitCode = -1
            };
        }

        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            var result = await ExecuteProcessAsync(config, code);
            stopwatch.Stop();
            
            return new CodeExecutionResult
            {
                Success = result.Success,
                StdOut = result.StdOut,
                StdErr = result.StdErr,
                ExitCode = result.ExitCode,
                Duration = stopwatch.Elapsed,
                Exception = result.Exception
            };
        }
        catch (global::System.Exception ex)
        {
            stopwatch.Stop();
            return new CodeExecutionResult
            {
                Success = false,
                StdErr = ex.Message,
                Exception = ex,
                ExitCode = -1,
                Duration = stopwatch.Elapsed
            };
        }
    }

    /// <summary>
    /// Execute code using external process
    /// 使用外部进程执行代码
    /// </summary>
    private async Task<CodeExecutionResult> ExecuteProcessAsync(LanguageConfig config, string code)
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"agentscope_{Guid.NewGuid()}{config.FileExtension}");
        await File.WriteAllTextAsync(tempFile, code);

        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = config.Command,
                Arguments = string.Format(config.Arguments, tempFile),
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = WorkingDirectory ?? Path.GetTempPath()
            };

            using var process = new Process { StartInfo = psi };
            var stdoutBuilder = new StringBuilder();
            var stderrBuilder = new StringBuilder();

            process.OutputDataReceived += (_, e) =>
            {
                if (e.Data != null) stdoutBuilder.AppendLine(e.Data);
            };

            process.ErrorDataReceived += (_, e) =>
            {
                if (e.Data != null) stderrBuilder.AppendLine(e.Data);
            };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            using var cts = new CancellationTokenSource(Timeout);
            await Task.Run(() =>
            {
                process.WaitForExit();
            }, cts.Token);

            if (!process.HasExited)
            {
                try
                {
                    process.Kill();
                }
                catch { }

                return new CodeExecutionResult
                {
                    Success = false,
                    StdOut = TruncateOutput(stdoutBuilder.ToString()),
                    StdErr = "Execution timed out" + Environment.NewLine + TruncateOutput(stderrBuilder.ToString()),
                    ExitCode = -1
                };
            }

            return new CodeExecutionResult
            {
                Success = true,
                StdOut = TruncateOutput(stdoutBuilder.ToString()),
                StdErr = TruncateOutput(stderrBuilder.ToString()),
                ExitCode = process.ExitCode
            };
        }
        finally
        {
            try
            {
                File.Delete(tempFile);
            }
            catch { }
        }
    }

    /// <summary>
    /// Format execution result for display
    /// 格式化执行结果用于显示
    /// </summary>
    private string FormatResult(CodeExecutionResult result)
    {
        var sb = new StringBuilder();

        sb.AppendLine($"Exit Code: {result.ExitCode}");
        sb.AppendLine($"Duration: {result.Duration.TotalMilliseconds:F0}ms");
        
        if (!string.IsNullOrEmpty(result.StdOut))
        {
            sb.AppendLine();
            sb.AppendLine("=== Output ===");
            sb.AppendLine(result.StdOut);
        }

        if (!string.IsNullOrEmpty(result.StdErr))
        {
            sb.AppendLine();
            sb.AppendLine("=== Error ===");
            sb.AppendLine(result.StdErr);
        }

        return sb.ToString().Trim();
    }

    /// <summary>
    /// Truncate output if too long
    /// 如果输出太长则截断
    /// </summary>
    private string TruncateOutput(string output)
    {
        if (output.Length <= MaxOutputLength) return output;
        
        return output[..MaxOutputLength] + 
            $"\n... (truncated, {output.Length - MaxOutputLength} more characters)";
    }

    /// <summary>
    /// Initialize language configurations
    /// 初始化语言配置
    /// </summary>
    private void InitializeLanguageConfigs()
    {
        // Python
        _languageConfigs[CodeLanguage.Python] = new LanguageConfig
        {
            Name = "Python",
            Command = "python",
            Arguments = "\"{0}\"",
            FileExtension = ".py",
            CheckCommand = "python --version"
        };

        // JavaScript/Node.js
        _languageConfigs[CodeLanguage.JavaScript] = new LanguageConfig
        {
            Name = "Node.js",
            Command = "node",
            Arguments = "\"{0}\"",
            FileExtension = ".js",
            CheckCommand = "node --version"
        };

        // Bash
        _languageConfigs[CodeLanguage.Bash] = new LanguageConfig
        {
            Name = "Bash",
            Command = OperatingSystem.IsWindows() ? "bash" : "/bin/bash",
            Arguments = OperatingSystem.IsWindows() ? "\"{0}\"" : "-c 'source \"{0}\"'",
            FileExtension = ".sh",
            CheckCommand = OperatingSystem.IsWindows() ? "bash --version" : "/bin/bash --version"
        };

        // PowerShell
        _languageConfigs[CodeLanguage.PowerShell] = new LanguageConfig
        {
            Name = "PowerShell",
            Command = OperatingSystem.IsWindows() ? "powershell" : "pwsh",
            Arguments = "-File \"{0}\"",
            FileExtension = ".ps1",
            CheckCommand = OperatingSystem.IsWindows() ? "powershell -Command \"$PSVersionTable.PSVersion\"" : "pwsh --version"
        };

        // C# (using dotnet run)
        _languageConfigs[CodeLanguage.CSharp] = new LanguageConfig
        {
            Name = "C#",
            Command = "dotnet",
            Arguments = "run --project \"{0}\"",
            FileExtension = ".csproj",
            CheckCommand = "dotnet --version"
        };
    }

    /// <summary>
    /// Language configuration
    /// 语言配置
    /// </summary>
    private class LanguageConfig
    {
        public required string Name { get; init; }
        public required string Command { get; init; }
        public required string Arguments { get; init; }
        public required string FileExtension { get; init; }
        public required string CheckCommand { get; init; }

        private bool? _isAvailable;

        public bool IsAvailable
        {
            get
            {
                if (_isAvailable.HasValue) return _isAvailable.Value;

                try
                {
                    var psi = new ProcessStartInfo
                    {
                        FileName = CheckCommand.Split(' ')[0],
                        Arguments = string.Join(' ', CheckCommand.Split(' ').Skip(1)),
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false
                    };

                    using var process = Process.Start(psi);
                    process?.WaitForExit(5000);
                    _isAvailable = process?.ExitCode == 0;
                }
                catch
                {
                    _isAvailable = false;
                }

                return _isAvailable.Value;
            }
        }
    }

    /// <summary>
    /// Get tool schema for LLM
    /// 获取 LLM 工具模式
    /// </summary>
    public override Dictionary<string, object> GetSchema()
    {
        return new Dictionary<string, object>
        {
            ["name"] = Name,
            ["description"] = Description,
            ["parameters"] = new Dictionary<string, object>
            {
                ["code"] = new Dictionary<string, object>
                {
                    ["type"] = "string",
                    ["description"] = "The code to execute",
                    ["required"] = true
                },
                ["language"] = new Dictionary<string, object>
                {
                    ["type"] = "string",
                    ["description"] = "Programming language (python, javascript, bash, powershell, csharp)",
                    ["required"] = false
                }
            }
        };
    }
}

/// <summary>
/// Safe code execution tool with restrictions
/// 带限制的安全代码执行工具
/// </summary>
public class SafeCodeExecutionTool : CodeExecutionTool
{
    /// <summary>
    /// Allowed commands/modules (empty = allow all)
    /// 允许的命令/模块（空 = 允许所有）
    /// </summary>
    public List<string> AllowedModules { get; set; } = new();

    /// <summary>
    /// Blocked commands/modules
    /// 阻止的命令/模块
    /// </summary>
    public List<string> BlockedModules { get; set; } = new()
    {
        "os.system", "subprocess", "eval(", "exec(", 
        "__import__", "import os", "import sys",
        "rm -rf", "del /", "format(", "shutdown"
    };

    /// <summary>
    /// Execute code with safety checks
    /// 带安全检查执行代码
    /// </summary>
    public override async Task<CodeExecutionResult> ExecuteCodeAsync(string code, CodeLanguage language)
    {
        // Check for blocked patterns
        foreach (var blocked in BlockedModules)
        {
            if (code.Contains(blocked, StringComparison.OrdinalIgnoreCase))
            {
                return new CodeExecutionResult
                {
                    Success = false,
                    StdErr = $"Security error: Blocked pattern '{blocked}' found in code",
                    ExitCode = -1
                };
            }
        }

        return await base.ExecuteCodeAsync(code, language);
    }
}
