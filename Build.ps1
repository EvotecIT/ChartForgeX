param(
    [ValidateSet('Debug','Release')]
    [string] $Configuration = 'Release',

    [switch] $SkipExamples,

    [switch] $SkipPack,

    [switch] $UpdateVisualBaseline,

    [ValidateRange(1, 86400)]
    [int] $DotNetCommandTimeoutSeconds = 900,

    [ValidateRange(1, 3600)]
    [int] $PackageConsumerTimeoutSeconds = 180
)

function Assert-VisualComparisonHealth {
    param(
        [Parameter(Mandatory = $true)] $Comparison,
        [Parameter(Mandatory = $true)] [string] $ComparisonManifest
    )

    $minimumChartPairs = 50
    if ($Comparison.chartPairs -lt $minimumChartPairs) {
        throw "SVG/PNG comparison generated only $($Comparison.chartPairs) chart pair(s); expected at least $minimumChartPairs. See $ComparisonManifest."
    }

    if ($Comparison.dimensionMatches -ne $Comparison.chartPairs) {
        throw "SVG/PNG comparison has $($Comparison.dimensionMatches) dimension-matched chart pair(s) out of $($Comparison.chartPairs). See $ComparisonManifest."
    }

    if ($Comparison.healthySvgs -ne $Comparison.chartPairs -or $Comparison.healthyPngs -ne $Comparison.chartPairs) {
        throw "SVG/PNG comparison health is incomplete: $($Comparison.healthySvgs) SVG(s), $($Comparison.healthyPngs) PNG(s), $($Comparison.chartPairs) chart pair(s). See $ComparisonManifest."
    }

    if ($Comparison.warnings -ne 0) {
        throw "SVG/PNG comparison reported $($Comparison.warnings) visual warning(s). See $ComparisonManifest."
    }
}

function New-VisualBaseline {
    param(
        [Parameter(Mandatory = $true)] $Comparison
    )

    $updatedCharts = foreach ($chart in $Comparison.charts) {
        [ordered]@{
            name = $chart.name
            width = [int]$chart.svg.width
            height = [int]$chart.svg.height
            svg = [ordered]@{
                minVisualNodes = [int][Math]::Max(2, [int][Math]::Floor([double]$chart.svg.visualNodes * 0.5))
                maxClippedTextNodes = [int]$chart.svg.clippedTextNodes
                maxNearEdgeTextNodes = [int]$chart.svg.nearEdgeTextNodes
            }
            png = [ordered]@{
                outputScale = [int]$chart.png.scale
                minVisiblePixels = [long][Math]::Max(64, [long][Math]::Floor([double]$chart.png.visiblePixels * 0.5))
                minDistinctColors = [int][Math]::Max(8, [int][Math]::Floor([double]$chart.png.distinctColors * 0.5))
                maxEdgeInkPixels = [long]$chart.png.edgeInkPixels
            }
        }
    }

    [ordered]@{
        version = 1
        charts = @($updatedCharts)
    }
}

function Update-VisualBaseline {
    param(
        [Parameter(Mandatory = $true)] $Comparison,
        [Parameter(Mandatory = $true)] [string] $VisualBaselinePath
    )

    New-VisualBaseline -Comparison $Comparison | ConvertTo-Json -Depth 8 | Set-Content -Path $VisualBaselinePath -Encoding UTF8
    Write-Host "Updated SVG/PNG visual baseline: $VisualBaselinePath"
}

