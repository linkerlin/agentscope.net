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
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using AgentScope.Core.Formatter.OpenAI.Dto;
using AgentScope.Core.Model.Transport;

namespace AgentScope.Core.Model.OpenAI;

/// <summary>
/// Stateless HTTP client for OpenAI-compatible APIs.
/// OpenAI HTTP 客户端
/// 
/// This client handles communication with OpenAI's Chat Completion API using direct HTTP calls.
/// All configuration (API key, base URL) is passed per-request, making this client stateless.
/// 
/// Java参考: io.agentscope.core.model.OpenAIClient
/// </summary>
public class OpenAIClient
{
    /// <summary>
    /// Default base URL for OpenAI API
    /// </summary>
    public const string DefaultBaseUrl = "https://api.openai.com";

    /// <summary>
    /// Chat completions API endpoint
    /// </summary>
    public const string ChatCompletionsEndpoint = "/v1/chat/completions";

    private readonly IHttpTransport _transport;
    private readonly JsonSerializerOptions _jsonOptions;

    /// <summary>
    /// Create a new OpenAIClient with default transport.
    /// </summary>
    public OpenAIClient()
    {
        _transport = new HttpClientTransport();
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };
    }

    /// <summary>
    /// Create a new OpenAIClient with custom transport.
    /// </summary>
    public OpenAIClient(IHttpTransport transport)
    {
        _transport = transport ?? throw new ArgumentNullException(nameof(transport));
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };
    }

    /// <summary>
    /// Make a synchronous call to OpenAI API.
    /// </summary>
    public async Task<OpenAIResponse> CallAsync(
        string? apiKey,
        string? baseUrl,
        OpenAIRequest request,
        CancellationToken cancellationToken = default)
    {
        var url = BuildUrl(baseUrl, ChatCompletionsEndpoint);
        var json = JsonSerializer.Serialize(request, _jsonOptions);

        var httpRequest = new HttpRequestBuilder()
            .Url(url)
            .Method("POST")
            .Header("Authorization", $"Bearer {GetApiKey(apiKey)}")
            .Header("Content-Type", "application/json")
            .Body(json)
            .Build();

        var response = await _transport.ExecuteAsync(httpRequest, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new ModelException(
                $"OpenAI API error: {response.StatusCode} - {response.Body}");
        }

        var result = JsonSerializer.Deserialize<OpenAIResponse>(response.Body, _jsonOptions);
        if (result == null)
        {
            throw new ModelException("Failed to deserialize OpenAI response");
        }

        return result;
    }

    /// <summary>
    /// Make a streaming call to OpenAI API.
    /// </summary>
    public IAsyncEnumerable<OpenAIResponse> StreamAsync(
        string? apiKey,
        string? baseUrl,
        OpenAIRequest request,
        CancellationToken cancellationToken = default)
    {
        // Cannot yield in try block with catch, so delegate to a separate method
        return StreamAsyncInternal(apiKey, baseUrl, request, cancellationToken);
    }

    private async IAsyncEnumerable<OpenAIResponse> StreamAsyncInternal(
        string? apiKey,
        string? baseUrl,
        OpenAIRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var url = BuildUrl(baseUrl, ChatCompletionsEndpoint);
        var json = JsonSerializer.Serialize(request, _jsonOptions);

        var httpRequest = new HttpRequestBuilder()
            .Url(url)
            .Method("POST")
            .Header("Authorization", $"Bearer {GetApiKey(apiKey)}")
            .Header("Content-Type", "application/json")
            .Header("Accept", "text/event-stream")
            .Body(json)
            .Build();

        await foreach (var line in _transport.StreamAsync(httpRequest, cancellationToken))
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            // Parse SSE format: "data: {...}"
            if (line.StartsWith("data: "))
            {
                var data = line.Substring(6);

                // Check for stream end
                if (data == "[DONE]")
                {
                    yield break;
                }

                OpenAIResponse? chunk = null;
                try
                {
                    chunk = JsonSerializer.Deserialize<OpenAIResponse>(data, _jsonOptions);
                }
                catch (JsonException)
                {
                    // Skip malformed chunks
                    continue;
                }

                if (chunk != null)
                {
                    yield return chunk;
                }
            }
        }
    }

    /// <summary>
    /// Build full URL from base URL and endpoint.
    /// </summary>
    private static string BuildUrl(string? baseUrl, string endpoint)
    {
        var baseUri = string.IsNullOrEmpty(baseUrl) ? DefaultBaseUrl : baseUrl.TrimEnd('/');
        return baseUri + endpoint;
    }

    /// <summary>
    /// Get API key from parameter or environment variable.
    /// </summary>
    private static string GetApiKey(string? apiKey)
    {
        if (!string.IsNullOrEmpty(apiKey))
        {
            return apiKey;
        }

        var envKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
        if (!string.IsNullOrEmpty(envKey))
        {
            return envKey;
        }

        throw new ModelException(
            "OpenAI API key not found. Please set OPENAI_API_KEY environment variable or provide apiKey parameter.");
    }
}
