param(
    [string] $SourceDirectory = (Join-Path $env:TEMP 'chartforgex-azure-stencils'),
    [string] $OutputDirectory = (Join-Path $PSScriptRoot '..\..\assets\topology-icons\microsoft-integration-azure-stencils'),
    [switch] $GeneratePng,
    [int] $PreviewSize = 128
)

$ErrorActionPreference = 'Stop'

$repositoryUrl = 'https://github.com/sandroasp/Microsoft-Integration-and-Azure-Stencils-Pack-for-Visio.git'
$sourceFullPath = [System.IO.Path]::GetFullPath($SourceDirectory)
$outputFullPath = [System.IO.Path]::GetFullPath($OutputDirectory)

if (-not (Test-Path -LiteralPath $sourceFullPath)) {
    git clone --depth 1 $repositoryUrl $sourceFullPath
}

$revision = (git -C $sourceFullPath rev-parse HEAD).Trim()
$project = Join-Path $PSScriptRoot '..\..\ChartForgeX.Tools.IconImport\ChartForgeX.Tools.IconImport.csproj'
$arguments = @(
    'run', '--project', $project, '-c', 'Release', '--',
    '--source', $sourceFullPath,
    '--output', $outputFullPath,
    '--source-revision', $revision,
    '--source-url', 'https://github.com/sandroasp/Microsoft-Integration-and-Azure-Stencils-Pack-for-Visio',
    '--source-license', 'MIT',
    '--source-license-url', 'https://github.com/sandroasp/Microsoft-Integration-and-Azure-Stencils-Pack-for-Visio/blob/master/LICENSE',
    '--source-license-path', 'LICENSE',
    '--vendor', 'Microsoft',
    '--pack-id-prefix', 'microsoft-azure-stencils',
    '--pack-label-prefix', 'Microsoft Azure Stencils',
    '--preview-size', $PreviewSize
)

if ($GeneratePng) {
    $arguments += '--generate-png'
}

dotnet @arguments
