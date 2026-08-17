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

namespace AgentScope.Core.Model.Transport;

/// <summary>
/// Enumeration of supported HTTP protocol versions for the transport layer.
/// Corresponds to Java: io.agentscope.core.model.transport.HttpVersion
/// 传输层支持的 HTTP 协议版本枚举。
/// 对应 Java: io.agentscope.core.model.transport.HttpVersion
/// </summary>
public enum HttpVersion
{
    /// <summary>HTTP/1.0 - Legacy version, rarely used / 旧版，很少使用。</summary>
    Http10,

    /// <summary>HTTP/1.1 - Most common version, default for most applications / 最常用版本，大多数应用的默认值。</summary>
    Http11,

    /// <summary>HTTP/2 - Multiplexed, binary protocol for better performance / 多路复用二进制协议，性能更好。</summary>
    Http2,

    /// <summary>HTTP/3 - QUIC-based protocol for reduced latency / 基于 QUIC 的协议，延迟更低。</summary>
    Http3
}
