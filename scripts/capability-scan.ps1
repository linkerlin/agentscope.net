# AgentScope.NET 能力清单扫描脚本（符号/文件级）
# 用于 CI 与 PR 时可重复校验「已实现/部分实现/缺失」状态
# 用法: .\scripts\capability-scan.ps1 [-OutputPath 'docs/capability-status.md'] [-Json]

param(
    [string]$OutputPath = '',
    [switch]$Json
)

$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot + '\..'
$coreSrc = Join-Path $root 'src\AgentScope.Core'

# Key capabilities: .NET path relative to src/AgentScope.Core
$capabilities = @(
    @{ Id = 'Event'; Path = 'Event\Event.cs'; Desc = 'Event'; PartialPath = 'Event\EventType.cs' },
    @{ Id = 'EventType'; Path = 'Event\EventType.cs'; Desc = 'EventType'; PartialPath = $null },
    @{ Id = 'IStreamableAgent'; Path = 'Agent\IStreamableAgent.cs'; Desc = 'IStreamableAgent'; PartialPath = 'Agent\StreamOptions.cs' },
    @{ Id = 'StreamOptions'; Path = 'Agent\StreamOptions.cs'; Desc = 'StreamOptions'; PartialPath = $null },
    @{ Id = 'Accumulator'; Path = 'Accumulator\ReasoningContext.cs'; Desc = 'Accumulator'; PartialPath = 'Accumulator\TextAccumulator.cs' },
    @{ Id = 'IHttpTransport'; Path = 'Model\Transport\IHttpTransport.cs'; Desc = 'IHttpTransport'; PartialPath = $null },
    @{ Id = 'Hook'; Path = 'Hook\IHook.cs'; Desc = 'Hook'; PartialPath = 'Hook\PreReasoningEvent.cs' },
    @{ Id = 'SkillRegistry'; Path = 'Skill\SkillRegistry.cs'; Desc = 'SkillRegistry'; PartialPath = 'Skill\ISkill.cs' },
    @{ Id = 'McpClient'; Path = 'MCP\McpClientWrapper.cs'; Desc = 'MCP Client'; PartialPath = 'MCP\IMcpClient.cs' },
    @{ Id = 'ITTSModel'; Path = 'Model\TTS\ITTSModel.cs'; Desc = 'TTS'; PartialPath = $null },
    @{ Id = 'Multimodal'; Path = 'Tool\Multimodal\OpenAIMultiModalTool.cs'; Desc = 'Multimodal'; PartialPath = $null },
    @{ Id = 'SubAgentTool'; Path = 'Tool\SubAgent\SubAgentTool.cs'; Desc = 'SubAgentTool'; PartialPath = $null },
    @{ Id = 'ToolGroup'; Path = 'Tool\ToolGroup.cs'; Desc = 'ToolGroup'; PartialPath = 'Tool\ToolGroupManager.cs' },
    @{ Id = 'IState'; Path = 'State\IState.cs'; Desc = 'State'; PartialPath = 'State\IStateModule.cs' },
    @{ Id = 'WebSocket'; Path = 'Model\Transport\WebSocket\IWebSocketTransport.cs'; Desc = 'WebSocket'; PartialPath = $null },
    @{ Id = 'GenerateOptions'; Path = 'Formatter\DashScope\GenerateOptions.cs'; Desc = 'GenerateOptions'; PartialPath = $null },
    @{ Id = 'WebSearchTool'; Path = 'Tool\WebSearchTool.cs'; Desc = 'WebSearchTool'; PartialPath = 'Tool\IWebSearchProvider.cs' }
)

$report = @()
foreach ($c in $capabilities) {
    $fullPath = Join-Path $coreSrc $c.Path
    $exists = Test-Path -LiteralPath $fullPath
    $partialExists = $false
    if ($c.PartialPath) {
        $partialFull = Join-Path $coreSrc $c.PartialPath
        $partialExists = Test-Path -LiteralPath $partialFull
    }
    $status = if ($exists) { 'Implemented' } elseif ($partialExists) { 'Partial' } else { 'Missing' }
    $report += [pscustomobject]@{
        Id     = $c.Id
        Path   = $c.Path
        Desc   = $c.Desc
        Status = $status
    }
}

function Write-Markdown {
    $sb = [System.Text.StringBuilder]::new()
    [void]$sb.AppendLine("# AgentScope.NET Capability Status Report")
    [void]$sb.AppendLine("")
    [void]$sb.AppendLine("Generated: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')")
    [void]$sb.AppendLine("")
    [void]$sb.AppendLine("| Capability | Path | Status |")
    [void]$sb.AppendLine("|------------|------|--------|")
    foreach ($r in $report) {
        [void]$sb.AppendLine("| $($r.Desc) | $($r.Path) | $($r.Status) |")
    }
    [void]$sb.AppendLine("")
    $implemented = ($report | Where-Object { $_.Status -eq 'Implemented' }).Count
    $partial = ($report | Where-Object { $_.Status -eq 'Partial' }).Count
    $missing = ($report | Where-Object { $_.Status -eq 'Missing' }).Count
    [void]$sb.AppendLine("**Summary**: Implemented $implemented | Partial $partial | Missing $missing")
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
