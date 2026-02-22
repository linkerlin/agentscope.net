using System;
using System.Collections.Generic;
using AgentScope.Core.Model;
using AgentScope.Core.Model.Anthropic;
using AgentScope.Core.Model.DeepSeek;
using AgentScope.Core.Model.Gemini;
using AgentScope.Core.Model.DashScope;
using AgentScope.Core.Model.Ollama;
using AgentScope.Core.Model.OpenAI;

namespace AgentScope.Core;

public class ModelFactory
{
    public static IModel Create(string provider, string modelName, string apiKey, string? baseUrl = null)
    {
        return provider.ToLowerInvariant() switch
        {
            "openai" => CreateOpenAI(modelName, apiKey, baseUrl),
            "azure" => CreateAzure(modelName, apiKey, baseUrl),
            "anthropic" => CreateAnthropic(modelName, apiKey, baseUrl),
            "deepseek" => CreateDeepSeek(modelName, apiKey),
            "gemini" => CreateGemini(modelName, apiKey),
            "dashscope" => CreateDashScope(modelName, apiKey),
            "ollama" => CreateOllama(modelName, baseUrl),
            _ => throw new NotSupportedException($"Provider '{provider}' is not supported. Supported providers: openai, azure, anthropic, deepseek, gemini, dashscope, ollama")
        };
    }

    public static IModel Create(Dictionary<string, string> config)
    {
        var provider = config.GetValueOrDefault("provider", "openai");
        var modelName = config.GetValueOrDefault("model", "gpt-4o");
        var apiKey = config.GetValueOrDefault("apiKey", "");
        var baseUrl = config.ContainsKey("baseUrl") ? config["baseUrl"] : null;

        if (string.IsNullOrEmpty(apiKey))
        {
            throw new InvalidOperationException("API key is required");
        }

        return Create(provider, modelName, apiKey, baseUrl);
    }

    private static IModel CreateOpenAI(string modelName, string apiKey, string? baseUrl)
    {
        var builder = OpenAIModel.CreateBuilder()
            .ModelName(modelName)
            .ApiKey(apiKey);
        
        if (!string.IsNullOrEmpty(baseUrl))
        {
            builder.BaseUrl(baseUrl);
        }
        
        return builder.Build();
    }

    private static IModel CreateAzure(string modelName, string apiKey, string? baseUrl)
    {
        if (string.IsNullOrEmpty(baseUrl))
        {
            throw new InvalidOperationException("Azure OpenAI requires a base URL (endpoint)");
        }

        var builder = OpenAIModel.CreateBuilder()
            .ModelName(modelName)
            .ApiKey(apiKey)
            .BaseUrl(baseUrl.TrimEnd('/') + "/openai");
        
        return builder.Build();
    }

    private static IModel CreateAnthropic(string modelName, string apiKey, string? baseUrl)
    {
        return new AnthropicModel(
            modelName: modelName,
            apiKey: apiKey,
            baseUrl: baseUrl);
    }

    private static IModel CreateDeepSeek(string modelName, string apiKey)
    {
        return DeepSeekModel.Builder()
            .ModelName(modelName)
            .ApiKey(apiKey)
            .Build();
    }

    private static IModel CreateGemini(string modelName, string apiKey)
    {
        return GeminiModel.Builder()
            .ModelName(modelName)
            .ApiKey(apiKey)
            .Build();
    }

    private static IModel CreateDashScope(string modelName, string apiKey)
    {
        return new DashScopeModel(
            modelName: modelName,
            apiKey: apiKey);
    }

    private static IModel CreateOllama(string modelName, string? baseUrl)
    {
        var builder = OllamaModel.Builder()
            .ModelName(modelName);
        
        if (!string.IsNullOrEmpty(baseUrl))
        {
            builder.BaseUrl(baseUrl);
        }
        
        return builder.Build();
    }
}

public static class ModelFactoryExtensions
{
    public static bool IsSupportedProvider(string provider)
    {
        var supported = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "openai", "azure", "anthropic", "deepseek", "gemini", "dashscope", "ollama"
        };
        return supported.Contains(provider);
    }

    public static string GetDefaultModel(string provider)
    {
        return provider.ToLowerInvariant() switch
        {
            "openai" => "gpt-4o",
            "azure" => "gpt-4o",
            "anthropic" => "claude-sonnet-4-5-20250929",
            "deepseek" => "deepseek-chat",
            "gemini" => "gemini-2.0-flash-exp",
            "dashscope" => "qwen-turbo",
            "ollama" => "llama3",
            _ => "gpt-4o"
        };
    }

    public static List<string> GetSupportedProviders()
    {
        return new List<string> { "openai", "azure", "anthropic", "deepseek", "gemini", "dashscope", "ollama" };
    }
}
