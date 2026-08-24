using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AgentScope.Client.Models;
using AgentScope.Client.Services;

namespace AgentScope.Client.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly ISessionStore _sessionStore;

    [ObservableProperty]
    private ObservableCollection<ChatSession> _sessions = [];

    [ObservableProperty]
    private ChatSession? _selectedSession;

    [ObservableProperty]
    private ViewModelBase? _currentPage;

    /// <summary>当前页面标识，用于导航高亮：chat/agents/settings/mcps/skills</summary>
    [ObservableProperty]
    private string _currentPageId = "chat";

    public ChatViewModel ChatViewModel { get; }
    public SettingsViewModel SettingsViewModel { get; }
    public AgentListViewModel AgentListViewModel { get; }
    public McpListViewModel McpListViewModel { get; }
    public SkillListViewModel SkillListViewModel { get; }

    public MainWindowViewModel(
        ISessionStore sessionStore,
        ChatViewModel chatViewModel,
        SettingsViewModel settingsViewModel,
        AgentListViewModel agentListViewModel,
        McpListViewModel mcpListViewModel,
        SkillListViewModel skillListViewModel)
    {
        _sessionStore = sessionStore;
        ChatViewModel = chatViewModel;
        SettingsViewModel = settingsViewModel;
        AgentListViewModel = agentListViewModel;
        McpListViewModel = mcpListViewModel;
        SkillListViewModel = skillListViewModel;

        CurrentPage = ChatViewModel;
    }

    [RelayCommand]
    private async Task LoadSessions()
    {
        var list = await _sessionStore.GetAllSessionsAsync();
        Sessions = new ObservableCollection<ChatSession>(list);
    }

    [RelayCommand]
    private async Task NewSession()
    {
        var session = await _sessionStore.CreateSessionAsync();
        Sessions.Insert(0, session);
        SelectedSession = session;
        await ChatViewModel.LoadSession(session.Id);
    }

    [RelayCommand]
    private async Task DeleteSession()
    {
        if (SelectedSession == null) return;
        await _sessionStore.DeleteSessionAsync(SelectedSession.Id);
        Sessions.Remove(SelectedSession);
        SelectedSession = null;
    }

    partial void OnSelectedSessionChanged(ChatSession? value)
    {
        if (value != null)
        {
            _ = ChatViewModel.LoadSession(value.Id);
            CurrentPageId = "chat";
            CurrentPage = ChatViewModel;
        }
    }

    [RelayCommand]
    private async Task NavigateToChat()
    {
        CurrentPageId = "chat";
        CurrentPage = ChatViewModel;
        await Task.CompletedTask;
    }

    [RelayCommand]
    private async Task NavigateToAgents()
    {
        CurrentPageId = "agents";
        CurrentPage = AgentListViewModel;
        await AgentListViewModel.LoadAsync();
    }

    [RelayCommand]
    private async Task NavigateToMcps()
    {
        CurrentPageId = "mcps";
        CurrentPage = McpListViewModel;
        await McpListViewModel.LoadAsync();
    }

    [RelayCommand]
    private async Task NavigateToSkills()
    {
        CurrentPageId = "skills";
        CurrentPage = SkillListViewModel;
        await SkillListViewModel.LoadAsync();
    }

    [RelayCommand]
    private async Task NavigateToSettings()
    {
        CurrentPageId = "settings";
        CurrentPage = SettingsViewModel;
        await SettingsViewModel.LoadAsync();
    }
}
