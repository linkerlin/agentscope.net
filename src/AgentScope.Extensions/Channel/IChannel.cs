// Copyright 2024-2026 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using AgentScope.Core.Message;

namespace AgentScope.Extensions.Channel;

/// <summary>
/// Communication channel interface. Maps to the Java Channel interface.
/// 通信渠道接口。对标 Java Channel。
/// </summary>
public interface IChannel
{
    /// <summary>
    /// Gets the name of the channel.
    /// 获取渠道名称。
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Starts the channel, allowing it to receive messages.
    /// 启动渠道，使其开始接收消息。
    /// </summary>
    /// <param name="ct">Cancellation token. 取消令牌。</param>
    Task StartAsync(CancellationToken ct = default);

    /// <summary>
    /// Stops the channel gracefully.
    /// 优雅地停止渠道。
    /// </summary>
    /// <param name="ct">Cancellation token. 取消令牌。</param>
    Task StopAsync(CancellationToken ct = default);

    /// <summary>
    /// Event raised when an inbound message is received from the platform.
    /// 当从平台接收到入站消息时触发的事件。
    /// </summary>
    event Func<InboundMessage, Task>? OnMessageReceived;

    /// <summary>
    /// Sends a message through the channel to the platform.
    /// 通过渠道向平台发送消息。
    /// </summary>
    /// <param name="message">The message to send. 要发送的消息。</param>
    /// <param name="ct">Cancellation token. 取消令牌。</param>
    ValueTask SendAsync(Msg message, CancellationToken ct = default);

    /// <summary>
    /// Processes an inbound webhook callback (raw request body + headers).
    /// Default returns "NotSupported".
    /// Called by the host (e.g. ASP.NET Core Controller) when a platform callback arrives.
    /// Returns the processing result: verification failure, duplicate, or triggered inbound message.
    /// 处理入站 webhook 回调（原始请求体 + 请求头）。默认返回"未实现"。
    /// 由宿主（如 ASP.NET Core Controller）在收到平台回调时调用。
    /// 返回处理结果：验证失败、已去重、或触发的入站消息。
    /// </summary>
    /// <param name="rawBody">The raw HTTP request body. 原始 HTTP 请求体。</param>
    /// <param name="headers">Request headers. 请求头。</param>
    /// <param name="ct">Cancellation token. 取消令牌。</param>
    /// <returns>Processing result indicating outcome. 表示处理结果。</returns>
    ValueTask<InboundProcessResult> ProcessInboundAsync(
        string rawBody, IReadOnlyDictionary<string, string>? headers = null, CancellationToken ct = default)
        => ValueTask.FromResult(InboundProcessResult.NotSupported);
}

/// <summary>
/// Inbound message (channel → framework). Simplified version of Java InboundMessage.
/// 入站消息（渠道 → 框架）。对标 Java InboundMessage 的简化形态。
/// </summary>
/// <param name="From">The sender identifier. 发送者标识。</param>
/// <param name="Text">The message text content. 消息文本内容。</param>
/// <param name="ChannelId">Optional channel identifier. 可选的渠道标识。</param>
/// <param name="Metadata">Optional metadata dictionary. 可选的元数据字典。</param>
public readonly record struct InboundMessage(
    string From,
    string Text,
    string? ChannelId = null,
    IDictionary<string, object>? Metadata = null);

/// <summary>
/// Result of processing an inbound callback.
/// 入站回调处理结果。
/// </summary>
public readonly record struct InboundProcessResult
{
    /// <summary>Whether the channel supports this inbound callback (false means not implemented).</summary>
    /// <summary>是否支持该入站回调（false 表示渠道未实现入站）。</summary>
    public bool Supported { get; init; }

    /// <summary>Whether signature/validation passed (false means verification failed, should reject).</summary>
    /// <summary>签名/校验是否通过（false 表示验证失败，应拒绝）。</summary>
    public bool Verified { get; init; }

    /// <summary>Whether the event was skipped due to idempotency deduplication (duplicate delivery).</summary>
    /// <summary>是否因幂等去重而被跳过（重复投递）。</summary>
    public bool Duplicate { get; init; }

    /// <summary>The actual inbound messages triggered for dispatch (empty after dedup means no dispatch needed).</summary>
    /// <summary>实际触发派发的入站消息（去重后为空表示无需派发）。</summary>
    public IReadOnlyList<InboundMessage> Messages { get; init; }

    /// <summary>URL verification challenge response (e.g. Feishu url_verification scenario returns challenge).</summary>
    /// <summary>URL 验证挑战响应（飞书 url_verification 场景返回 challenge）。</summary>
    public string? ChallengeResponse { get; init; }

    /// <summary>Returns a result indicating the channel does not support inbound callbacks.</summary>
    /// <summary>返回渠道不支持入站回调的结果。</summary>
    public static InboundProcessResult NotSupported => new() { Supported = false };

    /// <summary>Returns a result indicating verification failed.</summary>
    /// <summary>返回验证失败的结果。</summary>
    public static InboundProcessResult FailedVerification => new() { Supported = true, Verified = false, Messages = [] };

    /// <summary>Returns a result indicating the event was skipped as a duplicate.</summary>
    /// <summary>返回因重复被跳过的结果。</summary>
    public static InboundProcessResult SkippedAsDuplicate => new() { Supported = true, Verified = true, Duplicate = true, Messages = [] };

    /// <summary>Creates a result with dispatched messages.</summary>
    /// <summary>创建已派发消息的结果。</summary>
    /// <param name="messages">The dispatched inbound messages. 已派发的入站消息。</param>
    public static InboundProcessResult Dispatched(IReadOnlyList<InboundMessage> messages) =>
        new() { Supported = true, Verified = true, Messages = messages };
}
