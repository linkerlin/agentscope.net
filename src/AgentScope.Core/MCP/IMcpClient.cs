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
/// MCP 客户端抽象：初始化、列举工具、调用工具。可由 C# MCP SDK 或自研实现。
/// </summary>
public interface IMcpClient : IDisposable
{
    string Name { get; }
    bool IsInitialized { get; }
    Task InitializeAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<McpToolSchema>> ListToolsAsync(CancellationToken cancellationToken = default);
    Task<McpCallResult> CallToolAsync(string toolName, Dictionary<string, object> args, CancellationToken cancellationToken = default);
}
