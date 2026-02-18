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
using System.IO;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AgentScope.Core.Model.Transport;

/// <summary>
/// HTTP transport implementation using .NET HttpClient.
/// 使用 .NET HttpClient 的 HTTP 传输实现
/// </summary>
public class HttpClientTransport : IHttpTransport, IDisposable
{
    private readonly HttpClient _httpClient;
    private bool _disposed;

    /// <summary>
    /// Create a new HttpClientTransport with default HttpClient.
    /// </summary>
    public HttpClientTransport()
    {
        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "AgentScope.NET/1.0");
    }

    /// <summary>
    /// Create a new HttpClientTransport with custom HttpClient.
    /// </summary>
    public HttpClientTransport(HttpClient httpClient)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    }

    /// <inheritdoc />
    public async Task<HttpResponse> ExecuteAsync(HttpRequest request, CancellationToken cancellationToken = default)
    {
        using var httpRequest = new HttpRequestMessage(
            new HttpMethod(request.Method),
            request.Url);

        // Add headers
        foreach (var header in request.Headers)
        {
            httpRequest.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        // Add body if present
        if (!string.IsNullOrEmpty(request.Body))
        {
            httpRequest.Content = new StringContent(request.Body, Encoding.UTF8, "application/json");
        }

        // Set timeout if specified
        var timeout = request.Timeout ?? TimeSpan.FromSeconds(60);
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(timeout);

        try
        {
            using var httpResponse = await _httpClient.SendAsync(
                httpRequest,
                HttpCompletionOption.ResponseHeadersRead,
                cts.Token);

            var body = await httpResponse.Content.ReadAsStringAsync(cancellationToken);

            var headers = new Dictionary<string, string>();
            foreach (var header in httpResponse.Headers)
            {
                headers[header.Key] = string.Join(",", header.Value);
            }

            return new HttpResponse
            {
                StatusCode = (int)httpResponse.StatusCode,
                Headers = headers,
                Body = body
            };
        }
        catch (OperationCanceledException) when (cts.Token.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
        {
            throw new TimeoutException($"Request to {request.Url} timed out after {timeout}");
        }
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<string> StreamAsync(
        HttpRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        using var httpRequest = new HttpRequestMessage(
            new HttpMethod(request.Method),
            request.Url);

        // Add headers
        foreach (var header in request.Headers)
        {
            httpRequest.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        // Add body if present
        if (!string.IsNullOrEmpty(request.Body))
        {
            httpRequest.Content = new StringContent(request.Body, Encoding.UTF8, "application/json");
        }

        // Set timeout if specified
        var timeout = request.Timeout ?? TimeSpan.FromSeconds(60);
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(timeout);

        HttpResponseMessage? httpResponse = null;
        try
        {
            httpResponse = await _httpClient.SendAsync(
                httpRequest,
                HttpCompletionOption.ResponseHeadersRead,
                cts.Token);

            httpResponse.EnsureSuccessStatusCode();

            var stream = await httpResponse.Content.ReadAsStreamAsync(cancellationToken);
            using var reader = new StreamReader(stream, Encoding.UTF8);

            while (!reader.EndOfStream && !cts.Token.IsCancellationRequested)
            {
                var line = await reader.ReadLineAsync(cancellationToken);
                if (!string.IsNullOrEmpty(line))
                {
                    yield return line;
                }
            }
        }
        finally
        {
            httpResponse?.Dispose();
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (!_disposed)
        {
            _httpClient.Dispose();
            _disposed = true;
        }
    }
}
