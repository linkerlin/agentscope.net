using AgentScope.Core;
using AgentScope.Core.Model;
using AgentScope.Client.Models;

namespace AgentScope.Client.Services;

public class ProviderFactory : IProviderFactory
{
    public IModel CreateModel(LlmConfig config)
    {
        return ModelFactory.Create(
            config.Provider,
            config.ModelName,
            config.ApiKey ?? string.Empty,
            config.BaseUrl);
    }
}
