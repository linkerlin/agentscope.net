using System.Collections.Concurrent;
using AgentScope.Core.Message;

namespace AgentScope.Harness.Gateway.Channel;

/// <summary>ChatUI 消息渠道，对�?Java ChatUiChannel</summary>
public sealed class ChatUiChannel : IChannel
{
    public const string ChannelIdConst = "chatui";

    private IGateway? _gateway;
    private ChannelConfig _config;
    private readonly ConcurrentQueue<OutboundEnvelope> _outboundQueue = new();

    public ChatUiChannel(ChannelConfig? config = null)
    {
        _config = config ?? ChannelConfig.Of(ChannelIdConst);
    }

    public string ChannelId => ChannelIdConst;
    public ChannelConfig Config => _config;

    public void Init(IGateway gateway) => _gateway = gateway;

    public Task StartAsync(CancellationToken ct = default) => Task.CompletedTask;
    public Task StopAsync(CancellationToken ct = default) => Task.CompletedTask;

    public async Task<Msg> DispatchAsync(InboundMessage message, CancellationToken ct = default)
    {
        if (_gateway == null)
            throw new InvalidOperationException("ChatUiChannel not initialized with Gateway");
        var msg = message.Messages.FirstOrDefault()
            ?? Msg.Builder().Role("user").TextContent("").Build();
        return await _gateway.RunAsync(msg, ct: ct);
    }

    public void Deliver(OutboundAddress address, IReadOnlyList<Msg> messages)
    {
        _outboundQueue.Enqueue(new OutboundEnvelope(address, messages));
    }

    /// <summary>取出所有缓冲的出站消息</summary>
    public List<OutboundEnvelope> PollOutbound()
    {
        var result = new List<OutboundEnvelope>();
        while (_outboundQueue.TryDequeue(out var env))
            result.Add(env);
        return result;
    }

    public void Send(string text)
    {
        var msg = Msg.Builder().Role("user").TextContent(text).Build();
        _outboundQueue.Enqueue(new OutboundEnvelope(
            OutboundAddress.Direct(ChannelIdConst, "user"), [msg]));
    }
}

/// <summary>出站信封，对�?Java OutboundEnvelope</summary>
public sealed record OutboundEnvelope(
    OutboundAddress Address,
    IReadOnlyList<Msg> Messages,
    long TimestampMs)
{
    public OutboundEnvelope(OutboundAddress address, IReadOnlyList<Msg> messages)
        : this(address, messages, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()) { }
}
