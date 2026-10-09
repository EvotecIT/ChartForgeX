param(
    [Parameter(Mandatory = $true)]
    [string] $BundlePath,
    [Parameter(Mandatory = $true)]
    [string] $Version,
    [string] $ReleaseArchivesRoot,
    [string] $SourceRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
)

$ErrorActionPreference = 'Stop'
$projects = @( & (Join-Path $PSScriptRoot 'Export-WebsiteApiDocs.ps1') -SourceRoot $SourceRoot -Version $Version -ListProjects )
$expectedFiles = @($projects | ForEach-Object { "${_}.dll"; "${_}.xml" })
$bundle = [IO.Compression.ZipFile]::OpenRead((Resolve-Path -LiteralPath $BundlePath).Path)

function Get-EntryHash($Archive, [string] $EntryPath) {
    $entry = $Archive.GetEntry($EntryPath)
    if (-not $entry -or $entry.Length -eq 0) { throw "API documentation entry is missing or empty: $EntryPath" }
    $stream = $entry.Open()
    try { return [Convert]::ToHexString([Security.Cryptography.SHA256]::HashData($stream)).ToLowerInvariant() }
    finally { $stream.Dispose() }
}

try {
    $manifestEntry = $bundle.GetEntry('api-bundle.json')
    if (-not $manifestEntry) { throw 'API documentation bundle has no api-bundle.json receipt.' }
    $reader = [IO.StreamReader]::new($manifestEntry.Open())
    try { $manifest = $reader.ReadToEnd() | ConvertFrom-Json }
    finally { $reader.Dispose() }
    if ($manifest.version -ne $Version -or $manifest.targetFramework -ne 'net10.0') {
        throw 'API documentation bundle version or target framework does not match.'
    }
    $assemblies = @($manifest.assemblies)
    if ($assemblies.Count -ne $projects.Count -or (Compare-Object $projects $assemblies -CaseSensitive)) {
        throw 'API documentation bundle does not contain the complete release assembly set.'
    }
    $receiptedFiles = @($manifest.assemblyFileSha256.PSObject.Properties.Name)
    if ($receiptedFiles.Count -ne $expectedFiles.Count -or (Compare-Object $expectedFiles $receiptedFiles -CaseSensitive)) {
        throw 'API documentation bundle does not receipt every required assembly and XML documentation file.'
    }
    foreach ($fileName in $expectedFiles) {
        $hash = Get-EntryHash $bundle "WebsiteArtifacts/apidocs/dotnet/$fileName"
        if ($hash -cne $manifest.assemblyFileSha256.$fileName) {
            throw "API documentation payload does not match its hash receipt: $fileName"
        }
    }

    if ($manifest.releaseTag) {
        $receiptedArchives = @($manifest.releaseArchiveSha256.PSObject.Properties.Name)
        if ($manifest.releaseTag -ne "ChartForgeX-v$Version" -or
            $receiptedArchives.Count -ne $projects.Count -or (Compare-Object $projects $receiptedArchives -CaseSensitive)) {
            throw 'API documentation bundle release archive receipt does not match the release.'
        }
        foreach ($project in $projects) {
            if ($manifest.releaseArchiveSha256.$project -cnotmatch '^[0-9a-f]{64}$') {
                throw "API documentation release archive hash is invalid: $project"
            }
            if (-not $ReleaseArchivesRoot) { continue }
            $archivePath = Join-Path $ReleaseArchivesRoot "$project.$Version.zip"
            $hash = (Get-FileHash -LiteralPath $archivePath -Algorithm SHA256).Hash.ToLowerInvariant()
            if ($hash -cne $manifest.releaseArchiveSha256.$project) {
                throw "API documentation bundle release archive hash does not match: $project"
            }
            $archive = [IO.Compression.ZipFile]::OpenRead((Resolve-Path -LiteralPath $archivePath).Path)
            try {
                foreach ($extension in @('.dll', '.xml')) {
                    $fileName = "$project$extension"
                    $hash = Get-EntryHash $archive "net10.0/$fileName"
                    if ($hash -cne $manifest.assemblyFileSha256.$fileName) {
                        throw "API documentation payload differs from the released assembly input: $fileName"
                    }
                }
            }
            finally { $archive.Dispose() }
        }
    }
    elseif ($ReleaseArchivesRoot -or $manifest.sourceCommit -cnotmatch '^[0-9a-f]{40}$') {
        throw 'API documentation bundle is missing the expected source or release identity.'
    }
}
finally { $bundle.Dispose() }

Write-Output "API documentation bundle passed: $BundlePath ($($projects.Count) assemblies, $($expectedFiles.Count) DLL/XML hashes)."
