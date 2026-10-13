param(
    [Parameter(Mandatory = $true)]
    [string] $OutputRoot,
    [switch] $SkipBuild
)

$ErrorActionPreference = 'Stop'
if (-not [IO.Path]::IsPathRooted($OutputRoot)) { throw 'OutputRoot must be an explicit absolute task-owned path.' }
$repository = Split-Path -Parent $PSScriptRoot
$output = [IO.Path]::GetFullPath($OutputRoot)
$gallery = Join-Path $output 'gallery'
$captures = Join-Path $output 'captures'
$results = Join-Path $output 'results'
New-Item -ItemType Directory -Path $output, $captures, $results -Force | Out-Null
$project = Join-Path $repository 'ChartForgeX.Tests/ChartForgeX.Tests.csproj'
if (-not $SkipBuild) {
    dotnet build $project -c Release
    if ($LASTEXITCODE) { throw 'Browser test project build failed.' }
}
dotnet run --project (Join-Path $repository 'ChartForgeX.Examples/ChartForgeX.Examples.csproj') -c Release --no-build -- --v2-only --output $gallery
if ($LASTEXITCODE) { throw 'Fresh browser gallery generation failed.' }
if (-not (Test-Path -LiteralPath (Join-Path $gallery 'manifest.json'))) { throw 'The generated gallery has no manifest.' }

$addressFile = Join-Path $output 'gallery-address.txt'
[IO.File]::WriteAllText($addressFile, '')
$start = [Diagnostics.ProcessStartInfo]::new('node')
$start.CreateNoWindow = $true
$start.UseShellExecute = $false
$start.RedirectStandardOutput = $true
$start.RedirectStandardError = $true
$start.ArgumentList.Add((Join-Path $PSScriptRoot 'Serve-BrowserGallery.cjs'))
$start.ArgumentList.Add($gallery)
$start.ArgumentList.Add($addressFile)
$server = [Diagnostics.Process]::Start($start)
$variables = @('HFX_TEST_ENABLE_PLAYWRIGHT', 'CFX_TEST_REQUIRE_BROWSER', 'CFX_V2_GALLERY_URL', 'CFX_V2_CAPTURE_ROOT', 'CFX_BROWSER_CAPTURE_DIRECTORY')
$saved = @{}
foreach ($name in $variables) { $saved[$name] = [Environment]::GetEnvironmentVariable($name) }
try {
    $deadline = [DateTime]::UtcNow.AddSeconds(15)
    while ([IO.File]::ReadAllText($addressFile).Length -eq 0) {
        if ($server.HasExited) { throw ('Gallery server failed: ' + $server.StandardError.ReadToEnd()) }
        if ([DateTime]::UtcNow -gt $deadline) { throw 'Gallery server did not become ready within 15 seconds.' }
        Start-Sleep -Milliseconds 100
    }
    $env:HFX_TEST_ENABLE_PLAYWRIGHT = '1'
    $env:CFX_TEST_REQUIRE_BROWSER = '1'
    $env:CFX_V2_GALLERY_URL = [IO.File]::ReadAllText($addressFile)
    $env:CFX_V2_CAPTURE_ROOT = $captures
    $env:CFX_BROWSER_CAPTURE_DIRECTORY = $captures
    Invoke-WebRequest -Uri $env:CFX_V2_GALLERY_URL -TimeoutSec 10 | Out-Null
    # HtmlTinkerX installs and opens Chromium through the shared test session when it is first needed.
    dotnet test $project -c Release --no-build --no-restore --filter 'Category=Browser' `
        --settings (Join-Path $PSScriptRoot 'browser.runsettings') --logger 'trx;LogFileName=browser.trx' --results-directory $results
    if ($LASTEXITCODE) { throw 'Required browser contracts failed. See the retained TRX, captures and console evidence.' }
    [xml] $report = Get-Content -LiteralPath (Join-Path $results 'browser.trx') -Raw
    $counters = $report.TestRun.ResultSummary.Counters
    if ([int]$counters.total -eq 0 -or [int]$counters.notExecuted -ne 0 -or [int]$counters.executed -ne [int]$counters.total) {
        throw 'Required browser proof must discover and execute tests without skips.'
    }
    if (-not (Test-Path -LiteralPath (Join-Path $captures 'native-family-matrix.json'))) { throw 'Native family matrix evidence is missing.' }
    Write-Host "Required browser proof passed: $($counters.total) executed tests. Evidence: $output"
} finally {
    foreach ($name in $variables) { [Environment]::SetEnvironmentVariable($name, $saved[$name]) }
    if (-not $server.HasExited) { $server.Kill(); $server.WaitForExit() }
    [IO.File]::WriteAllText((Join-Path $output 'gallery-server.log'), $server.StandardOutput.ReadToEnd() + $server.StandardError.ReadToEnd())
    $server.Dispose()
}
