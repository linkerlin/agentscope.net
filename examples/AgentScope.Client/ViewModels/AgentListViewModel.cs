using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AgentScope.Client.Models;
using AgentScope.Client.Services;

namespace AgentScope.Client.ViewModels;

public partial class AgentListViewModel : ViewModelBase
{
    private readonly AgentConfigService _configService;
    private readonly McpConfigService _mcpConfigService;
    private readonly SkillConfigService _skillConfigService;

    [ObservableProperty]
    private ObservableCollection<AgentConfig> _agents = new();

    [ObservableProperty]
    private AgentConfig? _selectedAgent;

    [ObservableProperty]
    private bool _isEditing;

    /// <summary>数据是否已加载（避免 ComboBox 显示空的初始集合）</summary>
    [ObservableProperty]
    private bool _isDataLoaded;

    // ── 编辑字段 ──
    [ObservableProperty]
    private string _editName = string.Empty;

    [ObservableProperty]
    private string _editDescription = string.Empty;

    [ObservableProperty]
    private string _editSystemPrompt = string.Empty;

    [ObservableProperty]
    private int _editMaxIterations = 10;

    // ── 三个 ComboBox 下拉列表 ──
    [ObservableProperty]
    private ObservableCollection<LlmConfig> _llmConfigs = new();

    [ObservableProperty]
    private LlmConfig? _editSelectedLlm;

    [ObservableProperty]
    private ObservableCollection<McpConfig> _mcpConfigs = new();

    [ObservableProperty]
    private McpConfig? _editSelectedMcp;

    [ObservableProperty]
    private ObservableCollection<SkillConfig> _skillConfigs = new();

    [ObservableProperty]
    private SkillConfig? _editSelectedSkill;

    // ── 构造 ──
    public AgentListViewModel(
        AgentConfigService configService,
        McpConfigService mcpConfigService,
        SkillConfigService skillConfigService)
    {
        _configService = configService;
        _mcpConfigService = mcpConfigService;
        _skillConfigService = skillConfigService;
    }

    public async Task LoadAsync()
    {
        // 突变（mutate）已有集合而非替换，确保 ComboBox 绑定的引用不变
        var agents = await _configService.GetAllAgentsAsync();
        Agents.Clear();
        foreach (var a in agents) Agents.Add(a);

        var llms = await _configService.GetAllLlmConfigsAsync();
        LlmConfigs.Clear();
        foreach (var l in llms) LlmConfigs.Add(l);

        var mcps = await _mcpConfigService.GetAllAsync();
        McpConfigs.Clear();
        foreach (var m in mcps) McpConfigs.Add(m);

        var skills = await _skillConfigService.GetAllAsync();
        SkillConfigs.Clear();
        foreach (var s in skills) SkillConfigs.Add(s);

        IsDataLoaded = true;
    }

    [RelayCommand]
    private void NewAgent()
    {
        EditName = string.Empty;
        EditDescription = string.Empty;
        EditSystemPrompt = string.Empty;
        EditMaxIterations = 10;
        EditSelectedLlm = null;
        EditSelectedMcp = null;
        EditSelectedSkill = null;
        SelectedAgent = null;
        IsEditing = true;
    }

    [RelayCommand]
    private void EditAgent()
    {
        if (SelectedAgent == null) return;
        EditName = SelectedAgent.Name;
        EditDescription = SelectedAgent.Description ?? string.Empty;
        EditSystemPrompt = SelectedAgent.SystemPrompt ?? string.Empty;
        EditMaxIterations = SelectedAgent.MaxIterations;

        // 从 ComboBox 数据源中找到匹配项
        EditSelectedLlm = SelectedAgent.ModelId != null
            ? LlmConfigs.FirstOrDefault(l => l.Id == SelectedAgent.ModelId)
            : null;

        EditSelectedMcp = SelectedAgent.McpId != null
            ? McpConfigs.FirstOrDefault(m => m.Id == SelectedAgent.McpId)
            : null;

        EditSelectedSkill = SelectedAgent.SkillId != null
            ? SkillConfigs.FirstOrDefault(s => s.Id == SelectedAgent.SkillId)
            : null;

        IsEditing = true;
    }

    [RelayCommand]
    private async Task SaveAgent()
    {
        var agent = SelectedAgent ?? new AgentConfig { Id = Guid.NewGuid() };
        agent.Name = EditName;
        agent.Description = EditDescription;
        agent.SystemPrompt = EditSystemPrompt;
        agent.MaxIterations = EditMaxIterations;
        agent.ModelId = EditSelectedLlm?.Id;
        agent.McpId = EditSelectedMcp?.Id;
        agent.SkillId = EditSelectedSkill?.Id;

        await _configService.SaveAgentAsync(agent);
        IsEditing = false;
        await LoadAsync();
    }

    [RelayCommand]
    private void CancelEdit() => IsEditing = false;

    [RelayCommand]
    private async Task DeleteAgent()
    {
        if (SelectedAgent == null) return;
        await _configService.DeleteAgentAsync(SelectedAgent.Id);
        SelectedAgent = null;
        await LoadAsync();
    }
}
