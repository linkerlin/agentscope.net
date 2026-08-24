using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AgentScope.Client.Models;
using AgentScope.Client.Services;

namespace AgentScope.Client.ViewModels;

public partial class SkillListViewModel : ViewModelBase
{
    private readonly SkillConfigService _service;

    [ObservableProperty]
    private ObservableCollection<SkillConfig> _items = [];

    [ObservableProperty]
    private SkillConfig? _selectedItem;

    [ObservableProperty]
    private bool _isEditing;

    // 编辑字段
    [ObservableProperty]
    private string _editName = string.Empty;

    [ObservableProperty]
    private string _editDescription = string.Empty;

    [ObservableProperty]
    private string _editSourceType = "file";

    [ObservableProperty]
    private string _editSourcePath = string.Empty;

    [ObservableProperty]
    private string _editRawContent = string.Empty;

    [ObservableProperty]
    private bool _editIsActive = true;

    public static string[] SourceTypes => ["file", "inline"];

    public SkillListViewModel(SkillConfigService service)
    {
        _service = service;
    }

    public async Task LoadAsync()
    {
        var list = await _service.GetAllAsync();
        Items = new ObservableCollection<SkillConfig>(list);
    }

    [RelayCommand]
    private void NewItem()
    {
        EditName = string.Empty;
        EditDescription = string.Empty;
        EditSourceType = "file";
        EditSourcePath = string.Empty;
        EditRawContent = string.Empty;
        EditIsActive = true;
        SelectedItem = null;
        IsEditing = true;
    }

    [RelayCommand]
    private void EditItem()
    {
        if (SelectedItem == null) return;
        EditName = SelectedItem.Name;
        EditDescription = SelectedItem.Description ?? string.Empty;
        EditSourceType = SelectedItem.SourceType;
        EditSourcePath = SelectedItem.SourcePath ?? string.Empty;
        EditRawContent = SelectedItem.RawContent ?? string.Empty;
        EditIsActive = SelectedItem.IsActive;
        IsEditing = true;
    }

    [RelayCommand]
    private async Task SaveItem()
    {
        var item = SelectedItem ?? new SkillConfig { Id = Guid.NewGuid() };
        item.Name = EditName;
        item.Description = EditDescription;
        item.SourceType = EditSourceType;
        item.SourcePath = EditSourcePath;
        item.RawContent = EditRawContent;
        item.IsActive = EditIsActive;

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
