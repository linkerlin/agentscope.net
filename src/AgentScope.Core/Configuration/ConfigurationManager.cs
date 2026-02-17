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
/// Configuration manager for AgentScope that loads settings from .env file
/// </summary>
public static class ConfigurationManager
{
    private static bool _isLoaded = false;

    /// <summary>
    /// Load configuration from .env file
    /// </summary>
    /// <param name="envFilePath">Path to .env file. If null, searches in current and parent directories.</param>
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
                // Search for .env file in current and parent directories
                var currentDir = Directory.GetCurrentDirectory();
                var searchPath = currentDir;

                for (int i = 0; i < 5; i++) // Search up to 5 levels
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
            Console.WriteLine($"Warning: Failed to load .env file: {ex.Message}");
        }
    }

    /// <summary>
    /// Get configuration value
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
    /// Get OpenAI API key
    /// </summary>
    public static string? GetOpenAIApiKey() => Get("OPENAI_API_KEY");

    /// <summary>
    /// Get Azure OpenAI API key
    /// </summary>
    public static string? GetAzureOpenAIApiKey() => Get("AZURE_OPENAI_API_KEY");

    /// <summary>
    /// Get Azure OpenAI endpoint
    /// </summary>
    public static string? GetAzureOpenAIEndpoint() => Get("AZURE_OPENAI_ENDPOINT");

    /// <summary>
    /// Get Anthropic API key
    /// </summary>
    public static string? GetAnthropicApiKey() => Get("ANTHROPIC_API_KEY");

    /// <summary>
    /// Get DashScope API key
    /// </summary>
    public static string? GetDashScopeApiKey() => Get("DASHSCOPE_API_KEY");

    /// <summary>
    /// Get database path
    /// </summary>
    public static string GetDatabasePath() => Get("DATABASE_PATH", "agentscope.db")!;

    /// <summary>
    /// Get log level
    /// </summary>
    public static string GetLogLevel() => Get("LOG_LEVEL", "Information")!;

    /// <summary>
    /// Get max iterations
    /// </summary>
    public static int GetMaxIterations() => int.TryParse(Get("MAX_ITERATIONS"), out var value) ? value : 10;

    /// <summary>
    /// Get default model
    /// </summary>
    public static string GetDefaultModel() => Get("DEFAULT_MODEL", "gpt-3.5-turbo")!;
}
