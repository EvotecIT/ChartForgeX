<#
.SYNOPSIS
Proves that the shards of each browser suite ran every listed test class exactly once.
.PARAMETER ManifestRoot
A directory containing the shard.json files written by Select-BrowserTestShard.
.PARAMETER Suite
Suites that must be present, for example chromium.
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory)] [string] $ManifestRoot,
    [Parameter(Mandatory)] [string[]] $Suite
)

$ErrorActionPreference = 'Stop'
$manifests = @(Get-ChildItem -LiteralPath $ManifestRoot -Filter 'shard.json' -File -Recurse | ForEach-Object {
    Get-Content -LiteralPath $_.FullName -Raw | ConvertFrom-Json
})
foreach ($name in $Suite) {
    $shards = @($manifests | Where-Object suite -eq $name)
    if (-not $shards) { throw "No shard manifests were retained for the $name suite." }
    $count = $shards[0].shards
    $numbers = @($shards | ForEach-Object shard | Sort-Object)
    if (($numbers -join ',') -ne ((1..$count) -join ',')) { throw "$name retained shards $($numbers -join ', ') of $count." }
    if (@($shards | Where-Object { $_.shards -ne $count -or $_.listingSha256 -ne $shards[0].listingSha256 -or $_.filter -ne $shards[0].filter }).Count) {
        throw "$name shards listed different test sets."
    }
    # The concatenated shard classes must reproduce the complete listing: none is missing or repeated.
    $classes = [string[]] @($shards | ForEach-Object { $_.classes })
    [Array]::Sort($classes, [StringComparer]::Ordinal)
    $digest = [Convert]::ToHexString([Security.Cryptography.SHA256]::HashData([Text.Encoding]::UTF8.GetBytes(($classes -join "`n"))))
    if ($classes.Count -ne $shards[0].listedClasses -or $digest -ne $shards[0].listingSha256) {
        throw "$name shards ran $($classes.Count) classes; the listing has $($shards[0].listedClasses) and every class must run exactly once."
    }
    Write-Host "$name`: $count shards cover all $($classes.Count) listed classes exactly once."
}
