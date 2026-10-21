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
    New-Item -ItemType Directory -Path $OutputRoot -Force | Out-Null
    $arguments = @('test', (Join-Path $repository 'ChartForgeX.Tests/ChartForgeX.Tests.csproj'), '-c', 'Release',
        '--filter', 'Category=MermaidReference', '--settings', (Join-Path $PSScriptRoot 'browser.runsettings'),
        '--logger', 'trx;LogFileName=mermaid-reference.trx', '--results-directory', $OutputRoot)
    if ($SkipBuild) { $arguments += @('--no-build', '--no-restore') }
    dotnet @arguments
    if ($LASTEXITCODE) { throw 'Observed Mermaid reference/native rendering failed.' }
    [xml] $results = Get-Content -LiteralPath (Join-Path $OutputRoot 'mermaid-reference.trx') -Raw
    $counters = $results.TestRun.ResultSummary.Counters
    if ([int]$counters.total -lt 8 -or [int]$counters.executed -ne [int]$counters.total -or [int]$counters.failed -ne 0 -or [int]$counters.notExecuted -ne 0) {
        throw 'Observed Mermaid proof must execute the three pinned versions and dark-theme row without skips.'
    }
    if (-not ($results.TestRun.Results.UnitTestResult | Where-Object {
        $_.testName -like 'ChartForgeX.Tests.MermaidNotationReferenceRenderingTests.PinnedNotationAndNativeOutputPreserveSharedVisibleContracts*' -and $_.outcome -eq 'Passed'
    })) { throw 'The common-notation reference rendering contracts did not execute.' }
} finally {
    foreach ($name in $variables) { [Environment]::SetEnvironmentVariable($name, $saved[$name]) }
}
