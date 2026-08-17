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

using AgentScope.Harness.Filesystem.Local;
using AgentScope.Harness.Workspace;

namespace AgentScope.Harness.Filesystem.Spec;

/// <summary>
/// 本地文件系统规格构建器。对标 Java LocalFilesystemSpec。
/// 构建 OverlayFilesystem：上层 = LocalFilesystemWithShell（R/W），下层 = LocalFilesystem（R/O）。
/// </summary>
public sealed class LocalFilesystemSpec
{
    private string? _workspaceRoot;
    private string? _projectRoot;
    private LocalFsMode _mode = LocalFsMode.Sandboxed;
    private PathPolicy? _policy;
    private bool _projectWritable;

    public LocalFilesystemSpec WithRoot(string workspaceRoot, string? projectRoot = null)
    {
        _workspaceRoot = workspaceRoot;
        _projectRoot = projectRoot;
        return this;
    }

    public LocalFilesystemSpec WithMode(LocalFsMode mode)
    {
        _mode = mode;
        return this;
    }

    public LocalFilesystemSpec WithPolicy(PathPolicy policy)
    {
        _policy = policy;
        return this;
    }

    public LocalFilesystemSpec WithProjectWritable(bool writable = true)
    {
        _projectWritable = writable;
        return this;
    }

    public IFilesystem Build()
    {
        var root = _workspaceRoot ?? Directory.GetCurrentDirectory();
        var policy = _policy ?? PathPolicy.FromWorkspace(root);

        var upper = new LocalFilesystem(_projectWritable && _projectRoot != null ? _projectRoot : root, _mode, policy);
        var lower = new LocalFilesystem(root, LocalFsMode.Sandboxed, policy);

        return new OverlayFilesystem(upper, lower);
    }
}
