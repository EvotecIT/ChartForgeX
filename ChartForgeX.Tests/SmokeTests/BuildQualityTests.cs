using System;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace ChartForgeX.Tests;

internal static partial class SmokeTests {
    private static void BuildScriptVerifiesReleaseArtifacts() {
        var buildScript = Path.Combine(FindRepositoryRoot(), "Build.ps1");
        var script = File.ReadAllText(buildScript);
        Assert(script.Contains("ChartForgeX*.nupkg", StringComparison.Ordinal), "Build script should verify NuGet package creation and clean copy-suffixed package artifacts.");
        Assert(script.Contains("ChartForgeX*.snupkg", StringComparison.Ordinal), "Build script should verify symbol package creation and clean copy-suffixed symbol artifacts.");
        Assert(script.Contains("artifacts/packages/$Configuration", StringComparison.Ordinal), "Build script should put all release packages in a shared ignored artifact folder.");
        Assert(script.Contains("$packages.Count -ne $packageProjects.Count", StringComparison.Ordinal), "Build script should reject stale or duplicate packages.");
        Assert(script.Contains("ChartForgeX.Interactivity.Html", StringComparison.Ordinal), "Build script should package the HTML interactivity adapter.");
        Assert(script.Contains("DependencyIds = @('ChartForgeX', 'ChartForgeX.Interactivity')", StringComparison.Ordinal), "Build script should verify adapter package dependencies.");
        Assert(script.Contains("README.md", StringComparison.Ordinal), "Build script should verify README package inclusion.");
        Assert(script.Contains("@('README.md')", StringComparison.Ordinal), "Build script should verify README package inclusion without requiring separate changelog packaging.");
        Assert(script.Contains("lib/$framework/$($packageProject.Assembly).$extension", StringComparison.Ordinal), "Build script should verify package framework assets.");
        Assert(script.Contains("ChartForgeX-package-consumer", StringComparison.Ordinal), "Build script should verify package consumption from a clean project.");
        Assert(script.Contains("[switch] $SkipAot", StringComparison.Ordinal), "Build script should allow intentional Native AOT smoke skips for local triage.");
        Assert(script.Contains("ChartForgeX.AotSmoke", StringComparison.Ordinal), "Build script should publish and run the Native AOT smoke app.");
        Assert(script.Contains("Get-NativeAotRuntimeIdentifier", StringComparison.Ordinal), "Build script should resolve the current OS runtime identifier for Native AOT validation.");
        Assert(script.Contains("CHARTFORGEX_NATIVE_AOT_RID", StringComparison.Ordinal), "Build script should allow explicit Native AOT RID override for unusual validation hosts.");
        Assert(script.Contains("RuntimeInformation]::ProcessArchitecture", StringComparison.Ordinal), "Build script should choose Native AOT RID from process architecture so emulated shells do not request unsupported cross-AOT publishes.");
        Assert(script.Contains("linux-musl", StringComparison.Ordinal), "Build script should publish a musl Native AOT RID on musl-based Linux hosts.");
        Assert(script.Contains("Get-Command ldd -CommandType Application", StringComparison.Ordinal), "Build script should resolve ldd from PATH instead of a hard-coded Linux path.");
        Assert(script.Contains("$PSNativeCommandUseErrorActionPreference = $false", StringComparison.Ordinal), "Build script should prevent non-zero ldd probe exit codes from terminating Native AOT RID detection.");
        Assert(script.Contains("$PSNativeCommandUseErrorActionPreference = $previousNativeErrorActionPreference", StringComparison.Ordinal), "Build script should restore native command error handling after probing ldd.");
        Assert(script.Contains("$lddReportsMusl", StringComparison.Ordinal) && script.Contains("$lddReportsGlibc", StringComparison.Ordinal), "Build script should use ldd output as the authoritative libc probe.");
        Assert(!script.Contains("ld-musl-", StringComparison.Ordinal), "Build script should not infer the active libc from loader files that may belong to cross-toolchains.");
        Assert(script.Contains("if ($lddReportsMusl -and -not $lddReportsGlibc)", StringComparison.Ordinal), "Build script should select a musl RID only when ldd identifies musl without glibc.");
        Assert(script.Contains("Invoke-NativeSmokeExecutable", StringComparison.Ordinal), "Build script should execute the compiled Native AOT smoke binary.");
        Assert(script.Contains("globalPackagesFolder", StringComparison.Ordinal), "Build script should isolate the package consumer cache so same-version local packages are retested.");
        Assert(script.Contains("DotNetCommandTimeoutSeconds", StringComparison.Ordinal), "Build script should time-limit all dotnet validation commands.");
        Assert(script.Contains("Invoke-DotNetCommand", StringComparison.Ordinal), "Build script should route dotnet calls through one timeout-aware command runner.");
        Assert(script.Contains("$startInfo.WorkingDirectory = (Get-Location).ProviderPath", StringComparison.Ordinal), "Build script should run wrapped dotnet commands from the active PowerShell location.");
        Assert(script.Contains("timed out after $TimeoutSeconds", StringComparison.Ordinal), "Build script should fail loudly when a dotnet validation step hangs.");
        Assert(script.Contains("PackageConsumerTimeoutSeconds", StringComparison.Ordinal), "Build script should time-limit package consumer validation.");
        Assert(script.Contains("Package consumer validation", StringComparison.Ordinal), "Build script should name package consumer validation errors.");
        Assert(script.Contains("TimeoutSeconds $PackageConsumerTimeoutSeconds", StringComparison.Ordinal), "Build script should use the shorter package consumer timeout for the final smoke run.");
        Assert(script.Contains("'add', 'package', 'ChartForgeX.Interactivity.Html'", StringComparison.Ordinal), "Build script should install the freshest adapter package in the consumer smoke test.");
        Assert(script.Contains("function Join-ProcessArguments", StringComparison.Ordinal), "Build script should keep a Windows PowerShell compatible argument fallback.");
        Assert(script.Contains("$startInfo.Arguments = Join-ProcessArguments", StringComparison.Ordinal), "Build script should support ProcessStartInfo on runtimes without ArgumentList.");
        Assert(script.Contains("ToInteractiveHtmlPage", StringComparison.Ordinal), "Build script should verify interactive HTML package consumption from a clean project.");
        Assert(script.Contains("ToInteractiveHtmlDashboardPage", StringComparison.Ordinal), "Build script should verify interactive dashboard package consumption from a clean project.");
        Assert(script.Contains("<dependency\\s", StringComparison.Ordinal), "Build script should verify dependency-free package invariants.");
        Assert(script.Contains("svg-png-comparison.json", StringComparison.Ordinal), "Build script should verify generated SVG/PNG comparison health.");
        Assert(script.Contains("function Assert-VisualComparisonHealth", StringComparison.Ordinal), "Build script should keep visual comparison health checks isolated.");
        Assert(script.Contains("function New-VisualBaseline", StringComparison.Ordinal), "Build script should keep visual-baseline generation isolated.");
        Assert(script.Contains("function Assert-VisualBaseline", StringComparison.Ordinal), "Build script should keep visual-baseline assertions isolated.");
        Assert(script.Contains("function Assert-TopologyVisualCoverage", StringComparison.Ordinal), "Build script should keep topology visual coverage checks isolated.");
        Assert(script.Contains("baselineScope 'visual-capability-manifest'", StringComparison.Ordinal), "Build script should document the topology visual baseline scope.");
        Assert(script.Contains("outside visual-baseline.json", StringComparison.Ordinal), "Build script should require topology visual coverage to explain why it is not in the numeric visual baseline yet.");
        Assert(script.Contains("baselineCandidates", StringComparison.Ordinal), "Build script should require topology baseline candidates for future promotion.");
        Assert(script.Contains("$minimumChartPairs = 50", StringComparison.Ordinal), "Build script should enforce the current minimum example chart coverage.");
        Assert(script.Contains("$Comparison.chartPairs -lt $minimumChartPairs", StringComparison.Ordinal), "Build script should fail when generated SVG/PNG comparison coverage drops.");
        Assert(script.Contains("$Comparison.dimensionMatches -ne $Comparison.chartPairs", StringComparison.Ordinal), "Build script should fail when SVG/PNG dimensions drift apart.");
        Assert(script.Contains("$Comparison.healthySvgs -ne $Comparison.chartPairs", StringComparison.Ordinal), "Build script should fail when SVG health drops.");
        Assert(script.Contains("$Comparison.healthyPngs -ne $Comparison.chartPairs", StringComparison.Ordinal), "Build script should fail when PNG health drops.");
        Assert(script.Contains("$Comparison.healthyHtmls -ne $Comparison.chartPairs", StringComparison.Ordinal), "Build script should fail when standalone HTML health drops.");
        Assert(script.Contains("$Comparison.warnings -ne 0", StringComparison.Ordinal), "Build script should fail when generated SVG/PNG comparison warnings are present.");
        Assert(script.Contains("visual-baseline.json", StringComparison.Ordinal), "Build script should verify generated visuals against the checked-in baseline.");
        Assert(script.Contains("[switch] $UpdateVisualBaseline", StringComparison.Ordinal), "Build script should offer an intentional visual-baseline refresh switch.");
        Assert(script.Contains("Visual baseline updates require examples to run", StringComparison.Ordinal), "Build script should reject visual-baseline updates when examples are skipped.");
        Assert(script.Contains("ConvertTo-Json -Depth 8", StringComparison.Ordinal), "Build script should write refreshed visual baselines as JSON.");
        Assert(script.Contains("minDistinctColors = [int][Math]::Max", StringComparison.Ordinal), "Build script should keep refreshed PNG color-diversity baselines meaningful.");
        Assert(script.Contains("maxClippedTextNodes", StringComparison.Ordinal), "Build script should baseline SVG text-edge quality.");
        Assert(script.Contains("maxEdgeInkPixels", StringComparison.Ordinal), "Build script should baseline PNG edge-pressure quality.");
        Assert(script.Contains("outputScale = [int]$chart.png.scale", StringComparison.Ordinal), "Build script should record high-DPI PNG output scale in the visual baseline.");
        Assert(script.Contains("$generatedCharts.ContainsKey($expected.name)", StringComparison.Ordinal), "Build script should fail when a baseline chart is missing.");
        Assert(script.Contains("$baselineCharts.ContainsKey($actual.name)", StringComparison.Ordinal), "Build script should fail when a generated chart lacks baseline coverage.");
        Assert(script.Contains("$actual.svg.visualNodes -lt $expected.svg.minVisualNodes", StringComparison.Ordinal), "Build script should fail when SVG visual complexity drops below baseline.");
        Assert(script.Contains("$actual.png.visiblePixels -lt $expected.png.minVisiblePixels", StringComparison.Ordinal), "Build script should fail when PNG visible content drops below baseline.");
        Assert(script.Contains("visual-capability-manifest.json", StringComparison.Ordinal), "Build script should verify topology visual coverage manifest generation.");
        Assert(script.Contains("visual-geographic-topology-map", StringComparison.Ordinal), "Build script should verify topology-native geographic visual coverage.");
        Assert(script.Contains("data-route-curve=\"geographic\"", StringComparison.Ordinal), "Build script should verify geographic topology route-arc metadata.");

        var qualityWorkflow = File.ReadAllText(Path.Combine(FindRepositoryRoot(), ".github", "workflows", "quality.yml"));
        Assert(qualityWorkflow.Contains("Install Native AOT prerequisites", StringComparison.Ordinal), "Quality workflow should install Native AOT prerequisites before running the release gate.");
        Assert(qualityWorkflow.Contains("dotnet nuget locals all --clear", StringComparison.Ordinal), "Quality workflow should clear reusable NuGet cache space before package provisioning on self-hosted runners.");
        Assert(qualityWorkflow.Contains("has_native_aot_compiler", StringComparison.Ordinal), "Quality workflow should name the compiler probe generically because the self-hosted release gate can use clang or gcc.");
        Assert(qualityWorkflow.Contains("command -v apt-get", StringComparison.Ordinal) && qualityWorkflow.Contains("command -v dnf", StringComparison.Ordinal) && qualityWorkflow.Contains("command -v yum", StringComparison.Ordinal) && qualityWorkflow.Contains("command -v apk", StringComparison.Ordinal), "Quality workflow should support common Linux package managers on generic self-hosted Linux runners.");
        Assert(qualityWorkflow.Contains("apt-get clean", StringComparison.Ordinal) && qualityWorkflow.Contains("find /var/lib/apt/lists", StringComparison.Ordinal) && qualityWorkflow.Contains("Acquire::Languages=none", StringComparison.Ordinal), "Quality workflow should avoid refetching apt metadata when provisioning Native AOT prerequisites on space-constrained runners.");
        Assert(qualityWorkflow.Contains("rpm -q zlib-devel zlib-ng-devel zlib-ng-compat-devel", StringComparison.Ordinal), "Quality workflow should verify Fedora/RHEL Native AOT zlib compatibility headers before short-circuiting setup.");
        Assert(qualityWorkflow.Contains("apk info -e zlib-dev", StringComparison.Ordinal) && qualityWorkflow.Contains("apk info -e musl-dev", StringComparison.Ordinal), "Quality workflow should require Alpine musl headers before short-circuiting Native AOT setup.");
        Assert(qualityWorkflow.Contains("apt_packages+=(gcc)", StringComparison.Ordinal) && qualityWorkflow.Contains("apt_packages+=(zlib1g-dev)", StringComparison.Ordinal), "Quality workflow should only install missing Ubuntu Native AOT prerequisites on space-constrained self-hosted runners.");
        Assert(qualityWorkflow.Contains("dnf_packages+=(gcc)", StringComparison.Ordinal) && qualityWorkflow.Contains("dnf install -y \"${dnf_packages[@]}\"", StringComparison.Ordinal), "Quality workflow should install only missing Fedora Native AOT prerequisites.");
        Assert(qualityWorkflow.Contains("yum_packages+=(gcc)", StringComparison.Ordinal) && qualityWorkflow.Contains("yum install -y \"${yum_packages[@]}\"", StringComparison.Ordinal), "Quality workflow should install only missing RHEL Native AOT prerequisites.");
        Assert(qualityWorkflow.Contains("apk_packages+=(build-base)", StringComparison.Ordinal) && qualityWorkflow.Contains("apk_packages+=(zlib-dev)", StringComparison.Ordinal), "Quality workflow should install only missing Alpine Native AOT prerequisites.");

        var exampleSyncScript = File.ReadAllText(Path.Combine(FindRepositoryRoot(), "Website", "build", "Sync-GeneratedExamples.ps1"));
        Assert(exampleSyncScript.Contains("function Write-Utf8NoBom", StringComparison.Ordinal), "Example sync script should centralize generated text writing.");
        Assert(exampleSyncScript.Contains("function Read-Utf8Text", StringComparison.Ordinal), "Example sync script should centralize UTF-8 reads for Windows PowerShell 5.1 round-tripping.");
        Assert(exampleSyncScript.Contains("[System.IO.Path]::GetFullPath($Path)", StringComparison.Ordinal), "Example sync writer should support new output files without requiring Resolve-Path.");
        Assert(exampleSyncScript.Contains("New-Item -ItemType Directory -Force -Path $directory", StringComparison.Ordinal), "Example sync writer should create missing output directories for custom gallery paths.");
        Assert(exampleSyncScript.Contains("Read-Utf8Text -Path $GalleryPath | ConvertFrom-Json", StringComparison.Ordinal), "Example sync script should read existing gallery metadata as UTF-8 before rewriting it without a BOM.");
    }