function Assert-VisualBaseline {
    param(
        [Parameter(Mandatory = $true)] $Comparison,
        [Parameter(Mandatory = $true)] [string] $VisualBaselinePath,
        [Parameter(Mandatory = $true)] [string] $ComparisonManifest
    )

    if (-not (Test-Path $VisualBaselinePath)) {
        throw "SVG/PNG visual baseline was not found: $VisualBaselinePath"
    }

    $visualBaseline = Get-Content -Path $VisualBaselinePath -Raw | ConvertFrom-Json
    $generatedCharts = @{}
    foreach ($chart in $Comparison.charts) {
        $generatedCharts[$chart.name] = $chart
    }

    $baselineCharts = @{}
    foreach ($expected in $visualBaseline.charts) {
        $baselineCharts[$expected.name] = $expected
        if (-not $generatedCharts.ContainsKey($expected.name)) {
            throw "SVG/PNG baseline chart is missing from generated comparison: $($expected.name). See $ComparisonManifest."
        }

        $actual = $generatedCharts[$expected.name]
        $expectedScale = if ($expected.png.PSObject.Properties.Name -contains 'outputScale') { [int]$expected.png.outputScale } else { 1 }
        if ($actual.svg.width -ne $expected.width -or $actual.svg.height -ne $expected.height -or $actual.png.width -ne ($expected.width * $expectedScale) -or $actual.png.height -ne ($expected.height * $expectedScale)) {
            throw "SVG/PNG baseline dimensions changed for $($expected.name). Expected $($expected.width)x$($expected.height) at PNG scale $expectedScale. See $ComparisonManifest."
        }

        if ($actual.svg.visualNodes -lt $expected.svg.minVisualNodes) {
            throw "SVG visual-node baseline dropped for $($expected.name): $($actual.svg.visualNodes) < $($expected.svg.minVisualNodes). See $ComparisonManifest."
        }

        $maxClippedTextNodes = if ($expected.svg.PSObject.Properties.Name -contains 'maxClippedTextNodes') { [int]$expected.svg.maxClippedTextNodes } else { 0 }
        $maxNearEdgeTextNodes = if ($expected.svg.PSObject.Properties.Name -contains 'maxNearEdgeTextNodes') { [int]$expected.svg.maxNearEdgeTextNodes } else { [int]::MaxValue }
        if ($actual.svg.clippedTextNodes -gt $maxClippedTextNodes -or $actual.svg.nearEdgeTextNodes -gt $maxNearEdgeTextNodes) {
            throw "SVG text-edge baseline regressed for $($expected.name): clipped $($actual.svg.clippedTextNodes), near-edge $($actual.svg.nearEdgeTextNodes). See $ComparisonManifest."
        }

        if ($actual.png.visiblePixels -lt $expected.png.minVisiblePixels -or $actual.png.distinctColors -lt $expected.png.minDistinctColors) {
            throw "PNG visibility baseline dropped for $($expected.name): $($actual.png.visiblePixels) visible pixel(s), $($actual.png.distinctColors) color(s). See $ComparisonManifest."
        }

        $maxEdgeInkPixels = if ($expected.png.PSObject.Properties.Name -contains 'maxEdgeInkPixels') { [long]$expected.png.maxEdgeInkPixels } else { 0 }
        if ($actual.png.edgeInkPixels -gt $maxEdgeInkPixels) {
            throw "PNG edge-pressure baseline regressed for $($expected.name): $($actual.png.edgeInkPixels) edge-ink pixel(s). See $ComparisonManifest."
        }
    }

    foreach ($actual in $Comparison.charts) {
        if (-not $baselineCharts.ContainsKey($actual.name)) {
            throw "SVG/PNG generated chart is missing from visual baseline: $($actual.name). Update $VisualBaselinePath."
        }
    }
}

