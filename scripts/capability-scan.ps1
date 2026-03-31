# AgentScope.NET 能力清单扫描脚本（能力成熟度级别）
# 用于 CI 与 PR 时可重复校验「源码存在 / 已测试 / 已接入 / 真实 Provider」状态
# 用法: .\scripts\capability-scan.ps1 [-OutputPath 'docs/capability-status.md'] [-Json]

param(
    [string]$OutputPath = '',
    [switch]$Json
)

$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot + '\..'
$coreSrc = Join-Path $root 'src\AgentScope.Core'
$testsRoot = Join-Path $root 'tests'

# Key capabilities: .NET path relative to src/AgentScope.Core
$capabilities = @(
    @{
        Id = 'Event'; Path = 'Event\Event.cs'; Desc = 'Event';
        TestPaths = @('AgentScope.Core.Tests\Event\EventTests.cs');
        IntegrationPaths = @('Agent\IStreamableAgent.cs', 'Agent\AgentStreamAdapter.cs');
        ProviderPaths = @(); RequiresProvider = $false
    },
    @{
        Id = 'EventType'; Path = 'Event\EventType.cs'; Desc = 'EventType';
        TestPaths = @('AgentScope.Core.Tests\Event\EventTests.cs');
        IntegrationPaths = @('Event\Event.cs');
        ProviderPaths = @(); RequiresProvider = $false
    },
    @{
        Id = 'IStreamableAgent'; Path = 'Agent\IStreamableAgent.cs'; Desc = 'IStreamableAgent';
        TestPaths = @('AgentScope.Core.Tests\Agent\EnhancedReActAgentStreamingTests.cs');
        IntegrationPaths = @('Agent\AgentStreamAdapter.cs', 'EnhancedReActAgent.cs');
        ProviderPaths = @(); RequiresProvider = $false
    },
    @{
        Id = 'StreamOptions'; Path = 'Agent\StreamOptions.cs'; Desc = 'StreamOptions';
        TestPaths = @('AgentScope.Core.Tests\Agent\EnhancedReActAgentStreamingTests.cs');
        IntegrationPaths = @('Agent\IStreamableAgent.cs', 'EnhancedReActAgent.cs');
        ProviderPaths = @(); RequiresProvider = $false
    },
    @{
        Id = 'Accumulator'; Path = 'Accumulator\ReasoningContext.cs'; Desc = 'Accumulator';
        TestPaths = @('AgentScope.Core.Tests\Accumulator\AccumulatorTests.cs');
        IntegrationPaths = @('Accumulator\TextAccumulator.cs', 'Accumulator\ThinkingAccumulator.cs', 'Accumulator\ToolCallsAccumulator.cs');
        ProviderPaths = @(); RequiresProvider = $false
    },
    @{
        Id = 'IHttpTransport'; Path = 'Model\Transport\IHttpTransport.cs'; Desc = 'IHttpTransport';
        TestPaths = @('AgentScope.Core.Tests\Model\Transport\HttpClientTransportTests.cs');
        IntegrationPaths = @('Model\Transport\HttpClientTransport.cs', 'Model\OpenAI\OpenAIClient.cs');
        ProviderPaths = @(); RequiresProvider = $false
    },
    @{
        Id = 'Hook'; Path = 'Hook\IHook.cs'; Desc = 'Hook';
        TestPaths = @('AgentScope.Core.Tests\Hook\HookChunkErrorTests.cs');
        IntegrationPaths = @('EnhancedReActAgent.cs');
        ProviderPaths = @(); RequiresProvider = $false
    },
    @{
        Id = 'SkillRegistry'; Path = 'Skill\SkillRegistry.cs'; Desc = 'SkillRegistry';
        TestPaths = @('AgentScope.Core.Tests\Skill\SkillRegistryTests.cs');
        IntegrationPaths = @('Skill\ISkill.cs', 'Skill\ISkillRepository.cs', 'Skill\FileSystemSkillRepository.cs');
        ProviderPaths = @(); RequiresProvider = $false
    },
    @{
        Id = 'McpClient'; Path = 'MCP\McpClientWrapper.cs'; Desc = 'MCP Client';
        TestPaths = @('AgentScope.Core.Tests\MCP\McpToolTests.cs');
        IntegrationPaths = @('MCP\IMcpClient.cs', 'MCP\McpTool.cs');
        ProviderPaths = @(); RequiresProvider = $true
    },
    @{
        Id = 'ITTSModel'; Path = 'Model\TTS\ITTSModel.cs'; Desc = 'TTS';
        TestPaths = @('AgentScope.Core.Tests\Model\TTS\TTSTests.cs');
        IntegrationPaths = @('Model\TTS\IRealtimeTTSModel.cs', 'Model\TTS\StubTTSModel.cs', 'Model\TTS\AudioPlayer.cs');
        ProviderPaths = @(); RequiresProvider = $true
    },
    @{
        Id = 'Multimodal'; Path = 'Tool\Multimodal\OpenAIMultiModalTool.cs'; Desc = 'Multimodal';
        TestPaths = @('AgentScope.Core.Tests\Tool\Multimodal\OpenAIMultiModalToolTests.cs');
        IntegrationPaths = @('Tool\Multimodal\OpenAIMultiModalTool.cs');
        ProviderPaths = @(); RequiresProvider = $true
    },
    @{
        Id = 'SubAgentTool'; Path = 'Tool\SubAgent\SubAgentTool.cs'; Desc = 'SubAgentTool';
        TestPaths = @('AgentScope.Core.Tests\Tool\SubAgent\SubAgentToolTests.cs');
        IntegrationPaths = @('Tool\SubAgent\ISubAgentProvider.cs', 'Tool\SubAgent\SubAgentConfig.cs', 'State\IStateModule.cs');
        ProviderPaths = @(); RequiresProvider = $false
    },
    @{
        Id = 'ToolGroup'; Path = 'Tool\ToolGroup.cs'; Desc = 'ToolGroup';
        TestPaths = @('AgentScope.Core.Tests\Tool\ToolGroupTests.cs');
        IntegrationPaths = @('Tool\ToolGroupManager.cs');
        ProviderPaths = @(); RequiresProvider = $false
    },
    @{
        Id = 'IState'; Path = 'State\IState.cs'; Desc = 'State';
        TestPaths = @('AgentScope.Core.Tests\State\StateTests.cs');
        IntegrationPaths = @('State\IStateModule.cs', 'State\StatePersistence.cs', 'State\AgentMetaState.cs', 'State\ToolkitState.cs');
        ProviderPaths = @(); RequiresProvider = $false
    },
    @{
        Id = 'WebSocket'; Path = 'Model\Transport\WebSocket\IWebSocketTransport.cs'; Desc = 'WebSocket';
        TestPaths = @('AgentScope.Core.Tests\Model\Transport\WebSocket\WebSocketTransportTests.cs');
        IntegrationPaths = @('Model\Transport\WebSocket\ClientWebSocketTransport.cs', 'Model\Transport\WebSocket\IWebSocketConnection.cs');
        ProviderPaths = @(); RequiresProvider = $false
    },
    @{
        Id = 'GenerateOptions'; Path = 'Formatter\DashScope\GenerateOptions.cs'; Desc = 'GenerateOptions';
        TestPaths = @('AgentScope.Core.Tests\Formatter\DashScope\DashScopeFormatterTests.cs');
        IntegrationPaths = @('Model\DashScope\DashScopeModel.cs', 'Formatter\DashScope\DashScopeChatFormatter.cs');
        ProviderPaths = @(); RequiresProvider = $false
    },
    @{
        Id = 'WebSearchTool'; Path = 'Tool\WebSearchTool.cs'; Desc = 'WebSearchTool';
        TestPaths = @('AgentScope.Core.Tests\Tool\WebSearchToolTests.cs');
        IntegrationPaths = @('Tool\IWebSearchProvider.cs', 'Tool\SimulatedWebSearchProvider.cs');
        ProviderPaths = @('Tool\SimulatedWebSearchProvider.cs'); RequiresProvider = $false
    }
)

