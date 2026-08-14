namespace AgentScope.Harness.Subagent.Tasks;

/// <summary>远程目标，对�?Java RemoteTarget</summary>
public sealed record RemoteTarget(string BaseUrl, Dictionary<string, string>? Headers = null);

