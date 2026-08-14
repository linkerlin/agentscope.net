using AgentScope.Core.AgUI.Model;
using AgentScope.Core.Message;

namespace AgentScope.Core.AgUI.Converter;

/// <summary>
/// AG-UI ↔ AgentScope 消息双向转换器。对标 Java AguiMessageConverter。
/// </summary>
public sealed class AguiMessageConverter
{
    /// <summary>AG-UI → AgentScope.Msg</summary>
    public Msg ToMsg(AguiMessage aguiMsg)
    {
        var builder = Msg.Builder()
            .Role(aguiMsg.Role)
            .Name(aguiMsg.Role);

        if (aguiMsg.Text != null)
            builder.TextContent(aguiMsg.Text);

        if (aguiMsg.Blocks != null)
        {
            foreach (var block in aguiMsg.Blocks)
            {
                switch (block)
                {
                    case TextInputContent t:
                        builder.Content(new TextBlock { Text = t.Text });
                        break;
                    case ImageInputContent img:
                        builder.Content(new ImageBlock
                        {
                            Url = img.Source is UrlInputSource url ? url.Url : "",
                            Data = img.Source is DataInputSource data ? Convert.FromBase64String(data.Base64) : null
                        });
                        break;
                }
            }
        }

        if (aguiMsg.ToolCalls != null)
        {
            foreach (var tc in aguiMsg.ToolCalls)
            {
                builder.Content(new ToolUseBlock
                {
                    Id = tc.Id,
                    Name = tc.Function?.Name ?? "unknown",
                    Input = tc.Function?.Arguments != null
                        ? System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(tc.Function.Arguments)
                        : null
                });
            }
        }

        return builder.Build();
    }

    /// <summary>AgentScope.Msg → AG-UI</summary>
    public AguiMessage ToAguiMessage(Msg msg)
    {
        return AguiMessage.AssistantMessage(msg.GetTextContent() ?? "");
    }

    /// <summary>批量转换 RunAgentInput → List<Msg></summary>
    public List<Msg> ToMsgList(RunAgentInput input)
    {
        var msgs = new List<Msg>();
        foreach (var aguiMsg in input.Messages)
            msgs.Add(ToMsg(aguiMsg));
        return msgs;
    }
}
