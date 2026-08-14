using AgentScope.Core.Message;

namespace AgentScope.Extensions.Channel;

/// <summary>
/// 通信渠道接口。对标 Java Channel。
/// </summary>
public interface IChannel
{
    string Name { get; }
    Task StartAsync(CancellationToken ct = default);
    Task StopAsync(CancellationToken ct = default);
    event Func<InboundMessage, Task>? OnMessageReceived;
    ValueTask SendAsync(Msg message, CancellationToken ct = default);

    /// <summary>
    /// 处理入站 webhook 回调（原始请求体 + 请求头）。默认返回"未实现"。
    /// 由宿主（如 ASP.NET Core Controller）在收到平台回调时调用。
    /// 返回处理结果：验证失败、已去重、或触发的入站消息。
    /// </summary>
    ValueTask<InboundProcessResult> ProcessInboundAsync(
        string rawBody, IReadOnlyDictionary<string, string>? headers = null, CancellationToken ct = default)
        => ValueTask.FromResult(InboundProcessResult.NotSupported);
}

/// <summary>
/// 入站消息（渠道 → 框架）。对标 Java InboundMessage 的简化形态。
/// </summary>
public readonly record struct InboundMessage(
    string From,
    string Text,
    string? ChannelId = null,
    IDictionary<string, object>? Metadata = null);

/// <summary>
/// 入站回调处理结果。
/// </summary>
public readonly record struct InboundProcessResult
{
    /// <summary>是否支持该入站回调（false 表示渠道未实现入站）。</summary>
    public bool Supported { get; init; }

    /// <summary>签名/校验是否通过（false 表示验证失败，应拒绝）。</summary>
    public bool Verified { get; init; }

    /// <summary>是否因幂等去重而被跳过（重复投递）。</summary>
    public bool Duplicate { get; init; }

    /// <summary>实际触发派发的入站消息（去重后为空表示无需派发）。</summary>
    public IReadOnlyList<InboundMessage> Messages { get; init; }

    /// <summary>URL 验证挑战响应（飞书 url_verification 场景返回 challenge）。</summary>
    public string? ChallengeResponse { get; init; }

    public static InboundProcessResult NotSupported => new() { Supported = false };
    public static InboundProcessResult FailedVerification => new() { Supported = true, Verified = false, Messages = [] };
    public static InboundProcessResult SkippedAsDuplicate => new() { Supported = true, Verified = true, Duplicate = true, Messages = [] };
    public static InboundProcessResult Dispatched(IReadOnlyList<InboundMessage> messages) =>
        new() { Supported = true, Verified = true, Messages = messages };
}
