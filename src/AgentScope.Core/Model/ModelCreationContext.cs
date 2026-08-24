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
/// Model creation context that encapsulates environment variables and creation parameters.
/// Used during model instantiation to resolve configuration values from environment variables or parameters.
/// Corresponds to Java: io.agentscope.core.model.ModelCreationContext
/// 模型创建上下文，封装环境变量和创建参数。
/// 在模型实例化期间使用，从环境变量或参数中解析配置值。
/// 对应 Java: io.agentscope.core.model.ModelCreationContext
/// </summary>
public class ModelCreationContext
{
    /// <summary>
    /// Snapshot of environment variables at creation time.
    /// 创建时的环境变量快照。
    /// </summary>
    public IReadOnlyDictionary<string, string?> EnvironmentVariables { get; }

    /// <summary>
    /// Key-value pairs of creation parameters (e.g., API key, base URL, model config).
    /// 创建参数键值对（例如 API 密钥、基础 URL、模型配置）。
    /// </summary>
    public Dictionary<string, object?> Parameters { get; set; } = new();

    /// <summary>
    /// Initializes the context with the current process environment variables.
    /// 使用当前进程环境变量初始化上下文。
    /// </summary>
    public ModelCreationContext()
    {
        var env = new Dictionary<string, string?>();
        foreach (var key in Environment.GetEnvironmentVariables().Keys)
        {
            var k = key?.ToString();
            if (k != null)
            {
                env[k] = Environment.GetEnvironmentVariable(k);
            }
        }
        EnvironmentVariables = env;
    }

    /// <summary>
    /// Initializes the context with a specified environment variable dictionary.
    /// 使用指定环境变量字典初始化上下文。
    /// </summary>
    /// <param name="environmentVariables">Pre-populated environment variables / 预填充的环境变量。</param>
    public ModelCreationContext(Dictionary<string, string?> environmentVariables)
    {
        EnvironmentVariables = environmentVariables;
    }

    /// <summary>
    /// Resolves a configuration value by first checking the environment variable,
    /// then falling back to the parameters dictionary.
    /// 解析配置值：先检查环境变量，然后回退到参数字典。
    /// </summary>
    /// <param name="key">Parameter key / 参数键。</param>
    /// <param name="envVarName">Optional environment variable name to check first / 可选的环境变量名称（优先检查）。</param>
    /// <returns>Resolved value, or null if not found / 解析后的值，如果未找到则返回 null。</returns>
    public string? Resolve(string key, string? envVarName = null)
    {
        if (envVarName != null)
        {
            var envVal = Environment.GetEnvironmentVariable(envVarName);
            if (envVal != null) return envVal;
        }

        return Parameters.TryGetValue(key, out var val) ? val?.ToString() : null;
    }
}
