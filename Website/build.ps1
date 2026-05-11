[CmdletBinding()]
param(
    [switch] $Dev,
    [switch] $Ci,
    [switch] $Serve,
    [int] $Port = 8021,
    [string] $PowerForgeCli
)

$ErrorActionPreference = 'Stop'

$websiteRoot = $PSScriptRoot

if (-not $PowerForgeCli) {
    $localCli = Join-Path $websiteRoot '..\..\PSPublishModule\PowerForge.Web.Cli\bin\Debug\net8.0\PowerForge.Web.Cli.exe'
    if (Test-Path -LiteralPath $localCli) {
        $PowerForgeCli = (Resolve-Path -LiteralPath $localCli).Path
    } else {
        $PowerForgeCli = 'powerforge-web'
    }
}

$mode = if ($Ci) { 'ci' } else { 'dev' }

Push-Location -LiteralPath $websiteRoot
try {
    & $PowerForgeCli pipeline --config .\pipeline.json --mode $mode
    if ($LASTEXITCODE -ne 0) {
        throw "PowerForge.Web pipeline failed with exit code $LASTEXITCODE."
    }

    if ($Serve) {
        & py -3 -m http.server $Port --bind 127.0.0.1 --directory _site
        if ($LASTEXITCODE -ne 0) {
            throw "Static server failed with exit code $LASTEXITCODE."
        }
    }
} finally {
    Pop-Location
}