$maturityOrder = @('Missing', 'Scaffolded', 'Verified', 'Integrated', 'ProviderReady')

function Test-PathsExist {
    param(
        [string]$BasePath,
        [object[]]$RelativePaths
    )

    if ($null -eq $RelativePaths -or $RelativePaths.Count -eq 0) {
        return $false
    }

    foreach ($relativePath in $RelativePaths) {
        if ([string]::IsNullOrWhiteSpace($relativePath)) {
            continue
        }

        $fullPath = Join-Path $BasePath $relativePath
        if (Test-Path -LiteralPath $fullPath) {
            return $true
        }
    }

    return $false
}

function Get-Maturity {
    param(
        [bool]$HasSource,
        [bool]$HasTests,
        [bool]$HasIntegration,
        [bool]$HasProvider,
        [bool]$RequiresProvider
    )

    if (-not $HasSource) {
        return 'Missing'
    }

    if (-not $HasTests) {
        return 'Scaffolded'
    }

    if (-not $HasIntegration) {
        return 'Verified'
    }

    if ($RequiresProvider -and -not $HasProvider) {
        return 'Integrated'
    }

    if ($RequiresProvider) {
        return 'ProviderReady'
    }

    return 'Integrated'
}

$report = @()
foreach ($c in $capabilities) {
    $fullPath = Join-Path $coreSrc $c.Path
    $hasSource = Test-Path -LiteralPath $fullPath
    $hasTests = Test-PathsExist -BasePath $testsRoot -RelativePaths $c.TestPaths
    $hasIntegration = Test-PathsExist -BasePath $coreSrc -RelativePaths $c.IntegrationPaths
    $hasProvider = Test-PathsExist -BasePath $coreSrc -RelativePaths $c.ProviderPaths
    $status = Get-Maturity -HasSource $hasSource -HasTests $hasTests -HasIntegration $hasIntegration -HasProvider $hasProvider -RequiresProvider $c.RequiresProvider

    $report += [pscustomobject]@{
        Id           = $c.Id
        Path         = $c.Path
        Desc         = $c.Desc
        Source       = if ($hasSource) { 'Yes' } else { 'No' }
        Tests        = if ($hasTests) { 'Yes' } else { 'No' }
        Integration  = if ($hasIntegration) { 'Yes' } else { 'No' }
        Provider     = if ($c.RequiresProvider) { if ($hasProvider) { 'Yes' } else { 'No' } } else { 'N/A' }
        Status       = $status
    }
}

