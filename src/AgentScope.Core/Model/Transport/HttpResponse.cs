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

using System.Collections.Generic;

namespace AgentScope.Core.Model.Transport;

/// <summary>
/// HTTP response encapsulation for the transport layer.
/// HTTP 响应封装
/// 
/// Java参考: io.agentscope.core.model.transport.HttpResponse
/// </summary>
public class HttpResponse
{
    /// <summary>
    /// HTTP status code
    /// </summary>
    public required int StatusCode { get; init; }

    /// <summary>
    /// Response headers
    /// </summary>
    public Dictionary<string, string> Headers { get; init; } = new();

    /// <summary>
    /// Response body
    /// </summary>
    public required string Body { get; init; }

    /// <summary>
    /// Whether the response is successful (2xx status code)
    /// </summary>
    public bool IsSuccessStatusCode => StatusCode >= 200 && StatusCode < 300;
}
