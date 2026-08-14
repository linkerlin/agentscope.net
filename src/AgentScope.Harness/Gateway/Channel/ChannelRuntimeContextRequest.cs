using AgentScope.Core.Message;
namespace AgentScope.Harness.Gateway.Channel;

/// <summary>渠道运行时上下文请求，对�?Java ChannelRuntimeContextRequest</summary>
public sealed record ChannelRuntimeContextRequest(
    string ChannelId,
    InboundMessage InboundMessage,
    RouteResult RouteResult);

