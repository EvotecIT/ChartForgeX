<#
.SYNOPSIS
Runs the bounded, engine-independent HTML chart compatibility contracts through HtmlTinkerX.
.PARAMETER Browser
The existing HtmlTinkerX engine to qualify. Chromium's complete interaction suite remains a separate gate.
.PARAMETER OutputRoot
An absolute task-owned directory for results and rendered evidence.
.PARAMETER SkipBuild
Uses a previously built Release test assembly.
.PARAMETER Filter
Selects a focused set of browser contracts. The default runs the complete compatibility suite.
.PARAMETER ShardIndex
One-based shard to run when the selected contracts are split across parallel jobs.
.PARAMETER ShardCount
Number of parallel shards. Whole test classes are balanced across shards by their number of contracts;
every shard must own at least one class.
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [ValidateSet('chromium', 'firefox', 'webkit')]
    [string] $Browser,

    [Parameter(Mandatory = $true)]
    [string] $OutputRoot,

    [switch] $SkipBuild,

    [ValidateNotNullOrEmpty()]
    [string] $Filter = 'Category=Browser&BrowserSuite=Compatibility',

    [ValidateRange(1, 16)]
    [int] $ShardIndex = 1,

    [ValidateRange(1, 16)]
    [int] $ShardCount = 1
)

# Product-specific compatibility contracts; HtmlTinkerX owns browser installation and lifetime.
$ErrorActionPreference = 'Stop'
if (-not [IO.Path]::IsPathRooted($OutputRoot)) {
    throw 'OutputRoot must be an explicit absolute task-owned path.'
}
$output = [IO.Path]::GetFullPath($OutputRoot)
$results = Join-Path $output 'results'
$captures = Join-Path $output 'captures'
New-Item -ItemType Directory -Path $results, $captures -Force | Out-Null
$repository = Split-Path -Parent $PSScriptRoot
$project = Join-Path $repository 'ChartForgeX.Tests/ChartForgeX.Tests.csproj'
if (-not $SkipBuild) {
    dotnet build $project -c Release
    if ($LASTEXITCODE) {
        throw 'Compatibility test project build failed.'
    }
}

# Every run lists its cases and proves the TRX ran exactly those, as the Chromium shards do.
. (Join-Path $PSScriptRoot 'Select-BrowserTestShard.ps1')
$selection = Select-BrowserTestShard -Project $project -Configuration Release -Filter $Filter -ShardIndex $ShardIndex -ShardCount $ShardCount `
    -Suite "$($Browser.ToLowerInvariant())-compatibility" -ResultsDirectory $results
if (-not $selection.Filter) { throw "Compatibility shard $ShardIndex/$ShardCount owns no test classes." }
$Filter = $selection.Filter
# Most fixtures install their own HTML; embedding contracts navigate the maintained example pages.
$origin = Join-Path $output 'origin'
New-Item -ItemType Directory -Path $origin -Force | Out-Null
dotnet run --project (Join-Path $repository 'ChartForgeX.Examples/ChartForgeX.Examples.csproj') -c Release --no-build -- --embedding-only --output $origin
if ($LASTEXITCODE) { throw 'Embedding compatibility fixture generation failed.' }
[IO.File]::WriteAllText((Join-Path $origin 'catalog.html'), '<!doctype html><title>Browser test origin</title>')
[IO.File]::WriteAllText((Join-Path $origin 'manifest.json'), '{}')
. (Join-Path $PSScriptRoot 'Invoke-BrowserTestHost.ps1')
$settings = @{
    HFX_TEST_ENABLE_PLAYWRIGHT = '1'
    CFX_TEST_REQUIRE_BROWSER = '1'
    CFX_TEST_BROWSER = $Browser.ToLowerInvariant()
    CFX_V2_GALLERY_URL = $null
    CFX_V2_CAPTURE_ROOT = $captures
    CFX_BROWSER_CAPTURE_DIRECTORY = $captures
}
$saved = @{}
foreach ($name in $settings.Keys) {
    $saved[$name] = [Environment]::GetEnvironmentVariable($name)
}
try {
    Invoke-BrowserTestHost -Root $origin -OutputRoot $output -Action {
        param($galleryUri)
        $settings.CFX_V2_GALLERY_URL = $galleryUri.AbsoluteUri
    foreach ($name in $settings.Keys) {
        [Environment]::SetEnvironmentVariable($name, $settings[$name])
    }
    # Each contract owns a real browser. Keep macOS GPU sessions sequential;
    # concurrent WebKit workers can exhaust the hosted runner's WebGL resources.
    $runSettings = if ($IsMacOS) { 'browser-macos.runsettings' } else { 'browser.runsettings' }
    dotnet test $project -c Release --no-build --no-restore --filter $Filter `
        --settings (Join-Path $PSScriptRoot $runSettings) --logger 'console;verbosity=normal' --logger 'trx;LogFileName=compatibility.trx' --results-directory $results
    if ($LASTEXITCODE) {
        throw "Required $Browser compatibility contracts failed. See $output."
    }
    [xml] $report = Get-Content -LiteralPath (Join-Path $results 'compatibility.trx') -Raw
    $counters = $report.TestRun.ResultSummary.Counters
    if ([int]$counters.total -eq 0 -or [int]$counters.notExecuted -ne 0 -or [int]$counters.executed -ne [int]$counters.total) {
        throw 'Compatibility proof must discover and execute tests without skips.'
    }
    Assert-BrowserShardExecution -Selection $selection -Report $report
    Write-Host "$Browser compatibility proof passed: $($counters.total) executed cases. Evidence: $output"
    }
} finally {
    foreach ($name in $saved.Keys) {
        [Environment]::SetEnvironmentVariable($name, $saved[$name])
    }
}
