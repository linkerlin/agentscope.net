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

namespace AgentScope.Harness.Sandbox;

/// <summary>
/// 沙箱客户端创建选项：镜像、资源配额、超时、环境变量等通用配置。
/// 对应 Java: io.agentscope.harness.agent.sandbox.SandboxClientOptions
/// </summary>
public class SandboxClientOptions
{
    /// <summary>基础镜像/模板。</summary>
    public string? Image { get; set; }

    /// <summary>工作目录。</summary>
    public string WorkingDir { get; set; } = "/workspace";

    /// <summary>CPU 核数限制。</summary>
    public double? CpuLimit { get; set; }

    /// <summary>内存上限（MB）。</summary>
    public int? MemoryMb { get; set; }

    /// <summary>磁盘上限（MB）。</summary>
    public int? DiskMb { get; set; }

    /// <summary>单次命令执行超时。</summary>
    public TimeSpan ExecTimeout { get; set; } = TimeSpan.FromMinutes(10);

    /// <summary>沙箱空闲回收时间。</summary>
    public TimeSpan IdleTimeout { get; set; } = TimeSpan.FromMinutes(30);

    /// <summary>环境变量。</summary>
    public Dictionary<string, string> Environment { get; set; } = new();

    /// <summary>是否以 root 运行。</summary>
    public bool RunAsRoot { get; set; } = true;
}
