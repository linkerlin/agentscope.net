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
/// Exception thrown when a model operation fails.
/// 模型操作异常
/// 
/// Java参考: io.agentscope.core.model.ModelException
/// </summary>
public class ModelException : System.Exception
{
    /// <summary>
    /// Model name that caused the exception
    /// </summary>
    public string? ModelName { get; }

    /// <summary>
    /// Provider name (e.g., "openai", "anthropic")
    /// </summary>
    public string? Provider { get; }

    public ModelException(string message) : base(message)
    {
    }

    public ModelException(string message, System.Exception innerException) 
        : base(message, innerException)
    {
    }

    public ModelException(string message, System.Exception innerException, string modelName, string provider) 
        : base(message, innerException)
    {
        ModelName = modelName;
        Provider = provider;
    }
}
