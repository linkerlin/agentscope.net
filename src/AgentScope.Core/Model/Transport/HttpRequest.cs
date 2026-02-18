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

using System;
using System.Collections.Generic;

namespace AgentScope.Core.Model.Transport;

/// <summary>
/// HTTP request encapsulation for the transport layer.
/// HTTP 请求封装
/// 
/// Java参考: io.agentscope.core.model.transport.HttpRequest
/// </summary>
public class HttpRequest
{
    /// <summary>
    /// Request URL
    /// </summary>
    public required string Url { get; init; }

    /// <summary>
    /// HTTP method (GET, POST, etc.)
    /// </summary>
    public required string Method { get; init; }

    /// <summary>
    /// Request headers
    /// </summary>
    public Dictionary<string, string> Headers { get; init; } = new();

    /// <summary>
    /// Request body
    /// </summary>
    public string? Body { get; init; }

    /// <summary>
    /// Request timeout
    /// </summary>
    public TimeSpan? Timeout { get; init; }
}

/// <summary>
/// Builder for HttpRequest
/// </summary>
public class HttpRequestBuilder
{
    private string? _url;
    private string _method = "GET";
    private readonly Dictionary<string, string> _headers = new();
    private string? _body;
    private TimeSpan? _timeout;

    public HttpRequestBuilder Url(string url)
    {
        _url = url;
        return this;
    }

    public HttpRequestBuilder Method(string method)
    {
        _method = method;
        return this;
    }

    public HttpRequestBuilder Header(string name, string value)
    {
        _headers[name] = value;
        return this;
    }

    public HttpRequestBuilder Headers(Dictionary<string, string> headers)
    {
        foreach (var kvp in headers)
        {
            _headers[kvp.Key] = kvp.Value;
        }
        return this;
    }

    public HttpRequestBuilder Body(string body)
    {
        _body = body;
        return this;
    }

    public HttpRequestBuilder Timeout(TimeSpan timeout)
    {
        _timeout = timeout;
        return this;
    }

    public HttpRequest Build()
    {
        if (string.IsNullOrEmpty(_url))
        {
            throw new ArgumentException("URL is required");
        }

        return new HttpRequest
        {
            Url = _url,
            Method = _method,
            Headers = new Dictionary<string, string>(_headers),
            Body = _body,
            Timeout = _timeout
        };
    }
}
