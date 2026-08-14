using AgentScope.Core.Message;
namespace AgentScope.Harness.Gateway.Channel;

/// <summary>消息渠道接口，对�?Java Channel</summary>
public interface IChannel
{
    string ChannelId { get; }
    ChannelConfig Config { get; }

    void Init(IGateway gateway);
    Task StartAsync(CancellationToken ct = default);
    Task StopAsync(CancellationToken ct = default);

    Task<Msg> DispatchAsync(InboundMessage message, CancellationToken ct = default);
    void Deliver(OutboundAddress address, IReadOnlyList<Msg> messages);
}

