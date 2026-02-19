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
using System.IO;

namespace AgentScope.Core.Configuration;

/// <summary>
/// AgentScope 配置管理器，从 .env 文件加载设置
/// </summary>
public static class ConfigurationManager
{
    private static bool _isLoaded = false;

    /// <summary>
    /// 从 .env 文件加载配置
    /// </summary>
    /// <param name="envFilePath">.env 文件路径。如果为空，则在当前目录和父目录中搜索。</param>
    public static void Load(string? envFilePath = null)
    {
        if (_isLoaded)
        {
            return;
        }

        try
        {
            if (envFilePath == null)
            {
                // 在当前目录和父目录中搜索 .env 文件
                var currentDir = Directory.GetCurrentDirectory();
                var searchPath = currentDir;

                for (int i = 0; i < 5; i++) // 最多向上搜索5层
                {
                    var envPath = Path.Combine(searchPath, ".env");
                    if (File.Exists(envPath))
                    {
                        envFilePath = envPath;
                        break;
                    }

                    var parentDir = Directory.GetParent(searchPath);
                    if (parentDir == null)
                        break;

                    searchPath = parentDir.FullName;
                }
            }

            if (envFilePath != null && File.Exists(envFilePath))
            {
                DotNetEnv.Env.Load(envFilePath);
                _isLoaded = true;
            }
        }
        catch (System.Exception ex)
        {
            Console.WriteLine($"警告：加载 .env 文件失败：{ex.Message}");
        }
    }

    /// <summary>
    /// 获取配置值
    /// </summary>
    public static string? Get(string key, string? defaultValue = null)
    {
        if (!_isLoaded)
        {
            Load();
        }

        return Environment.GetEnvironmentVariable(key) ?? defaultValue;
    }

    /// <summary>
    /// 获取 OpenAI API 密钥
    /// </summary>
    public static string? GetOpenAIApiKey() => Get("OPENAI_API_KEY");

    /// <summary>
    /// 获取 Azure OpenAI API 密钥
    /// </summary>
    public static string? GetAzureOpenAIApiKey() => Get("AZURE_OPENAI_API_KEY");

    /// <summary>
    /// 获取 Azure OpenAI 端点
    /// </summary>
    public static string? GetAzureOpenAIEndpoint() => Get("AZURE_OPENAI_ENDPOINT");

    /// <summary>
    /// 获取 Anthropic API 密钥
    /// </summary>
    public static string? GetAnthropicApiKey() => Get("ANTHROPIC_API_KEY");

    /// <summary>
    /// 获取 DashScope API 密钥
    /// </summary>
    public static string? GetDashScopeApiKey() => Get("DASHSCOPE_API_KEY");

    /// <summary>
    /// 获取数据库路径
    /// </summary>
    public static string GetDatabasePath() => Get("DATABASE_PATH", "agentscope.db")!;

    /// <summary>
    /// 获取日志级别
    /// </summary>
    public static string GetLogLevel() => Get("LOG_LEVEL", "Information")!;

    /// <summary>
    /// 获取最大迭代次数
    /// </summary>
    public static int GetMaxIterations() => int.TryParse(Get("MAX_ITERATIONS"), out var value) ? value : 10;

    /// <summary>
    /// 获取默认模型
    /// </summary>
    public static string GetDefaultModel() => Get("DEFAULT_MODEL", "gpt-3.5-turbo")!;
}
