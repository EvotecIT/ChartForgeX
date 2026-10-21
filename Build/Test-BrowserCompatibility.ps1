<#
.SYNOPSIS
Runs the bounded, engine-independent HTML chart compatibility contracts through HtmlTinkerX.
.PARAMETER Browser
The existing HtmlTinkerX engine to qualify. Chromium's complete interaction suite remains a separate gate.
.PARAMETER OutputRoot
An absolute task-owned directory for results and rendered evidence.
.PARAMETER SkipBuild
Uses a previously built Release test assembly.
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [ValidateSet('chromium', 'firefox', 'webkit')]
    [string] $Browser,

    [Parameter(Mandatory = $true)]
    [string] $OutputRoot,

    [switch] $SkipBuild
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

$settings = @{
    HFX_TEST_ENABLE_PLAYWRIGHT = '1'
    CFX_TEST_REQUIRE_BROWSER = '1'
    CFX_TEST_BROWSER = $Browser.ToLowerInvariant()
    CFX_V2_CAPTURE_ROOT = $captures
    CFX_BROWSER_CAPTURE_DIRECTORY = $captures
}
$saved = @{}
foreach ($name in $settings.Keys) {
    $saved[$name] = [Environment]::GetEnvironmentVariable($name)
}
try {
    foreach ($name in $settings.Keys) {
        [Environment]::SetEnvironmentVariable($name, $settings[$name])
    }
    # Each contract owns a real browser. Keep macOS GPU sessions sequential;
    # concurrent WebKit workers can exhaust the hosted runner's WebGL resources.
    $runSettings = if ($IsMacOS) { 'browser-macos.runsettings' } else { 'browser.runsettings' }
    dotnet test $project -c Release --no-build --no-restore --filter 'Category=Browser&BrowserSuite=Compatibility' `
        --settings (Join-Path $PSScriptRoot $runSettings) --logger 'trx;LogFileName=compatibility.trx' --results-directory $results
    if ($LASTEXITCODE) {
        throw "Required $Browser compatibility contracts failed. See $output."
    }
    [xml] $report = Get-Content -LiteralPath (Join-Path $results 'compatibility.trx') -Raw
    $counters = $report.TestRun.ResultSummary.Counters
    if ([int]$counters.total -eq 0 -or [int]$counters.notExecuted -ne 0 -or [int]$counters.executed -ne [int]$counters.total) {
        throw 'Compatibility proof must discover and execute tests without skips.'
    }
    Write-Host "$Browser compatibility proof passed: $($counters.total) executed cases. Evidence: $output"
} finally {
    foreach ($name in $saved.Keys) {
        [Environment]::SetEnvironmentVariable($name, $saved[$name])
    }
}