function Assert-TopologyVisualCoverage {
    param(
        [Parameter(Mandatory = $true)] [string] $TopologyOutput
    )

    $manifestPath = Join-Path $TopologyOutput 'visual-capability-manifest.json'
    if (-not (Test-Path $manifestPath)) {
        throw "Topology visual coverage manifest was not generated: $manifestPath"
    }

    $manifest = Get-Content -Path $manifestPath -Raw | ConvertFrom-Json
    if ([string]::IsNullOrWhiteSpace([string]$manifest.baselinePolicy) -or [string]$manifest.baselinePolicy -notlike '*outside visual-baseline.json*') {
        throw "Topology visual coverage manifest must document why topology artifacts are gated outside visual-baseline.json. See $manifestPath."
    }

    if ([string]$manifest.baselineScope -ne 'visual-capability-manifest') {
        throw "Topology visual coverage manifest must use baselineScope 'visual-capability-manifest'. See $manifestPath."
    }

    $baselineCandidates = @($manifest.baselineCandidates)
    if ($baselineCandidates.Count -lt 4 -or -not ($baselineCandidates -contains 'visual-geographic-topology-map')) {
        throw "Topology visual coverage manifest must list topology baseline candidates for future numeric baselines. See $manifestPath."
    }

    $artifacts = @($manifest.artifacts)
    if ($artifacts.Count -lt 12) {
        throw "Topology visual coverage generated only $($artifacts.Count) artifact(s); expected at least 12. See $manifestPath."
    }

    $artifactNames = @{}
    foreach ($artifact in $artifacts) {
        $artifactNames[$artifact.name] = $artifact
        foreach ($extension in @('svg', 'html', 'png')) {
            $fileName = [string]$artifact.$extension
            if ([string]::IsNullOrWhiteSpace($fileName)) {
                throw "Topology visual artifact $($artifact.name) is missing a $extension file entry. See $manifestPath."
            }

            $filePath = Join-Path $TopologyOutput $fileName
            if (-not (Test-Path $filePath)) {
                throw "Topology visual artifact file was not generated: $filePath"
            }

            if ($extension -eq 'png' -and (Get-Item $filePath).Length -le 64) {
                throw "Topology visual PNG artifact is unexpectedly small: $filePath"
            }
        }
    }

    foreach ($requiredName in @(
        'visual-topology-explorer',
        'visual-replication-mesh-explorer',
        'visual-subnets-site-links-map',
        'visual-geographic-topology-map',
        'visual-geographic-region-map',
        'visual-site-distribution-map',
        'visual-wan-latency-map'
    )) {
        if (-not $artifactNames.ContainsKey($requiredName)) {
            throw "Topology visual coverage manifest is missing required artifact: $requiredName. See $manifestPath."
        }
    }

    $geographicSvg = Join-Path $TopologyOutput 'visual-geographic-topology-map.svg'
    $geographicSource = Get-Content -Path $geographicSvg -Raw
    foreach ($requiredFragment in @(
        'data-layout-mode="Geographic"',
        'data-cfx-role="topology-geographic-frame"',
        'data-route-curve="geographic"',
        'data-route-control-x',
        'data-node-longitude',
        'data-node-geo-visible',
        'data-cfx-visual-role="topology-geographic-callout"',
        'data-callout-node-count'
    )) {
        if (-not $geographicSource.Contains($requiredFragment)) {
            throw "Topology geographic visual artifact is missing required SVG fragment '$requiredFragment'. See $geographicSvg."
        }
    }
}

function Join-ProcessArguments {
    param(
        [Parameter(Mandatory = $true)] [string[]] $Arguments
    )

    $escaped = foreach ($argument in $Arguments) {
        if ($argument -match '^[A-Za-z0-9_./:\\=-]+$') {
            $argument
        } else {
            '"' + ($argument -replace '(\\*)"', '$1$1\"' -replace '(\\+)$', '$1$1') + '"'
        }
    }

    $escaped -join ' '
}

