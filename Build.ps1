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
    dotnet test $tests -c $Configuration --no-build --no-restore

    if (-not $SkipExamples) {
        dotnet run --project $examples -c $Configuration --no-build
    }

    if (-not $SkipPack) {
        $packageRoot = Join-Path $root "ChartForgeX/bin/$Configuration"
        if (Test-Path $packageRoot) {
            Get-ChildItem $packageRoot -Filter 'ChartForgeX.*.nupkg' -ErrorAction SilentlyContinue | Remove-Item -Force
            Get-ChildItem $packageRoot -Filter 'ChartForgeX.*.snupkg' -ErrorAction SilentlyContinue | Remove-Item -Force
        }

        dotnet pack $library -c $Configuration --no-build
        $packages = @(Get-ChildItem $packageRoot -Filter 'ChartForgeX.*.nupkg')
        if ($packages.Count -ne 1) {
            throw "Expected exactly one package, found $($packages.Count)."
        }
        $package = $packages[0]
        if (-not $package) {
            throw 'Package was not created.'
        }
        $symbolsPackages = @(Get-ChildItem $packageRoot -Filter 'ChartForgeX.*.snupkg')
        if ($symbolsPackages.Count -ne 1) {
            throw "Expected exactly one symbol package, found $($symbolsPackages.Count)."
        }
        $symbolsPackage = $symbolsPackages[0]
        if (-not $symbolsPackage) {
            throw 'Symbol package was not created.'
        }

        Add-Type -AssemblyName System.IO.Compression.FileSystem
        $archive = [System.IO.Compression.ZipFile]::OpenRead($package.FullName)
        try {
            $nuspecEntry = $archive.Entries | Where-Object { $_.FullName -eq 'ChartForgeX.nuspec' } | Select-Object -First 1
            if (-not $nuspecEntry) {
                throw 'Package is missing ChartForgeX.nuspec.'
            }
            foreach ($requiredEntry in @('README.md', 'CHANGELOG.md')) {
                if (-not ($archive.Entries | Where-Object { $_.FullName -eq $requiredEntry } | Select-Object -First 1)) {
                    throw "Package is missing $requiredEntry."
                }
            }
            foreach ($framework in @('net472', 'netstandard2.0', 'net8.0', 'net10.0')) {
                foreach ($extension in @('dll', 'xml')) {
                    $requiredEntry = "lib/$framework/ChartForgeX.$extension"
                    if (-not ($archive.Entries | Where-Object { $_.FullName -eq $requiredEntry } | Select-Object -First 1)) {
                        throw "Package is missing $requiredEntry."
                    }
                }
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

        $packageVersion = [System.IO.Path]::GetFileNameWithoutExtension($package.Name).Substring('ChartForgeX.'.Length)
        $consumerRoot = Join-Path ([System.IO.Path]::GetTempPath()) "ChartForgeX-package-consumer-$([Guid]::NewGuid().ToString('N'))"
        try {
            New-Item -ItemType Directory -Path $consumerRoot | Out-Null
            Push-Location $consumerRoot
            try {
                dotnet new console --framework net8.0 --no-restore | Out-Null
                @"
<configuration>
  <packageSources>
    <clear />
    <add key="local-chartforgex" value="$packageRoot" />
  </packageSources>
</configuration>
"@ | Set-Content -Path (Join-Path $consumerRoot 'NuGet.config') -Encoding UTF8
                dotnet add package ChartForgeX --version $packageVersion --source $packageRoot | Out-Null
                @"
using ChartForgeX;
using ChartForgeX.Core;
using ChartForgeX.Primitives;

var chart = Chart.Create()
    .WithTitle("Package smoke")
    .WithSize(320, 180)
    .AddLine("Values", new[] { new ChartPoint(1, 2), new ChartPoint(2, 3) });

if (!chart.ToSvg().Contains("<svg", StringComparison.Ordinal)) throw new InvalidOperationException("SVG render failed.");
if (!chart.ToHtmlFragment().Contains("<svg", StringComparison.Ordinal)) throw new InvalidOperationException("HTML render failed.");
if (chart.ToPng().Length <= 64) throw new InvalidOperationException("PNG render failed.");
"@ | Set-Content -Path (Join-Path $consumerRoot 'Program.cs') -Encoding UTF8
                dotnet run -c Release --no-restore | Out-Null
            } finally {
                Pop-Location
            }
        } finally {
            if (Test-Path $consumerRoot) {
                Remove-Item $consumerRoot -Recurse -Force
            }
        }
    }
} finally {
    Pop-Location
}
