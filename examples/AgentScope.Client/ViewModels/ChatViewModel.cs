using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AgentScope.Client.Models;
using AgentScope.Client.Services;

namespace AgentScope.Client.ViewModels;

public partial class ChatViewModel : ViewModelBase
{
    private readonly ChatService _chatService;
    private readonly ISessionStore _sessionStore;

    [ObservableProperty]
    private ObservableCollection<ChatMessage> _messages = [];

    [ObservableProperty]
    private string _inputText = string.Empty;

    [ObservableProperty]
    private bool _isBusy;

    private Guid _currentSessionId;

    public ChatViewModel(ChatService chatService, ISessionStore sessionStore)
    {
        _chatService = chatService;
        _sessionStore = sessionStore;
    }

    public async Task LoadSession(Guid sessionId)
    {
        _currentSessionId = sessionId;
        var msgs = await _sessionStore.GetMessagesAsync(sessionId);
        Messages = new ObservableCollection<ChatMessage>(msgs);
    }

    [RelayCommand]
    private async Task SendMessage()
    {
        var text = InputText?.Trim();
        if (string.IsNullOrEmpty(text) || _currentSessionId == Guid.Empty) return;

        InputText = string.Empty;
        IsBusy = true;

        var userMsg = new ChatMessage
        {
            SessionId = _currentSessionId,
            Role = "user",
            Content = text,
            Timestamp = DateTime.UtcNow
        };
        Messages.Add(userMsg);

        try
        {
            var assistantMsg = new ChatMessage
            {
                SessionId = _currentSessionId,
                Role = "assistant",
                Content = string.Empty,
                Timestamp = DateTime.UtcNow
            };
            Messages.Add(assistantMsg);
            var contentBuilder = new System.Text.StringBuilder();

            await foreach (var chunk in _chatService.StreamMessageAsync(_currentSessionId, text))
            {
                contentBuilder.Append(chunk);
                assistantMsg.Content = contentBuilder.ToString();

                var idx = Messages.Count - 1;
                Messages[idx] = assistantMsg;
            }
        }
        catch (System.Exception ex)
        {
            Messages.Add(new ChatMessage
            {
                SessionId = _currentSessionId,
                Role = "assistant",
                Content = $"错误: {ex.Message}",
                Timestamp = DateTime.UtcNow
            });
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task NewChat()
    {
        var session = await _sessionStore.CreateSessionAsync();
        _currentSessionId = session.Id;
        Messages.Clear();
    }
}
