 [CmdletBinding()]
param(
    [ValidateSet("quiet", "minimal", "normal", "detailed", "diagnostic")]
    [string]$Verbosity = "minimal",

    [Parameter(ValueFromRemainingArguments = $true)]
    [string[]]$AdditionalArgs
)

$arguments = @(
    "test",
    "AgentScope.slnx",
    "--filter",
    "Category!=ExternalDependency",
    "--verbosity",
    $Verbosity
)

if ($AdditionalArgs) {
    $arguments += $AdditionalArgs
}

& dotnet @arguments
exit $LASTEXITCODE
