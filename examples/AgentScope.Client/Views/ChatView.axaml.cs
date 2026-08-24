using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace AgentScope.Client.Views;

public partial class ChatView : UserControl
{
    public ChatView()
    {
        InitializeComponent();
    }

    /// <summary>复制消息内容到剪贴板</summary>
    private async void OnCopyClick(object? sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: string content } && !string.IsNullOrEmpty(content))
        {
            var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
            if (clipboard != null)
            {
                await clipboard.SetTextAsync(content);
            }
        }
    }
}
