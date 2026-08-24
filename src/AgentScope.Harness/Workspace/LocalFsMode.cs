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

namespace AgentScope.Harness.Workspace;

/// <summary>
/// 本地文件系统路径隔离模式。对标 Java LocalFsMode。
/// </summary>
public enum LocalFsMode
{
    /// <summary>所有路径锚定到根目录，拒绝 .. 遍历</summary>
    Sandboxed,
    /// <summary>绝对路径仅允许在白名单根目录下</summary>
    Rooted,
    /// <summary>绝对路径原样通过（限信任 Agent）</summary>
    Unrestricted
}
