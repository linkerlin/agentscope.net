using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AgentScope.Client.Models;
using AgentScope.Client.Services;

namespace AgentScope.Client.ViewModels;

public partial class McpListViewModel : ViewModelBase
{
    private readonly McpConfigService _service;

    [ObservableProperty]
    private ObservableCollection<McpConfig> _items = [];

    [ObservableProperty]
    private McpConfig? _selectedItem;

    [ObservableProperty]
    private bool _isEditing;

    // 编辑字段
    [ObservableProperty]
    private string _editName = string.Empty;

    [ObservableProperty]
    private string _editTransportType = "stdio";

    [ObservableProperty]
    private string _editCommand = string.Empty;

    [ObservableProperty]
    private string _editArgs = string.Empty;

    [ObservableProperty]
    private string _editUrl = string.Empty;

    [ObservableProperty]
    private string _editApiKey = string.Empty;

    [ObservableProperty]
    private string _editWorkingDirectory = string.Empty;

    [ObservableProperty]
    private bool _editIsEnabled = true;

    public static string[] TransportTypes => ["stdio", "sse", "http"];

    public McpListViewModel(McpConfigService service)
    {
        _service = service;
    }

    public async Task LoadAsync()
    {
        var list = await _service.GetAllAsync();
        Items = new ObservableCollection<McpConfig>(list);
    }

    [RelayCommand]
    private void NewItem()
    {
        EditName = string.Empty;
        EditTransportType = "stdio";
        EditCommand = string.Empty;
        EditArgs = string.Empty;
        EditUrl = string.Empty;
        EditApiKey = string.Empty;
        EditWorkingDirectory = string.Empty;
        EditIsEnabled = true;
        SelectedItem = null;
        IsEditing = true;
    }

    [RelayCommand]
    private void EditItem()
    {
        if (SelectedItem == null) return;
        EditName = SelectedItem.Name;
        EditTransportType = SelectedItem.TransportType;
        EditCommand = SelectedItem.Command ?? string.Empty;
        EditArgs = SelectedItem.Args ?? string.Empty;
        EditUrl = SelectedItem.Url ?? string.Empty;
        EditApiKey = SelectedItem.ApiKey ?? string.Empty;
        EditWorkingDirectory = SelectedItem.WorkingDirectory ?? string.Empty;
        EditIsEnabled = SelectedItem.IsEnabled;
        IsEditing = true;
    }

    [RelayCommand]
    private async Task SaveItem()
    {
        var item = SelectedItem ?? new McpConfig { Id = Guid.NewGuid() };
        item.Name = EditName;
        item.TransportType = EditTransportType;
        item.Command = EditCommand;
        item.Args = EditArgs;
        item.Url = EditUrl;
        item.ApiKey = EditApiKey;
        item.WorkingDirectory = EditWorkingDirectory;
        item.IsEnabled = EditIsEnabled;

        await _service.SaveAsync(item);
        IsEditing = false;
        await LoadAsync();
    }

    [RelayCommand]
    private void CancelEdit() => IsEditing = false;

    [RelayCommand]
    private async Task DeleteItem()
    {
        if (SelectedItem == null) return;
        await _service.DeleteAsync(SelectedItem.Id);
        SelectedItem = null;
        await LoadAsync();
    }
}
