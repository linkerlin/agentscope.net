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
using System.Threading;
using System.Threading.Tasks;

namespace AgentScope.Core.Model.Transport;

/// <summary>
/// HTTP transport layer interface for making HTTP requests.
/// HTTP 传输层接口
/// 
/// This interface abstracts the actual HTTP client implementation.
/// Java参考: io.agentscope.core.model.transport.HttpTransport
/// </summary>
public interface IHttpTransport
{
    /// <summary>
    /// Execute a synchronous HTTP request.
    /// </summary>
    /// <param name="request">The HTTP request to execute</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The HTTP response</returns>
    Task<HttpResponse> ExecuteAsync(HttpRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Execute a streaming HTTP request (Server-Sent Events).
    /// </summary>
    /// <param name="request">The HTTP request to execute</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Async enumerable of SSE data lines</returns>
    IAsyncEnumerable<string> StreamAsync(HttpRequest request, CancellationToken cancellationToken = default);
}