    private static void VisualBaselineIsStructuredAndActionable() {
        var baselinePath = Path.Combine(FindRepositoryRoot(), "ChartForgeX.Examples", "visual-baseline.json");
        using var document = JsonDocument.Parse(File.ReadAllText(baselinePath));
        var root = document.RootElement;
        Assert(root.GetProperty("version").GetInt32() == 1, "Visual baseline should use schema version 1.");
        var charts = root.GetProperty("charts").EnumerateArray().ToArray();
        Assert(charts.Length >= 20, "Visual baseline should cover the full generated example set.");

        var names = charts.Select(chart => chart.GetProperty("name").GetString() ?? string.Empty).ToArray();
        Assert(names.All(name => !string.IsNullOrWhiteSpace(name)), "Visual baseline chart names should be non-empty.");
        Assert(names.Distinct(StringComparer.OrdinalIgnoreCase).Count() == names.Length, "Visual baseline chart names should be unique.");
        Assert(names.SequenceEqual(names.OrderBy(name => name, StringComparer.OrdinalIgnoreCase)), "Visual baseline chart names should stay sorted for reviewable diffs.");
        Assert(names.Contains("warnings-sparkline", StringComparer.OrdinalIgnoreCase), "Visual baseline should cover compact sparkline charts.");
        Assert(names.Contains("domain-remediation-timeline-light", StringComparer.OrdinalIgnoreCase), "Visual baseline should cover timeline charts.");
        Assert(names.Contains("security-posture-radar-dark", StringComparer.OrdinalIgnoreCase), "Visual baseline should cover radar charts.");
        Assert(names.Contains("control-partition-sunburst-aurora", StringComparer.OrdinalIgnoreCase), "Visual baseline should cover sunburst hierarchy charts.");
        Assert(names.Contains("audience-pictorial-candy", StringComparer.OrdinalIgnoreCase), "Visual baseline should cover pictorial charts.");
        Assert(names.Contains("support-themes-word-cloud-editorial", StringComparer.OrdinalIgnoreCase), "Visual baseline should cover word cloud charts.");
        Assert(names.Contains("developer-consistency-calendar-light", StringComparer.OrdinalIgnoreCase), "Visual baseline should cover calendar heatmap charts.");
        Assert(names.Contains("travel-dotted-map-dark", StringComparer.OrdinalIgnoreCase), "Visual baseline should cover dotted map charts.");
        Assert(names.Contains("revenue-region-map-us-states-light", StringComparer.OrdinalIgnoreCase), "Visual baseline should cover catalog-backed region map charts.");
        Assert(names.Contains("revenue-tile-map-us-states-light", StringComparer.OrdinalIgnoreCase), "Visual baseline should cover catalog-backed tile map charts.");
        Assert(names.Contains("theme-font-showcase-grid", StringComparer.OrdinalIgnoreCase), "Visual baseline should cover theme and font gallery output.");
        Assert(names.Contains("brand-kit-showcase-grid", StringComparer.OrdinalIgnoreCase), "Visual baseline should cover brand kit gallery output.");
        Assert(names.Contains("palette-swatch-showcase-grid", StringComparer.OrdinalIgnoreCase), "Visual baseline should cover palette gallery output.");
        Assert(names.Contains("pictorial-symbol-showcase-grid", StringComparer.OrdinalIgnoreCase), "Visual baseline should cover pictorial symbol gallery output.");
        Assert(names.Contains("pictorial-isotype-showcase-grid", StringComparer.OrdinalIgnoreCase), "Visual baseline should cover pictorial Isotype gallery output.");
        Assert(names.Contains("people-infographic-showcase-grid", StringComparer.OrdinalIgnoreCase), "Visual baseline should cover people infographic gallery output.");
        Assert(names.Contains("word-cloud-control-showcase-grid", StringComparer.OrdinalIgnoreCase), "Visual baseline should cover word cloud control gallery output.");
        Assert(names.Contains("point-color-customization-showcase-grid", StringComparer.OrdinalIgnoreCase), "Visual baseline should cover point color customization gallery output.");

        foreach (var chart in charts) {
            var name = chart.GetProperty("name").GetString() ?? "<unknown>";
            var width = chart.GetProperty("width").GetInt32();
            var height = chart.GetProperty("height").GetInt32();
            var minVisualNodes = chart.GetProperty("svg").GetProperty("minVisualNodes").GetInt32();
            var maxClippedTextNodes = chart.GetProperty("svg").GetProperty("maxClippedTextNodes").GetInt32();
            var minVisiblePixels = chart.GetProperty("png").GetProperty("minVisiblePixels").GetInt64();
            var minDistinctColors = chart.GetProperty("png").GetProperty("minDistinctColors").GetInt32();
            var outputScale = chart.GetProperty("png").GetProperty("outputScale").GetInt32();
            var maxEdgeInkPixels = chart.GetProperty("png").GetProperty("maxEdgeInkPixels").GetInt64();
            Assert(width >= 320 && height >= 90, "Visual baseline dimensions should describe real chart output: " + name + ".");
            Assert(minVisualNodes >= 2, "Visual baseline SVG visual-node minimum should be meaningful: " + name + ".");
            Assert(maxClippedTextNodes == 0, "Visual baseline should reject clipped SVG text: " + name + ".");
            Assert(outputScale >= 1 && outputScale <= 4, "Visual baseline PNG scale should be explicit: " + name + ".");
            Assert(minVisiblePixels >= 64, "Visual baseline PNG visible-pixel minimum should be meaningful: " + name + ".");
            Assert(minDistinctColors >= 8, "Visual baseline PNG color-diversity minimum should be meaningful: " + name + ".");
            Assert(maxEdgeInkPixels == 0, "Visual baseline should reject PNG edge pressure: " + name + ".");
        }

        var contributionGuide = File.ReadAllText(Path.Combine(FindRepositoryRoot(), "CONTRIBUTING.md"));
        Assert(contributionGuide.Contains("DotNetCommandTimeoutSeconds", StringComparison.Ordinal) && contributionGuide.Contains("PackageConsumerTimeoutSeconds", StringComparison.Ordinal), "Contribution guidance should document build timeout controls.");
        Assert(contributionGuide.Contains("fails with a named timeout", StringComparison.Ordinal), "Contribution guidance should explain timeout failures as actionable build signals.");
        Assert(contributionGuide.Contains("-UpdateVisualBaseline", StringComparison.Ordinal), "Contribution guidance should explain intentional visual-baseline refreshes.");
        Assert(contributionGuide.Contains("clipped SVG text", StringComparison.Ordinal) && contributionGuide.Contains("PNG edge pressure", StringComparison.Ordinal), "Contribution guidance should explain the visual-baseline quality gates.");
        Assert(contributionGuide.Contains("Topology Visual Coverage", StringComparison.Ordinal), "Contribution guidance should document the topology visual coverage gate.");
        Assert(contributionGuide.Contains("visual-capability-manifest.json", StringComparison.Ordinal), "Contribution guidance should document the topology visual coverage manifest.");
    }
}