function Invoke-DotNetCommand {
    param(
        [Parameter(Mandatory = $true)] [string[]] $Arguments,
        [Parameter(Mandatory = $true)] [string] $Description,
        [Parameter(Mandatory = $true)] [int] $TimeoutSeconds,
        [switch] $Quiet
    )

    $startInfo = [System.Diagnostics.ProcessStartInfo]::new()
    $startInfo.FileName = 'dotnet'
    $startInfo.WorkingDirectory = (Get-Location).ProviderPath
    $startInfo.UseShellExecute = $false
    $startInfo.RedirectStandardOutput = [bool]$Quiet
    $startInfo.RedirectStandardError = [bool]$Quiet
    if ($null -ne $startInfo.ArgumentList) {
        foreach ($argument in $Arguments) {
            [void] $startInfo.ArgumentList.Add($argument)
        }
    } else {
        $startInfo.Arguments = Join-ProcessArguments -Arguments $Arguments
    }

    $process = [System.Diagnostics.Process]::Start($startInfo)
    $standardOutput = $null
    $standardError = $null
    if ($Quiet) {
        $standardOutput = $process.StandardOutput.ReadToEndAsync()
        $standardError = $process.StandardError.ReadToEndAsync()
    }

    if (-not $process.WaitForExit($TimeoutSeconds * 1000)) {
        try {
            $process.Kill($true)
        } catch {
            $process.Kill()
        }

        throw "$Description timed out after $TimeoutSeconds second(s): dotnet $($Arguments -join ' ')"
    }

    if ($process.ExitCode -ne 0) {
        if ($Quiet) {
            $output = $standardOutput.GetAwaiter().GetResult()
            $errorOutput = $standardError.GetAwaiter().GetResult()
            $tail = (($output, $errorOutput) -join [Environment]::NewLine).Trim()
            if ($tail.Length -gt 0) {
                throw "$Description failed with exit code $($process.ExitCode): $tail"
            }
        }

        throw "$Description failed with exit code $($process.ExitCode)."
    }
}

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
    $interactivityLibrary = Join-Path $root 'ChartForgeX.Interactivity/ChartForgeX.Interactivity.csproj'
    $htmlInteractivityLibrary = Join-Path $root 'ChartForgeX.Interactivity.Html/ChartForgeX.Interactivity.Html.csproj'
    if ($SkipExamples -and $UpdateVisualBaseline) {
        throw 'Visual baseline updates require examples to run. Remove -SkipExamples.'
    }

    Invoke-DotNetCommand -Arguments @('restore', '.\ChartForgeX.sln') -Description 'Solution restore' -TimeoutSeconds $DotNetCommandTimeoutSeconds
    Invoke-DotNetCommand -Arguments @('build', $solution, '-c', $Configuration, '--no-restore') -Description 'Solution build' -TimeoutSeconds $DotNetCommandTimeoutSeconds
    Invoke-DotNetCommand -Arguments @('test', $tests, '-c', $Configuration, '--no-build', '--no-restore') -Description 'Test run' -TimeoutSeconds $DotNetCommandTimeoutSeconds

    if (-not $SkipExamples) {
        Invoke-DotNetCommand -Arguments @('run', '--project', $examples, '-c', $Configuration, '--no-build') -Description 'Example generation' -TimeoutSeconds $DotNetCommandTimeoutSeconds
        $comparisonManifest = Join-Path $root "ChartForgeX.Examples/bin/$Configuration/net8.0/output/svg-png-comparison.json"
        if (-not (Test-Path $comparisonManifest)) {
            throw "SVG/PNG comparison manifest was not generated: $comparisonManifest"
        }

        $comparison = Get-Content -Path $comparisonManifest -Raw | ConvertFrom-Json
        Assert-VisualComparisonHealth -Comparison $comparison -ComparisonManifest $comparisonManifest
        $visualBaselinePath = Join-Path $root "ChartForgeX.Examples/visual-baseline.json"
        if ($UpdateVisualBaseline) {
            Update-VisualBaseline -Comparison $comparison -VisualBaselinePath $visualBaselinePath
        }

        Assert-VisualBaseline -Comparison $comparison -VisualBaselinePath $visualBaselinePath -ComparisonManifest $comparisonManifest
        $topologyOutput = Join-Path $root "ChartForgeX.Examples/bin/$Configuration/net8.0/output/topology-demo"
        Assert-TopologyVisualCoverage -TopologyOutput $topologyOutput
    }

    if (-not $SkipPack) {
        $packageRoot = Join-Path $root "artifacts/packages/$Configuration"
        if (Test-Path $packageRoot) {
            Get-ChildItem $packageRoot -Filter 'ChartForgeX*.nupkg' -ErrorAction SilentlyContinue | Remove-Item -Force
            Get-ChildItem $packageRoot -Filter 'ChartForgeX*.snupkg' -ErrorAction SilentlyContinue | Remove-Item -Force
        } else {
            New-Item -ItemType Directory -Path $packageRoot | Out-Null
        }

        $packageProjects = @(
            [ordered]@{ Id = 'ChartForgeX'; Project = $library; Assembly = 'ChartForgeX'; Nuspec = 'ChartForgeX.nuspec'; DependencyIds = @(); RequiresDependencyFreeNuspec = $true },
            [ordered]@{ Id = 'ChartForgeX.Interactivity'; Project = $interactivityLibrary; Assembly = 'ChartForgeX.Interactivity'; Nuspec = 'ChartForgeX.Interactivity.nuspec'; DependencyIds = @(); RequiresDependencyFreeNuspec = $true },
            [ordered]@{ Id = 'ChartForgeX.Interactivity.Html'; Project = $htmlInteractivityLibrary; Assembly = 'ChartForgeX.Interactivity.Html'; Nuspec = 'ChartForgeX.Interactivity.Html.nuspec'; DependencyIds = @('ChartForgeX', 'ChartForgeX.Interactivity'); RequiresDependencyFreeNuspec = $false }
        )

        foreach ($packageProject in $packageProjects) {
            Invoke-DotNetCommand -Arguments @('pack', $packageProject.Project, '-c', $Configuration, '--no-build', '--output', $packageRoot) -Description "$($packageProject.Id) package creation" -TimeoutSeconds $DotNetCommandTimeoutSeconds
        }

        $packages = @(Get-ChildItem $packageRoot -Filter 'ChartForgeX*.nupkg' | Sort-Object Name)
        if ($packages.Count -ne $packageProjects.Count) {
            throw "Expected $($packageProjects.Count) packages, found $($packages.Count)."
        }
        $symbolsPackages = @(Get-ChildItem $packageRoot -Filter 'ChartForgeX*.snupkg' | Sort-Object Name)
        if ($symbolsPackages.Count -ne $packageProjects.Count) {
            throw "Expected $($packageProjects.Count) symbol packages, found $($symbolsPackages.Count)."
        }

        Add-Type -AssemblyName System.IO.Compression.FileSystem
        $htmlPackageVersion = $null
        foreach ($packageProject in $packageProjects) {
            [xml] $projectXml = Get-Content -Path $packageProject.Project
            $packageVersion = [string] $projectXml.Project.PropertyGroup.Version
            if ([string]::IsNullOrWhiteSpace($packageVersion)) {
                throw "Package version is missing for $($packageProject.Project)."
            }

            $package = Get-Item (Join-Path $packageRoot "$($packageProject.Id).$packageVersion.nupkg")
            $symbolsPackage = Get-Item (Join-Path $packageRoot "$($packageProject.Id).$packageVersion.snupkg")
            if (-not $package) {
                throw "Package was not created for $($packageProject.Id)."
            }
            if (-not $symbolsPackage) {
                throw "Symbol package was not created for $($packageProject.Id)."
            }

            $archive = [System.IO.Compression.ZipFile]::OpenRead($package.FullName)
            try {
                $nuspecEntry = $archive.Entries | Where-Object { $_.FullName -eq $packageProject.Nuspec } | Select-Object -First 1
                if (-not $nuspecEntry) {
                    throw "Package is missing $($packageProject.Nuspec)."
                }
                foreach ($requiredEntry in @('README.md', 'CHANGELOG.md')) {
                    if (-not ($archive.Entries | Where-Object { $_.FullName -eq $requiredEntry } | Select-Object -First 1)) {
                        throw "$($packageProject.Id) package is missing $requiredEntry."
                    }
                }
                foreach ($framework in @('net472', 'netstandard2.0', 'net8.0', 'net10.0')) {
                    foreach ($extension in @('dll', 'xml')) {
                        $requiredEntry = "lib/$framework/$($packageProject.Assembly).$extension"
                        if (-not ($archive.Entries | Where-Object { $_.FullName -eq $requiredEntry } | Select-Object -First 1)) {
                            throw "$($packageProject.Id) package is missing $requiredEntry."
                        }
                    }
                }

                $reader = [System.IO.StreamReader]::new($nuspecEntry.Open())
                try {
                    $nuspec = $reader.ReadToEnd()
                } finally {
                    $reader.Dispose()
                }

                if ($packageProject.RequiresDependencyFreeNuspec -and $nuspec -match '<dependency\s') {
                    throw "$($packageProject.Id) package must not contain runtime NuGet dependencies."
                }
                foreach ($dependencyId in $packageProject.DependencyIds) {
                    if ($nuspec -notmatch ('<dependency\s+id="' + [Regex]::Escape($dependencyId) + '"')) {
                        throw "$($packageProject.Id) package is missing dependency on $dependencyId."
                    }
                }
            } finally {
                $archive.Dispose()
            }

            if ($packageProject.Id -eq 'ChartForgeX.Interactivity.Html') {
                $htmlPackageVersion = $packageVersion
            }
        }

        $consumerRoot = Join-Path ([System.IO.Path]::GetTempPath()) "ChartForgeX-package-consumer-$([Guid]::NewGuid().ToString('N'))"
        try {
            New-Item -ItemType Directory -Path $consumerRoot | Out-Null
            Push-Location $consumerRoot
            try {
                Invoke-DotNetCommand -Arguments @('new', 'console', '--framework', 'net8.0', '--no-restore') -Description 'Package consumer project creation' -TimeoutSeconds $DotNetCommandTimeoutSeconds -Quiet
                @"
<configuration>
  <config>
    <add key="globalPackagesFolder" value="$consumerRoot\.nuget-packages" />
  </config>
  <packageSources>
    <clear />
    <add key="local-chartforgex" value="$packageRoot" />
  </packageSources>
</configuration>
"@ | Set-Content -Path (Join-Path $consumerRoot 'NuGet.config') -Encoding UTF8
                Invoke-DotNetCommand -Arguments @('add', 'package', 'ChartForgeX.Interactivity.Html', '--version', $htmlPackageVersion, '--source', $packageRoot) -Description 'Package consumer dependency restore' -TimeoutSeconds $DotNetCommandTimeoutSeconds -Quiet
                @"
using ChartForgeX;
using ChartForgeX.Core;
using ChartForgeX.Interactivity;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Primitives;

var chart = Chart.Create()
    .WithTitle("Package smoke")
    .WithSize(320, 180)
    .AddLine("Values", new[] { new ChartPoint(1, 2), new ChartPoint(2, 3) });

if (!chart.ToSvg().Contains("<svg", StringComparison.Ordinal)) throw new InvalidOperationException("SVG render failed.");
if (!chart.ToHtmlFragment().Contains("<svg", StringComparison.Ordinal)) throw new InvalidOperationException("HTML render failed.");
if (chart.ToPng().Length <= 64) throw new InvalidOperationException("PNG render failed.");
var html = chart.ToInteractiveHtmlPage(options => options.Interaction.Enable(ChartInteractionFeatures.Zoom | ChartInteractionFeatures.Pan | ChartInteractionFeatures.Brush | ChartInteractionFeatures.Export | ChartInteractionFeatures.SynchronizedCharts));
if (!html.Contains("data-cfx-zoom=\"in\"", StringComparison.Ordinal)) throw new InvalidOperationException("Interactive HTML zoom controls missing.");
if (!html.Contains("data-cfx-mode-button=\"brush\"", StringComparison.Ordinal)) throw new InvalidOperationException("Interactive HTML brush controls missing.");
if (!html.Contains("data-cfx-export=\"svg\"", StringComparison.Ordinal)) throw new InvalidOperationException("Interactive HTML export controls missing.");
if (!html.Contains("data-cfx-export=\"png\"", StringComparison.Ordinal)) throw new InvalidOperationException("Interactive HTML PNG export controls missing.");
if (!html.Contains("new CustomEvent('cfxsync'", StringComparison.Ordinal)) throw new InvalidOperationException("Interactive HTML sync events missing.");
var dashboard = new[] { chart, chart }.ToInteractiveHtmlDashboardPage(options => {
    options.IdScope = "package-dashboard";
    options.Interaction.GroupName = "package-group";
    options.Interaction.Enable(ChartInteractionFeatures.Zoom | ChartInteractionFeatures.SynchronizedCharts);
});
if (!dashboard.Contains("class=\"cfx-dashboard\"", StringComparison.Ordinal)) throw new InvalidOperationException("Interactive dashboard surface missing.");
if (!dashboard.Contains("data-cfx-chart-id=\"package-dashboard-2\"", StringComparison.Ordinal)) throw new InvalidOperationException("Interactive dashboard child chart IDs missing.");
"@ | Set-Content -Path (Join-Path $consumerRoot 'Program.cs') -Encoding UTF8
                Invoke-DotNetCommand -Arguments @('run', '-c', 'Release', '--no-restore') -Description 'Package consumer validation' -TimeoutSeconds $PackageConsumerTimeoutSeconds -Quiet
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
