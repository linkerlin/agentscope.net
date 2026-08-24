using AgentScope.Core.Model;
using AgentScope.Client.Models;

namespace AgentScope.Client.Services;

public interface IProviderFactory
{
    IModel CreateModel(LlmConfig config);
}
