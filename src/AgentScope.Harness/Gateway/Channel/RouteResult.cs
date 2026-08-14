using AgentScope.Core.Message;
namespace AgentScope.Harness.Gateway.Channel;

/// <summary>路由结果，对�?Java RouteResult</summary>
public sealed record RouteResult(
    string AgentId,
    string MatchedBy,
    OutboundAddress OutboundAddress);

