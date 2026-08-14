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
/// 模型创建上下文，封装环境变量和创建参数。
/// 对标 Java ModelCreationContext。
/// </summary>
public class ModelCreationContext
{
    /// <summary>环境变量快照</summary>
    public IReadOnlyDictionary<string, string?> EnvironmentVariables { get; }

    /// <summary>创建参数键值对</summary>
    public Dictionary<string, object?> Parameters { get; set; } = new();

    /// <summary>
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
    /// 使用指定环境变量字典初始化上下文。
    /// </summary>
    public ModelCreationContext(Dictionary<string, string?> environmentVariables)
    {
        EnvironmentVariables = environmentVariables;
    }

    /// <summary>
    /// 从环境变量中读取值，支持回退到参数。
    /// </summary>
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
