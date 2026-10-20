param(
    [Parameter(Mandatory = $true)]
    [string] $OutputRoot,
    [switch] $SkipBuild
)

$ErrorActionPreference = 'Stop'
if (-not [IO.Path]::IsPathRooted($OutputRoot)) { throw 'OutputRoot must be an explicit absolute task-owned path.' }
$repository = Split-Path -Parent $PSScriptRoot
$conformance = Join-Path $repository 'tests/mermaid-conformance'
$variables = @('HFX_TEST_ENABLE_PLAYWRIGHT', 'CFX_TEST_MERMAID_REFERENCE', 'CFX_BROWSER_CAPTURE_DIRECTORY')
$saved = @{}
foreach ($name in $variables) { $saved[$name] = [Environment]::GetEnvironmentVariable($name) }
try {
    Push-Location $conformance
    try {
        npm ci --engine-strict --ignore-scripts --no-audit --no-fund
        if ($LASTEXITCODE) { throw 'Pinned Mermaid reference restore failed.' }
    } finally { Pop-Location }
    $env:HFX_TEST_ENABLE_PLAYWRIGHT = '1'
    $env:CFX_TEST_MERMAID_REFERENCE = '1'
    $env:CFX_BROWSER_CAPTURE_DIRECTORY = [IO.Path]::GetFullPath($OutputRoot)
    $arguments = @('test', (Join-Path $repository 'ChartForgeX.Tests/ChartForgeX.Tests.csproj'), '-c', 'Release',
        '--filter', 'Category=MermaidReference', '--settings', (Join-Path $PSScriptRoot 'browser.runsettings'))
    if ($SkipBuild) { $arguments += @('--no-build', '--no-restore') }
    dotnet @arguments
    if ($LASTEXITCODE) { throw 'Observed Mermaid reference/native rendering failed.' }
} finally {
    foreach ($name in $variables) { [Environment]::SetEnvironmentVariable($name, $saved[$name]) }
}
