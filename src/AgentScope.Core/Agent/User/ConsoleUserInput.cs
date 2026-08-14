// Copyright 2024-2026 the original author or authors.
// Licensed under the Apache License, Version 2.0

using System;
using System.Threading.Tasks;

namespace AgentScope.Core.Agent.User;

/// <summary>
/// 控制台用户输入实现
/// </summary>
public class ConsoleUserInput : IUserInput
{
    public Task<string> RequestAsync(string prompt)
    {
        Console.Write(prompt);
        return Task.FromResult(Console.ReadLine() ?? "");
    }
}
