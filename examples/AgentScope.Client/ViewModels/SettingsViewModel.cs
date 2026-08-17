using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AgentScope.Client.Models;
using AgentScope.Client.Services;

namespace AgentScope.Client.ViewModels;

public partial class SettingsViewModel : ViewModelBase
{
    private readonly AgentConfigService _configService;

    [ObservableProperty]
    private ObservableCollection<LlmConfig> _llmConfigs = [];

    [ObservableProperty]
    private LlmConfig? _selectedLlm;

    [ObservableProperty]
    private bool _isEditing;

    [ObservableProperty]
    private string _editName = string.Empty;

    [ObservableProperty]
    private string _editProvider = "openai";

    [ObservableProperty]
    private string _editModelName = string.Empty;

    [ObservableProperty]
    private string _editApiKey = string.Empty;

    [ObservableProperty]
    private string _editBaseUrl = string.Empty;

    [ObservableProperty]
    private double _editTemperature = 0.7;

    public static string[] Providers => ["openai", "deepseek", "anthropic", "gemini", "dashscope", "ollama"];

    public SettingsViewModel(AgentConfigService configService)
    {
        _configService = configService;
    }

    public async Task LoadAsync()
    {
        var list = await _configService.GetAllLlmConfigsAsync();
        LlmConfigs = new ObservableCollection<LlmConfig>(list);
    }

    [RelayCommand]
    private void NewLlm()
    {
        EditName = string.Empty;
        EditProvider = "openai";
        EditModelName = string.Empty;
        EditApiKey = string.Empty;
        EditBaseUrl = string.Empty;
        EditTemperature = 0.7;
        SelectedLlm = null;
        IsEditing = true;
    }

    [RelayCommand]
    private void EditLlm()
    {
        if (SelectedLlm == null) return;
        EditName = SelectedLlm.Name;
        EditProvider = SelectedLlm.Provider;
        EditModelName = SelectedLlm.ModelName;
        EditApiKey = SelectedLlm.ApiKey ?? string.Empty;
        EditBaseUrl = SelectedLlm.BaseUrl ?? string.Empty;
        EditTemperature = SelectedLlm.Temperature;
        IsEditing = true;
    }

    [RelayCommand]
    private async Task SaveLlm()
    {
        var llm = SelectedLlm ?? new LlmConfig { Id = Guid.NewGuid() };
        llm.Name = EditName;
        llm.Provider = EditProvider;
        llm.ModelName = EditModelName;
        llm.ApiKey = EditApiKey;
        llm.BaseUrl = EditBaseUrl;
        llm.Temperature = EditTemperature;

        await _configService.SaveLlmAsync(llm);
        IsEditing = false;
        await LoadAsync();
    }

    [RelayCommand]
    private void CancelEdit()
    {
        IsEditing = false;
    }

    [RelayCommand]
    private async Task DeleteLlm()
    {
        if (SelectedLlm == null) return;
        await _configService.DeleteLlmAsync(SelectedLlm.Id);
        SelectedLlm = null;
        await LoadAsync();
    }

    [RelayCommand]
    private async Task SetDefault()
    {
        if (SelectedLlm == null) return;
        SelectedLlm.IsDefault = true;
        await _configService.SaveLlmAsync(SelectedLlm);
        await LoadAsync();
    }
}
