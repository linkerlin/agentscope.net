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

namespace AgentScope.Core.MCP;

/// <summary>
/// MCP 客户端包装器抽象基类，与 Java McpClientWrapper 对应。具体实现可委托给 C# MCP SDK。
/// </summary>
public abstract class McpClientWrapper : IMcpClient
{
    public abstract string Name { get; }
    public virtual bool IsInitialized { get; protected set; }

    public abstract Task InitializeAsync(CancellationToken cancellationToken = default);
    public abstract Task<IReadOnlyList<McpToolSchema>> ListToolsAsync(CancellationToken cancellationToken = default);
    public abstract Task<McpCallResult> CallToolAsync(string toolName, Dictionary<string, object> args, CancellationToken cancellationToken = default);

    public virtual void Dispose() => GC.SuppressFinalize(this);
}
