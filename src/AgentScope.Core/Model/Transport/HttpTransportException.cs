// Copyright 2024-2026 the original author or authors.
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
/// HTTP 传输异常。对应 Java: io.agentscope.core.model.transport.HttpTransportException
/// </summary>
public class HttpTransportException : System.Exception
{
    public int? StatusCode { get; }

    public HttpTransportException(string message) : base(message) { }

    public HttpTransportException(string message, System.Exception inner) : base(message, inner) { }

    public HttpTransportException(string message, int statusCode, string? responseBody = null)
        : base(message)
    {
        StatusCode = statusCode;
        ResponseBody = responseBody;
    }

    public string? ResponseBody { get; }
}
