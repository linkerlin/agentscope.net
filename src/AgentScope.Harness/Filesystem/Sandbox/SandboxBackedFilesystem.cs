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

using SandboxBase = AgentScope.Harness.Sandbox.SandboxBase;
using SBExecResult = AgentScope.Harness.Sandbox.ExecResult;

namespace AgentScope.Harness.Filesystem.Sandbox;

/// <summary>
/// 沙箱代理文件系统。将对 IFilesystem 的操作转发到活跃的 Sandbox.exec()。
/// 对标 Java SandboxBackedFilesystem。
/// </summary>
public sealed class SandboxBackedFilesystem(
    SandboxBase sandbox,
    string id) : SandboxFilesystemBase
{
    public override string Id => id;

    public override async Task<SBExecResult> ExecuteAsync(string command, int? timeout = null,
        CancellationToken ct = default)
    {
        var result = await sandbox.ExecAsync(command, timeout, ct);
        return new SBExecResult(result.ExitCode, result.StdOut, result.StdErr, result.Truncated);
    }
}
