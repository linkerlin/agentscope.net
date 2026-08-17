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
/// 传输层常量：默认超时、内容类型、认证头等。
/// 对应 Java: io.agentscope.core.model.transport.TransportConstants
/// </summary>
public static class TransportConstants
{
    public const string ContentTypeJson = "application/json";
    public const string ContentTypeForm = "application/x-www-form-urlencoded";
    public const string ContentTypeEventStream = "text/event-stream";

    public const string Authorization = "Authorization";
    public const string BearerPrefix = "Bearer ";
    public const string ApiKeyHeader = "api-key";

    public const string UserAgent = "AgentScope.NET/1.0";

    public static readonly System.TimeSpan DefaultConnectTimeout = System.TimeSpan.FromSeconds(30);
    public static readonly System.TimeSpan DefaultReadTimeout = System.TimeSpan.FromSeconds(120);
    public static readonly System.TimeSpan DefaultWebSocketPingInterval = System.TimeSpan.FromSeconds(20);

    /// <summary>SSE 流中的数据行前缀。</summary>
    public const string SseDataPrefix = "data: ";
    public const string SseDoneMarker = "[DONE]";
}
