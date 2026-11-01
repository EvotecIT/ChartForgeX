<#
.SYNOPSIS
Checks the embedded visual tokens against the HtmlForgeX generated contract.
.DESCRIPTION
Uses an explicit generated file, the sibling HtmlForgeX checkout, or the newest
installed HtmlForgeX package carrying the contract. Compares the token version
and SHA-256 of LF-normalized UTF-8 content. With no source available, reports an
explicit skip; it never downloads packages or modifies either copy.
.PARAMETER SourcePath
Generated HtmlForgeX token file. An explicitly selected missing file is an error.
.EXAMPLE
./Build/Test-DesignTokenDrift.ps1
.EXAMPLE
./Build/Test-DesignTokenDrift.ps1 -SourcePath ../HtmlForgeX/Generated/Tokens/ChartForgeX/evotec.chartforgex.tokens.json
.OUTPUTS
A verification or skip message; throws on a stale or invalid contract.
#>
[CmdletBinding()]
param([string] $SourcePath = $env:CHARTFORGEX_TOKEN_SOURCE)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$relative = 'Generated/Tokens/ChartForgeX/evotec.chartforgex.tokens.json'
$embeddedPath = Join-Path $root 'ChartForgeX/Themes/Tokens/evotec.chartforgex.tokens.json'

if ($SourcePath) {
    $SourcePath = (Get-Item -LiteralPath $SourcePath -ErrorAction Stop).FullName
} else {
    $candidates = @(Join-Path (Split-Path -Parent $root) "HtmlForgeX/$relative")
    # A linked checkout's common Git directory identifies the ordinary sibling root.
    if ((Test-Path -LiteralPath (Join-Path $root '.git')) -and (Get-Command git -ErrorAction SilentlyContinue)) {
        $commonGit = & git -C $root rev-parse --path-format=absolute --git-common-dir
        if ($LASTEXITCODE -eq 0) {
            $repoParent = Split-Path -Parent (Split-Path -Parent $commonGit)
            $candidates += Join-Path $repoParent "HtmlForgeX/$relative"
        }
    }
    $SourcePath = $candidates | Where-Object { Test-Path -LiteralPath $_ -PathType Leaf } | Select-Object -First 1
    if (-not $SourcePath) {
        $packageRoot = if ($env:NUGET_PACKAGES) { $env:NUGET_PACKAGES } else { Join-Path $HOME '.nuget/packages' }
        $packageRoot = Join-Path $packageRoot 'htmlforgex'
        if (Test-Path -LiteralPath $packageRoot) {
            $SourcePath = Get-ChildItem -LiteralPath $packageRoot -Directory |
                Where-Object { $_.Name -match '^\d+\.\d+\.\d+$' } |
                Sort-Object { [version]$_.Name } -Descending |
                ForEach-Object { Join-Path $_.FullName 'contentFiles/any/any/DesignTokens/evotec.chartforgex.tokens.json' } |
                Where-Object { Test-Path -LiteralPath $_ -PathType Leaf } | Select-Object -First 1
        }
    }
}

if (-not $SourcePath) {
    Write-Host 'SKIP: visual token drift check; no HtmlForgeX generated sibling or package contract is available.'
    return
}

function Read-TokenIdentity {
    param([string] $Path)
    $content = [IO.File]::ReadAllText($Path).Replace("`r`n", "`n").Replace("`r", "`n")
    $document = $content | ConvertFrom-Json
    $version = $document.version
    if (-not $version -and $document.source -match ' (\d+\.\d+\.\d+) \(generated') {
        $version = $Matches[1]
    }
    if ($version -notmatch '^\d+\.\d+\.\d+$') { throw "Visual token contract requires a three-part version: $Path" }
    $sha = [Security.Cryptography.SHA256]::Create()
    try {
        $hash = [BitConverter]::ToString($sha.ComputeHash([Text.Encoding]::UTF8.GetBytes($content))).Replace('-', '').ToLowerInvariant()
    } finally {
        $sha.Dispose()
    }
    [pscustomobject]@{ Version = [string]$version; Hash = $hash }
}

$embedded = Read-TokenIdentity $embeddedPath
$generated = Read-TokenIdentity $SourcePath
if ($embedded.Version -cne $generated.Version -or $embedded.Hash -cne $generated.Hash) {
    throw "Visual token drift: embedded $($embedded.Version) [$($embedded.Hash)] differs from HtmlForgeX $($generated.Version) [$($generated.Hash)]. Refresh the embedded file from $SourcePath."
}
Write-Host "Visual token contract verified: $($embedded.Version), SHA-256 $($embedded.Hash)."
