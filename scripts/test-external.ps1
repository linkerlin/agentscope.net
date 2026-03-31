 [CmdletBinding()]
param(
    [switch]$Full,

    [ValidateSet("quiet", "minimal", "normal", "detailed", "diagnostic")]
    [string]$Verbosity = "minimal",

    [Parameter(ValueFromRemainingArguments = $true)]
    [string[]]$AdditionalArgs
)

$filter = if ($Full) {
    "Category=ExternalDependency"
}
else {
    "Category=ExternalDependencySmoke"
}

$arguments = @(
    "test",
    "tests/AgentScope.Integration.Tests/AgentScope.Integration.Tests.csproj",
    "--filter",
    $filter,
    "--verbosity",
    $Verbosity
)

if ($AdditionalArgs) {
    $arguments += $AdditionalArgs
}

$previousValue = [System.Environment]::GetEnvironmentVariable("AGENTSCOPE_RUN_EXTERNAL_TESTS", "Process")

try {
    [System.Environment]::SetEnvironmentVariable("AGENTSCOPE_RUN_EXTERNAL_TESTS", "1", "Process")
    & dotnet @arguments
    exit $LASTEXITCODE
}
finally {
    [System.Environment]::SetEnvironmentVariable("AGENTSCOPE_RUN_EXTERNAL_TESTS", $previousValue, "Process")
}
