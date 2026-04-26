param(
    [ValidateSet('Debug','Release')]
    [string] $Configuration = 'Release',

    [switch] $SkipExamples,

    [switch] $SkipPack
)

$ErrorActionPreference = 'Stop'
if ($PSVersionTable.PSVersion.Major -ge 7) {
    $PSNativeCommandUseErrorActionPreference = $true
}
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
Push-Location $root
try {
    $solution = Join-Path $root 'ChartForgeX.sln'
    $tests = Join-Path $root 'ChartForgeX.Tests/ChartForgeX.Tests.csproj'
    $examples = Join-Path $root 'ChartForgeX.Examples/ChartForgeX.Examples.csproj'
    $library = Join-Path $root 'ChartForgeX/ChartForgeX.csproj'

    dotnet restore .\ChartForgeX.sln
    dotnet build $solution -c $Configuration --no-restore
    dotnet run --project $tests -c $Configuration --no-build

    if (-not $SkipExamples) {
        dotnet run --project $examples -c $Configuration --no-build
    }

    if (-not $SkipPack) {
        dotnet pack $library -c $Configuration --no-build
        $packageRoot = Join-Path $root "ChartForgeX/bin/$Configuration"
        $package = Get-ChildItem $packageRoot -Filter 'ChartForgeX.*.nupkg' | Sort-Object LastWriteTime -Descending | Select-Object -First 1
        if (-not $package) {
            throw 'Package was not created.'
        }

        Add-Type -AssemblyName System.IO.Compression.FileSystem
        $archive = [System.IO.Compression.ZipFile]::OpenRead($package.FullName)
        try {
            $nuspecEntry = $archive.Entries | Where-Object { $_.FullName -eq 'ChartForgeX.nuspec' } | Select-Object -First 1
            if (-not $nuspecEntry) {
                throw 'Package is missing ChartForgeX.nuspec.'
            }

            $reader = [System.IO.StreamReader]::new($nuspecEntry.Open())
            try {
                $nuspec = $reader.ReadToEnd()
            } finally {
                $reader.Dispose()
            }

            if ($nuspec -match '<dependency\s') {
                throw 'Core package must not contain runtime NuGet dependencies.'
            }
        } finally {
            $archive.Dispose()
        }
    }
} finally {
    Pop-Location
}
