param(
    [string] $SourceRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path,
    [string] $OutputRoot = (Join-Path (Join-Path $PSScriptRoot '..') 'artifacts/website-api'),
    [string] $Version,
    [string] $ReleaseArchivesRoot,
    [switch] $SkipBuild,
    [switch] $ListProjects
)

$ErrorActionPreference = 'Stop'
$sourceRootResolved = (Resolve-Path -LiteralPath $SourceRoot).Path
$buildConfig = Get-Content -LiteralPath (Join-Path $sourceRootResolved 'Build/project.build.json') -Raw | ConvertFrom-Json
$projects = @($buildConfig.ExpectedVersionMap.PSObject.Properties.Name)
if ($ReleaseArchivesRoot -or $ListProjects) {
    if ([string]::IsNullOrWhiteSpace($Version)) {
        throw 'Specify -Version when selecting released API documentation packages.'
    }
}
else {
    $primaryProject = [string] $buildConfig.GitHubPrimaryProject
    $primaryProjectPath = Join-Path $sourceRootResolved "$primaryProject/$primaryProject.csproj"
    $versionOutput = & dotnet msbuild $primaryProjectPath -nologo -getProperty:Version
    if ($LASTEXITCODE -ne 0) {
        throw 'Could not evaluate the ChartForgeX source version.'
    }
    $sourceVersion = ($versionOutput -join "`n").Trim()
    if (-not [string]::IsNullOrWhiteSpace($Version) -and $Version -ne $sourceVersion) {
        throw "Source API documentation version must match the evaluated project version $sourceVersion`: $Version"
    }
    $Version = $sourceVersion
}
if ($Version -notmatch '^\d+\.\d+\.\d+$') {
    throw "Website API bundle requires a public three-part version: $Version"
}
# Visuals and Stories are independent packages from v2; 1.x release backfills contain six archives.
if (([version] $Version).Major -eq 1) {
    $projects = @($projects | Where-Object { $_ -notin @('ChartForgeX.Visuals', 'ChartForgeX.Stories') })
}
if ($ListProjects) {
    Write-Output $projects
    return
}

if (-not $ReleaseArchivesRoot -and -not $SkipBuild) {
    foreach ($project in $projects) {
        $projectPath = Join-Path $sourceRootResolved "$project/$project.csproj"
        & dotnet build $projectPath -c Release -f net10.0 --nologo -v:minimal
        if ($LASTEXITCODE -ne 0) {
            throw "ChartForgeX API documentation build failed for $project."
        }
    }
}

$outputRootResolved = [System.IO.Path]::GetFullPath($OutputRoot)
$releaseArchivesRootResolved = if ($ReleaseArchivesRoot) {
    (Resolve-Path -LiteralPath $ReleaseArchivesRoot).Path
}
$stageRoot = Join-Path $outputRootResolved ('stage-' + [guid]::NewGuid().ToString('N'))
$apiRoot = Join-Path $stageRoot 'WebsiteArtifacts/apidocs/dotnet'
New-Item -ItemType Directory -Path $apiRoot -Force | Out-Null

