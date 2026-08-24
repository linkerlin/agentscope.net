using AgentScope.Core;
using AgentScope.Core.Model;
using AgentScope.Client.Models;

namespace AgentScope.Client.Services;

public class ProviderFactory : IProviderFactory
{
    public IModel CreateModel(LlmConfig config)
    {
        // 优先使用 UI 配置的 API Key，如果为空则回退到环境变量
        var apiKey = config.ApiKey;
        if (string.IsNullOrEmpty(apiKey))
        {
            apiKey = config.Provider.ToLowerInvariant() switch
            {
                "openai" => Environment.GetEnvironmentVariable("OPENAI_API_KEY"),
                "deepseek" => Environment.GetEnvironmentVariable("DEEPSEEK_API_KEY"),
                "anthropic" => Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY"),
                "gemini" => Environment.GetEnvironmentVariable("GEMINI_API_KEY"),
                _ => null
            };
        }

        return ModelFactory.Create(
            config.Provider,
            config.ModelName,
            apiKey ?? string.Empty,
            config.BaseUrl);
    }
}
