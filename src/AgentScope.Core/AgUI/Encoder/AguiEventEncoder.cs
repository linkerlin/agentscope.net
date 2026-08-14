using System.Text.Json;
using AgentScope.Core.AgUI.Event;

namespace AgentScope.Core.AgUI.Encoder;

/// <summary>
/// AG-UI 事件 SSE 编码器。对标 Java AguiEventEncoder。
/// 将 AguiEvent 序列化为 SSE（Server-Sent Events）格式。
/// </summary>
public static class AguiEventEncoder
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        WriteIndented = false,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    /// <summary>编码为 SSE 事件行</summary>
    public static string Encode(AguiEvent evt)
    {
        var json = EncodeToJson(evt);
        return $"data: {json}\n\n";
    }

    /// <summary>仅编码为 JSON 字符串</summary>
    public static string EncodeToJson(AguiEvent evt)
    {
        return JsonSerializer.Serialize(evt, JsonOpts);
    }

    /// <summary>SSE 注释行</summary>
    public static string EncodeComment(string comment) => $": {comment}\n\n";

    /// <summary>保活信号</summary>
    public static string KeepAlive() => ": keepalive\n\n";
}