try {
foreach ($project in $projects) {
    if ($releaseArchivesRootResolved) {
        $archivePath = Join-Path $releaseArchivesRootResolved "$project.$Version.zip"
        if (-not (Test-Path -LiteralPath $archivePath -PathType Leaf)) {
            throw "ChartForgeX release archive is missing: $archivePath"
        }
        $archive = [System.IO.Compression.ZipFile]::OpenRead($archivePath)
        try {
            foreach ($extension in @('.dll', '.xml', '.pdb', '.deps.json')) {
                $entry = $archive.GetEntry("net10.0/$project$extension")
                if (-not $entry) {
                    if ($extension -in @('.dll', '.xml')) {
                        throw "ChartForgeX release archive is missing net10.0/$project$extension`: $archivePath"
                    }
                    continue
                }
                $target = Join-Path $apiRoot "$project$extension"
                $entryStream = $entry.Open()
                try {
                    $targetStream = [System.IO.File]::Create($target)
                    try { $entryStream.CopyTo($targetStream) }
                    finally { $targetStream.Dispose() }
                }
                finally { $entryStream.Dispose() }
            }
            foreach ($entry in $archive.Entries) {
                if (-not $entry.FullName.StartsWith('net10.0/', [StringComparison]::Ordinal) -or
                    -not $entry.Name.EndsWith('.dll', [StringComparison]::OrdinalIgnoreCase) -or
                    [System.IO.Path]::GetFileNameWithoutExtension($entry.Name) -in $projects) {
                    continue
                }
                $target = Join-Path $apiRoot $entry.Name
                if (-not (Test-Path -LiteralPath $target)) {
                    $entryStream = $entry.Open()
                    try {
                        $targetStream = [System.IO.File]::Create($target)
                        try { $entryStream.CopyTo($targetStream) }
                        finally { $targetStream.Dispose() }
                    }
                    finally { $entryStream.Dispose() }
                    continue
                }
                $candidateStream = $entry.Open()
                try {
                    $candidateHash = [System.Security.Cryptography.SHA256]::HashData($candidateStream)
                }
                finally { $candidateStream.Dispose() }
                $existingHash = (Get-FileHash -LiteralPath $target -Algorithm SHA256).Hash
                if ($existingHash -ne [Convert]::ToHexString($candidateHash)) {
                    throw "Conflicting API documentation dependency: $($entry.Name)"
                }
            }
        }
        finally { $archive.Dispose() }
        continue
    }

    $binRoot = Join-Path $sourceRootResolved "$project/bin/Release/net10.0"
    foreach ($extension in @('.dll', '.xml')) {
        $requiredPath = Join-Path $binRoot "$project$extension"
        if (-not (Test-Path -LiteralPath $requiredPath -PathType Leaf)) {
            throw "ChartForgeX API documentation input is missing: $requiredPath"
        }
        Copy-Item -LiteralPath $requiredPath -Destination (Join-Path $apiRoot "$project$extension")
    }
    foreach ($extension in @('.pdb', '.deps.json')) {
        $optionalPath = Join-Path $binRoot "$project$extension"
        if (Test-Path -LiteralPath $optionalPath -PathType Leaf) {
            Copy-Item -LiteralPath $optionalPath -Destination (Join-Path $apiRoot "$project$extension")
        }
    }

    foreach ($dependency in Get-ChildItem -LiteralPath $binRoot -Filter '*.dll' -File) {
        $target = Join-Path $apiRoot $dependency.Name
        if (-not (Test-Path -LiteralPath $target)) {
            Copy-Item -LiteralPath $dependency.FullName -Destination $target
            continue
        }
        $existingHash = (Get-FileHash -LiteralPath $target -Algorithm SHA256).Hash
        $candidateHash = (Get-FileHash -LiteralPath $dependency.FullName -Algorithm SHA256).Hash
        if ($existingHash -ne $candidateHash) {
            throw "Conflicting API documentation dependency: $($dependency.Name)"
        }
    }
}

$bundleManifest = [ordered]@{
    version = $Version
    targetFramework = 'net10.0'
    assemblies = $projects
}
$assemblyFiles = [ordered]@{}
foreach ($project in $projects) {
    foreach ($extension in @('.dll', '.xml')) {
        $fileName = "$project$extension"
        $assemblyFiles[$fileName] = (Get-FileHash -LiteralPath (Join-Path $apiRoot $fileName) -Algorithm SHA256).Hash.ToLowerInvariant()
    }
}
$bundleManifest.assemblyFileSha256 = $assemblyFiles
if ($releaseArchivesRootResolved) {
    $releaseArchives = [ordered]@{}
    foreach ($project in $projects) {
        $archivePath = Join-Path $releaseArchivesRootResolved "$project.$Version.zip"
        $releaseArchives[$project] = (Get-FileHash -LiteralPath $archivePath -Algorithm SHA256).Hash.ToLowerInvariant()
    }
    $bundleManifest.releaseTag = "ChartForgeX-v$Version"
    $bundleManifest.releaseArchiveSha256 = $releaseArchives
}
else {
    $sourceCommit = (& git -C $sourceRootResolved rev-parse HEAD).Trim()
    if ($LASTEXITCODE -ne 0 -or $sourceCommit -notmatch '^[0-9a-f]{40}$') {
        throw 'Could not resolve the ChartForgeX source commit.'
    }
    $bundleManifest.sourceCommit = $sourceCommit
}
$bundleManifestPath = Join-Path $stageRoot 'api-bundle.json'
[System.IO.File]::WriteAllText(
    $bundleManifestPath,
    (($bundleManifest | ConvertTo-Json -Depth 4) + "`n"),
    [System.Text.UTF8Encoding]::new($false))

$zipPath = Join-Path $outputRootResolved "ChartForgeX.ApiDocs.$Version.zip"
$temporaryZipPath = Join-Path $outputRootResolved ('ChartForgeX.ApiDocs.tmp-' + [guid]::NewGuid().ToString('N') + '.zip')
$stream = [System.IO.File]::Open($temporaryZipPath, [System.IO.FileMode]::CreateNew)
try {
    $archive = [System.IO.Compression.ZipArchive]::new($stream, [System.IO.Compression.ZipArchiveMode]::Create, $true)
    try {
        foreach ($file in Get-ChildItem -LiteralPath $stageRoot -Recurse -File | Sort-Object FullName) {
            $relativePath = [System.IO.Path]::GetRelativePath($stageRoot, $file.FullName).Replace('\', '/')
            $entry = $archive.CreateEntry($relativePath, [System.IO.Compression.CompressionLevel]::Optimal)
            $entry.LastWriteTime = [datetimeoffset]::new(1980, 1, 1, 0, 0, 0, [timespan]::Zero)
            $inputStream = [System.IO.File]::OpenRead($file.FullName)
            try {
                $entryStream = $entry.Open()
                try { $inputStream.CopyTo($entryStream) }
                finally { $entryStream.Dispose() }
            }
            finally { $inputStream.Dispose() }
        }
    }
    finally { $archive.Dispose() }
}
finally { $stream.Dispose() }

[System.IO.File]::Move($temporaryZipPath, $zipPath, $true)
Write-Output $zipPath
}
finally {
    if (Test-Path -LiteralPath $stageRoot) {
        Remove-Item -LiteralPath $stageRoot -Recurse
    }
    if ($temporaryZipPath -and (Test-Path -LiteralPath $temporaryZipPath)) {
        Remove-Item -LiteralPath $temporaryZipPath
    }
}
