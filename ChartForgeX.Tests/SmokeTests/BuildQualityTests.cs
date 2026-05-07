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
        Assert(script.Contains("CHANGELOG.md", StringComparison.Ordinal), "Build script should verify changelog package inclusion.");
        Assert(script.Contains("lib/$framework/$($packageProject.Assembly).$extension", StringComparison.Ordinal), "Build script should verify package framework assets.");
        Assert(script.Contains("ChartForgeX-package-consumer", StringComparison.Ordinal), "Build script should verify package consumption from a clean project.");
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
        Assert(script.Contains("$minimumChartPairs = 50", StringComparison.Ordinal), "Build script should enforce the current minimum example chart coverage.");
        Assert(script.Contains("$Comparison.chartPairs -lt $minimumChartPairs", StringComparison.Ordinal), "Build script should fail when generated SVG/PNG comparison coverage drops.");
        Assert(script.Contains("$Comparison.dimensionMatches -ne $Comparison.chartPairs", StringComparison.Ordinal), "Build script should fail when SVG/PNG dimensions drift apart.");
        Assert(script.Contains("$Comparison.healthySvgs -ne $Comparison.chartPairs", StringComparison.Ordinal), "Build script should fail when SVG health drops.");
        Assert(script.Contains("$Comparison.healthyPngs -ne $Comparison.chartPairs", StringComparison.Ordinal), "Build script should fail when PNG health drops.");
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
        Assert(names.Contains("revenue-us-state-geo-map-light", StringComparer.OrdinalIgnoreCase), "Visual baseline should cover US state geographic map charts.");
        Assert(names.Contains("revenue-us-state-tile-map-light", StringComparer.OrdinalIgnoreCase), "Visual baseline should cover US state tile map charts.");
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

        var releaseGuide = File.ReadAllText(Path.Combine(FindRepositoryRoot(), "RELEASING.md"));
        Assert(releaseGuide.Contains("DotNetCommandTimeoutSeconds", StringComparison.Ordinal) && releaseGuide.Contains("PackageConsumerTimeoutSeconds", StringComparison.Ordinal), "Release guidance should document build timeout controls.");
        Assert(releaseGuide.Contains("fails with a named timeout", StringComparison.Ordinal), "Release guidance should explain timeout failures as actionable build signals.");
        Assert(releaseGuide.Contains("-UpdateVisualBaseline", StringComparison.Ordinal), "Release guidance should explain intentional visual-baseline refreshes.");
        Assert(releaseGuide.Contains("clipped SVG text", StringComparison.Ordinal) && releaseGuide.Contains("PNG edge pressure", StringComparison.Ordinal), "Release guidance should explain the visual-baseline quality gates.");
    }
}