function Write-Markdown {
    $sb = [System.Text.StringBuilder]::new()
    [void]$sb.AppendLine("# AgentScope.NET Capability Status Report")
    [void]$sb.AppendLine("")
    [void]$sb.AppendLine("Generated: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')")
    [void]$sb.AppendLine("")
    [void]$sb.AppendLine("Status model: Missing -> Scaffolded -> Verified -> Integrated -> ProviderReady")
    [void]$sb.AppendLine("")
    [void]$sb.AppendLine("| Capability | Path | Source | Tests | Integration | Provider | Status |")
    [void]$sb.AppendLine("|------------|------|--------|-------|-------------|----------|--------|")
    foreach ($r in $report) {
        [void]$sb.AppendLine("| $($r.Desc) | $($r.Path) | $($r.Source) | $($r.Tests) | $($r.Integration) | $($r.Provider) | $($r.Status) |")
    }
    [void]$sb.AppendLine("")
    $summaryParts = foreach ($status in $maturityOrder) {
        $count = ($report | Where-Object { $_.Status -eq $status }).Count
        "$status $count"
    }
    [void]$sb.AppendLine("**Summary**: " + ($summaryParts -join ' | '))
    $sb.ToString()
}

if ($Json) {
    $report | ConvertTo-Json -Depth 3
} else {
    $md = Write-Markdown
    if ($OutputPath) {
        $outFull = Join-Path $root $OutputPath
        $dir = Split-Path -Parent $outFull
        if ($dir -and !(Test-Path $dir)) { New-Item -ItemType Directory -Path $dir -Force | Out-Null }
        Set-Content -Path $outFull -Value $md -Encoding UTF8
        Write-Host "Report written: $outFull"
    } else {
        Write-Host $md
    }
}

# Exit 0 always; CI can parse report or use -Json for gates
exit 0
