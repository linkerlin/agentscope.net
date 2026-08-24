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

namespace AgentScope.Core.A2A.Server.Transport;

/// <summary>
/// Transport protocol wrapper. Counterpart to Java TransportWrapper.
/// Supports multiple transports such as JSON-RPC, gRPC, REST, etc.
/// 传输协议包装器。对标 Java TransportWrapper。
/// 支持 JSON-RPC、gRPC、REST 等多种传输。
/// </summary>
public interface ITransportWrapper
{
    /// <summary>
    /// Gets the transport type identifier (e.g. "jsonrpc", "grpc", "rest").
    /// 获取传输类型标识（如 "jsonrpc"、"grpc"、"rest"）。
    /// </summary>
    string TransportType { get; }

    /// <summary>
    /// Handles an incoming transport request and returns a response.
    /// 处理传入的传输请求并返回响应。
    /// </summary>
    /// <param name="body">Raw request body / 原始请求体</param>
    /// <param name="headers">Optional request headers / 可选的请求头</param>
    /// <param name="ct">Cancellation token / 取消令牌</param>
    /// <returns>Response object / 响应对象</returns>
    Task<object> HandleRequestAsync(string body, IDictionary<string, string>? headers = null, CancellationToken ct = default);
}
