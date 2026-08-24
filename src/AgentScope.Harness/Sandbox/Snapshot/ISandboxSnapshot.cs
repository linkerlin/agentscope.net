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

namespace AgentScope.Harness.Sandbox.Snapshot;

/// <summary>沙箱快照接口：持久化和恢复沙箱工作区</summary>
public interface ISandboxSnapshot
{
    string Id { get; }
    string Type { get; }
    bool IsPersistenceEnabled { get; }

    /// <summary>持久化工作区流到快照</summary>
    Task PersistAsync(Stream data, CancellationToken ct = default);

    /// <summary>恢复快照到工作区</summary>
    Task<Stream> RestoreAsync(CancellationToken ct = default);

    /// <summary>检查快照是否可恢复</summary>
    bool IsRestorable();
}

/// <summary>快照规范工厂接口</summary>
public interface ISandboxSnapshotSpec
{
    ISandboxSnapshot Build(string snapshotId);
}
