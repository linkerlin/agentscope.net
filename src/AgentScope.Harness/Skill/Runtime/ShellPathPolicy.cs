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

namespace AgentScope.Harness.Skill.Runtime;

/// <summary>Shell 路径策略，根据模式解析 filesRoot，对应 Java ShellPathPolicy</summary>
public sealed class ShellPathPolicy
{
    public Mode ShellMode { get; }

    private ShellPathPolicy(Mode mode) { ShellMode = mode; }

    public static ShellPathPolicy NoShell() => new(Mode.NoShell);
    public static ShellPathPolicy Sandbox() => new(Mode.Sandbox);
    public static ShellPathPolicy SandboxWithPrefix(string workspacePrefix) => new(Mode.Sandbox);
    public static ShellPathPolicy LocalWithShell(string workspaceRoot) => new(Mode.LocalWithShell);

    public string? Resolve(string skillName, string? stageResult)
    {
        return ShellMode switch
        {
            Mode.NoShell => null,
            Mode.Sandbox => $"/workspace/.skills/{skillName}",
            Mode.LocalWithShell => Path.Combine(
                Environment.CurrentDirectory, ".skills", skillName),
            _ => null
        };
    }

    public enum Mode { NoShell, Sandbox, LocalWithShell }
}
