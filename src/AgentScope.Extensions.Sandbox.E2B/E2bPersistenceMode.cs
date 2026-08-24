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

namespace AgentScope.Extensions.Sandbox.E2B;

/// <summary>
/// E2B 工作区字节的持久化方式。对标 Java E2bPersistenceMode。
/// </summary>
public enum E2bPersistenceMode
{
    /// <summary>Tar 归档字节（默认，与其他 Harness 快照兼容）。</summary>
    Tar,

    /// <summary>E2B 原生快照（POST /sandboxes/{id}/snapshots）。</summary>
    NativeSnapshot,
}
