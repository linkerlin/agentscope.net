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

namespace AgentScope.Core.Model;

/// <summary>
/// 模型 HTTP 异常细分：携带 HTTP 状态码与（可能的）响应体。
/// 对应 Java: io.agentscope.core.model.ModelHttpException
/// </summary>
public class ModelHttpException : ModelException
{
    /// <summary>HTTP 状态码（0 表示未知/传输层错误）。</summary>
    public int StatusCode { get; }

    /// <summary>原始响应体（可能为空）。</summary>
    public string? ResponseBody { get; }

    /// <summary>是否可重试（5xx / 429）。</summary>
    public bool Retryable => ModelUtils.IsRetryableStatus(StatusCode);

    public ModelHttpException(string message, int statusCode, string? responseBody = null)
        : base(message)
    {
        StatusCode = statusCode;
        ResponseBody = responseBody;
    }

    public ModelHttpException(string message, int statusCode, System.Exception inner, string? responseBody = null)
        : base(message, inner)
    {
        StatusCode = statusCode;
        ResponseBody = responseBody;
    }

    public ModelHttpException(string message, int statusCode, System.Exception inner,
        string modelName, string provider, string? responseBody = null)
        : base(message, inner, modelName, provider)
    {
        StatusCode = statusCode;
        ResponseBody = responseBody;
    }
}
