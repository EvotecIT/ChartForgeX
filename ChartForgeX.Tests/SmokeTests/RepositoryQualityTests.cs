using System;
using System.IO;
using System.Linq;

namespace ChartForgeX.Tests;

internal static partial class SmokeTests {
    private static void SourceFilesStayUnderArchitectureLineBudget() {
        const int lineBudget = 800;
        var root = FindRepositoryRoot();
        var oversized = new[] { "ChartForgeX", "ChartForgeX.Interactivity", "ChartForgeX.Interactivity.Html", "ChartForgeX.Examples", "ChartForgeX.Tests" }
            .Where(sourceRoot => Directory.Exists(Path.Combine(root, sourceRoot)))
            .SelectMany(sourceRoot => Directory.EnumerateFiles(Path.Combine(root, sourceRoot), "*.cs", SearchOption.AllDirectories))
            .Where(file => !IsGeneratedPath(file))
            .Select(file => new { File = file, Lines = File.ReadLines(file).Count() })
            .Where(item => item.Lines > lineBudget)
            .Select(item => Path.GetRelativePath(root, item.File) + " (" + item.Lines.ToString(System.Globalization.CultureInfo.InvariantCulture) + " lines)")
            .ToArray();
        Assert(oversized.Length == 0, "Source files should stay under " + lineBudget.ToString(System.Globalization.CultureInfo.InvariantCulture) + " lines. Split: " + string.Join(", ", oversized));
    }

    private static void ProjectFilesKeepStrictBuildSettings() {
        var root = FindRepositoryRoot();
        var projectFiles = Directory.EnumerateFiles(root, "*.csproj", SearchOption.AllDirectories).Where(file => !IsGeneratedPath(file)).ToArray();
        var projectSettingFiles = Directory.EnumerateFiles(root, "*.*", SearchOption.AllDirectories).Where(IsProjectSettingFile).Where(file => !IsGeneratedPath(file)).ToArray();

        foreach (var file in projectSettingFiles) {
            Assert(!File.ReadAllText(file).Contains("<NoWarn", StringComparison.OrdinalIgnoreCase), "Project files should not suppress warnings with NoWarn: " + Path.GetRelativePath(root, file));
        }

        foreach (var projectFile in projectFiles) {
            Assert(HasXmlProperty(projectFile, "TreatWarningsAsErrors", "true"), "Project should treat warnings as errors: " + Path.GetRelativePath(root, projectFile));
        }

        var libraryProject = Path.Combine(root, "ChartForgeX", "ChartForgeX.csproj");
        Assert(HasXmlProperty(libraryProject, "GenerateDocumentationFile", "true"), "Library project should generate XML documentation.");
        var testProject = Path.Combine(root, "ChartForgeX.Tests", "ChartForgeX.Tests.csproj");
        Assert(HasXmlProperty(testProject, "IsTestProject", "true"), "Smoke suite should be discoverable by dotnet test.");

        foreach (var packageReference in GetXmlElements(libraryProject, "PackageReference")) {
            var include = packageReference.Attribute("Include")?.Value ?? string.Empty;
            var privateAssets = packageReference.Attribute("PrivateAssets")?.Value ?? string.Empty;
            var allowedBuildPackage = string.Equals(include, "Microsoft.NETFramework.ReferenceAssemblies.net472", StringComparison.OrdinalIgnoreCase) &&
                string.Equals(privateAssets, "all", StringComparison.OrdinalIgnoreCase);
            Assert(allowedBuildPackage, "Runtime package dependencies are not allowed in the core library: " + include);
        }

        foreach (var projectFile in projectFiles.Where(file => !string.Equals(file, libraryProject, StringComparison.OrdinalIgnoreCase))) {
            foreach (var packageReference in GetXmlElements(projectFile, "PackageReference")) {
                var privateAssets = packageReference.Attribute("PrivateAssets")?.Value ?? string.Empty;
                Assert(string.Equals(privateAssets, "all", StringComparison.OrdinalIgnoreCase), "Non-library package references should stay private: " + Path.GetRelativePath(root, projectFile));
            }
        }
    }

    private static void ExampleAppClearsGeneratedOutputBeforeWriting() {
        var program = File.ReadAllText(Path.Combine(FindRepositoryRoot(), "ChartForgeX.Examples", "Program.cs"));
        Assert(program.Contains("Directory.Delete(output, recursive: true)", StringComparison.Ordinal), "Example generation should wipe stale output before writing comparison artifacts.");
        Assert(program.Contains("SaveInteractiveHtml", StringComparison.Ordinal), "Example generation should include a visible interactive HTML adapter demo.");
        Assert(program.Contains("SaveInteractiveHtmlDashboard", StringComparison.Ordinal), "Example generation should include a visible synchronized dashboard adapter demo.");
        Assert(program.Contains("ChartInteractionFeatures.Zoom | ChartInteractionFeatures.Pan | ChartInteractionFeatures.Brush | ChartInteractionFeatures.Export | ChartInteractionFeatures.SynchronizedCharts", StringComparison.Ordinal), "Interactive example should exercise the competitive review toolbar features.");
    }

    private static void EuropeRevenueMapRoutesTargetRenderedMarkers() {
        var maps = File.ReadAllText(Path.Combine(FindRepositoryRoot(), "ChartForgeX.Examples", "MapExamples.cs"));
        Assert(!maps.Contains("London to Warsaw", StringComparison.Ordinal) && !maps.Contains("Madrid to Berlin", StringComparison.Ordinal), "Europe revenue map routes should not use capital-city coordinates when the rendered markers are country market points.");
        Assert(maps.Contains(".AddMapRouteBetweenPoints(\"United Kingdom to Poland\", \"United Kingdom\", \"Poland\"", StringComparison.Ordinal), "Europe revenue map should route from the United Kingdom marker to the Poland marker.");
        Assert(maps.Contains(".AddMapRouteBetweenPoints(\"Spain to Germany\", \"Spain\", \"Germany\"", StringComparison.Ordinal), "Europe revenue map should route from the Spain marker to the Germany marker.");
        Assert(!maps.Contains(".AddMapRoute(route.Label", StringComparison.Ordinal), "Viewport map examples should use point-bound route helpers instead of duplicating raw route coordinates.");
        Assert(maps.Contains(".AddMapRouteBetweenPoints(route.Label, route.FromPointLabel, route.ToPointLabel", StringComparison.Ordinal), "Viewport map examples should bind route overlays to the rendered dotted-map markers.");
    }

    private static void NuGetPackageMetadataStaysPublishReady() {
        var libraryProject = Path.Combine(FindRepositoryRoot(), "ChartForgeX", "ChartForgeX.csproj");
        Assert(HasXmlProperty(libraryProject, "PackageId", "ChartForgeX"), "PackageId should remain stable.");
        Assert(HasXmlProperty(libraryProject, "PackageReadmeFile", "README.md"), "Package should include the README.");
        Assert(HasXmlProperty(libraryProject, "PackageProjectUrl", "https://github.com/EvotecIT/ChartForgeX"), "Package should expose the project URL.");
        Assert(HasXmlProperty(libraryProject, "RepositoryUrl", "https://github.com/EvotecIT/ChartForgeX"), "Package should expose the repository URL.");
        Assert(HasXmlProperty(libraryProject, "RepositoryType", "git"), "Package repository type should be git.");
        Assert(HasXmlProperty(libraryProject, "PublishRepositoryUrl", "true"), "Package should publish repository metadata.");
        Assert(HasXmlProperty(libraryProject, "Deterministic", "true"), "Package builds should be deterministic.");
        Assert(HasXmlProperty(libraryProject, "IncludeSymbols", "true"), "Package should include symbol package generation.");
        Assert(HasXmlProperty(libraryProject, "SymbolPackageFormat", "snupkg"), "Package symbols should use snupkg format.");
        var releaseNotes = GetXmlValue(libraryProject, "PackageReleaseNotes");
        Assert(releaseNotes.Contains("SVG", StringComparison.OrdinalIgnoreCase) && releaseNotes.Contains("PNG", StringComparison.OrdinalIgnoreCase) && releaseNotes.Contains("validation", StringComparison.OrdinalIgnoreCase), "Package release notes should summarize renderer coverage and validation work.");
        Assert(releaseNotes.Contains("brand kits", StringComparison.OrdinalIgnoreCase) && releaseNotes.Contains("pictorial", StringComparison.OrdinalIgnoreCase) && releaseNotes.Contains("word cloud", StringComparison.OrdinalIgnoreCase), "Package release notes should summarize the current chart and styling surface.");
        Assert(File.Exists(Path.Combine(FindRepositoryRoot(), "CHANGELOG.md")), "Repository should include a changelog.");
        Assert(File.Exists(Path.Combine(FindRepositoryRoot(), "CONTRIBUTING.md")), "Repository should include contribution guidance.");
        Assert(File.Exists(Path.Combine(FindRepositoryRoot(), "RELEASING.md")), "Repository should include release guidance.");
        var tags = GetXmlValue(libraryProject, "PackageTags");
        foreach (var tag in new[] { "charts", "svg", "reports", "zero-dependency" }) {
            Assert(tags.Contains(tag, StringComparison.OrdinalIgnoreCase), "Package tags should include " + tag + ".");
        }
    }

    private static void ReadmeDocumentsChartCatalog() {
        var readme = File.ReadAllText(Path.Combine(FindRepositoryRoot(), "README.md"));
        Assert(readme.Contains("## Chart catalog", StringComparison.Ordinal), "README should include a chart catalog.");
        Assert(readme.Contains("validates chart data before rendering", StringComparison.Ordinal), "README should document render-time validation behavior.");
        Assert(readme.Contains("cyclic Sankey flows", StringComparison.Ordinal), "README should document Sankey cycle rejection.");
        Assert(readme.Contains("`chart.ToSvg(\"panel-a\")`", StringComparison.Ordinal), "README should document scoped raw chart SVG embedding.");
        Assert(readme.Contains("`grid.ToSvg(\"report-a\")`", StringComparison.Ordinal), "README should document scoped raw grid SVG embedding.");
        Assert(readme.Contains("## Customization cookbook", StringComparison.Ordinal), "README should include a customization cookbook.");
        Assert(readme.Contains("ChartTheme.Aurora()", StringComparison.Ordinal), "README should document expressive built-in themes.");
        Assert(readme.Contains("ChartTheme.Colorblind()", StringComparison.Ordinal), "README should document accessible color themes.");
        Assert(readme.Contains("ChartTheme.DashboardLight()", StringComparison.Ordinal) && readme.Contains("ChartTheme.SaasDashboardLight()", StringComparison.Ordinal), "README should document dashboard theme presets.");
        Assert(readme.Contains("ChartFontStacks", StringComparison.Ordinal), "README should document built-in font stacks.");
        Assert(readme.Contains("ChartPalettes.Vivid", StringComparison.Ordinal), "README should document reusable palette presets.");
        Assert(readme.Contains("ChartPictorialShape.Person", StringComparison.Ordinal), "README should document expanded pictorial symbols.");
        Assert(readme.Contains("data-cfx-status=\"empty\"", StringComparison.Ordinal) && readme.Contains("explicit zero value", StringComparison.Ordinal), "README should document no-data versus low-value metadata for heatmap-style charts.");
        Assert(readme.Contains("data-cfx-row-count", StringComparison.Ordinal) && readme.Contains("data-cfx-column-count", StringComparison.Ordinal) && readme.Contains("data-cfx-min", StringComparison.Ordinal) && readme.Contains("data-cfx-max", StringComparison.Ordinal), "README should document matrix heatmap container metadata.");
        Assert(readme.Contains("data-cfx-start-date", StringComparison.Ordinal) && readme.Contains("filled/empty day counts", StringComparison.Ordinal), "README should document calendar heatmap container metadata.");
        Assert(readme.Contains("data-cfx-label", StringComparison.Ordinal) && readme.Contains("data-cfx-projection", StringComparison.Ordinal) && readme.Contains("data-cfx-map-kind", StringComparison.Ordinal) && readme.Contains("data-cfx-point-count", StringComparison.Ordinal), "README should document map SVG container metadata.");
        Assert(readme.Contains("ChartSurfaceStyle.Glass", StringComparison.Ordinal), "README should document reusable surface presets.");
        Assert(readme.Contains(".WithTheme(theme => theme", StringComparison.Ordinal), "README should document fluent theme customization callbacks.");
        Assert(readme.Contains("WithPalette(", StringComparison.Ordinal), "README should document palette customization.");
        Assert(readme.Contains("ChartBrandKit", StringComparison.Ordinal) && readme.Contains("WithBrandKit", StringComparison.Ordinal), "README should document reusable brand kits.");
        Assert(readme.Contains("ChartBrandKit.Executive()", StringComparison.Ordinal) && readme.Contains("PeopleInfographic()", StringComparison.Ordinal) && readme.Contains("Accessible()", StringComparison.Ordinal), "README should document brand kit presets.");
        Assert(readme.Contains("Report intent", StringComparison.Ordinal) && readme.Contains("Theme starting point", StringComparison.Ordinal) && readme.Contains("Brand kit starting point", StringComparison.Ordinal), "README should help users choose between themes and brand kits.");
        Assert(readme.Contains("WithPanelSpan", StringComparison.Ordinal) && readme.Contains("columnSpan", StringComparison.Ordinal), "README should document grid panel spans.");
        Assert(readme.Contains("ChartColor.FromHex", StringComparison.Ordinal) && readme.Contains("ChartPalettes.FromHex", StringComparison.Ordinal), "README should document pasted hex color customization.");
        Assert(readme.Contains("WithSurfaceColors", StringComparison.Ordinal) && readme.Contains("WithSemanticColors", StringComparison.Ordinal) && readme.Contains("WithSurfaceStyle", StringComparison.Ordinal), "README should document theme color and surface customization helpers.");
        Assert(readme.Contains("WithStrokeWidth", StringComparison.Ordinal) && readme.Contains("UseThemeColor", StringComparison.Ordinal), "README should document fluent series styling helpers.");
        foreach (var api in new[] {
            "AddLine",
            "AddSmoothLine",
            "AddStepLine",
            "AddArea",
            "AddStepArea",
            "AddSmoothArea",
            "AddStackedArea",
            "AddSmoothStackedArea",
            "AddScatter",
            "AddTrendLine",
            "AddPointCallout",
            "WithPointLabel",
            "WithLegendEntry",
            "WithSemanticRole",
            "AddMeanLine",
            "AddMedianLine",
            "AddStandardDeviationBand",
            "AddSlope",
            "AddBarLineCombo",
            "AddColumnLineCombo",
            "AddBarAreaCombo",
            "AddColumnAreaCombo",
            "AddScatterLineCombo",
            "AddBar",
            "AddHistogram",
            "AddLollipop",
            "AddBubble",
            "AddErrorBar",
            "AddCandlestick",
            "AddOhlc",
            "AddRangeBand",
            "AddRangeArea",
            "AddDumbbell",
            "AddPareto",
            "AddRangeBar",
            "AddBoxPlot",
            "AddHorizontalBar",
            "WithStackedHorizontalBars",
            "AddHeatmapRow",
            "AddHeatmapRows",
            "ChartHeatmapRow",
            "AddHexbinHeatmapRow",
            "AddHexbinHeatmapRows",
            "AddCalendarHeatmap",
            "ChartCalendarHeatmapItem",
            "AddDottedMap",
            "ChartMapPoint",
            "ChartMapViewport",
            "WithMapViewport",
            "AddMapConnector",
            "AddMapRoute",
            "AddMapConnectorBetweenPoints",
            "AddMapRouteBetweenPoints",
            "AddRegionMap",
            "AddTileMap",
            "ChartMapCatalog",
            "ChartMapDefinition",
            "ChartMapRegion",
            "ChartTileMapCatalog",
            "ChartTileMapDefinition",
            "ChartTileMapRegion",
            "ChartRegionMapItem",
            "WithMapLabels",
            "WithMapScaleLegend",
            "AddGauge",
            "AddCircle",
            "AddRadialBar",
            "AddLayeredRadial",
            "ChartRadialLayer",
            "ChartRadialLayerCap",
            "AddBullet",
            "AddWaterfall",
            "AddRadar",
            "AddPolarArea",
            "AddFunnel",
            "AddTreemap",
            "AddPictorial",
            "ChartPictorialItem",
            "ChartPictorialShape",
            "WithPictorialShape",
            "WithPictorialColumns",
            "WithPictorialMaximum",
            "WithPictorialValuePerSymbol",
            "WithPictorialValues",
            "WithPictorialSymbolScale",
            "WithPictorialEmptyOpacity",
            "WithPictorialSvgPath",
            "AddProgressBars",
            "ChartProgressItem",
            "WithProgressMaximum",
            "WithProgressValues",
            "WithProgressHandles",
            "WithProgressBarThickness",
            "WithProgressTrackOpacity",
            "WithLegendPosition",
            "WithPointLegend",
            "ChartTextRole",
            "ChartTextStyle",
            "WithTextStyle",
            "WithTitleStyle",
            "WithSubtitleStyle",
            "WithAxisTitleStyle",
            "WithTickLabelStyle",
            "WithLegendStyle",
            "WithDataLabelStyle",
            "WithDonutCenterLabel",
            "WithDonutCenterText",
            "WithDonutInnerRadiusRatio",
            "WithRadialBarCenterLabel",
            "WithCircleStatusLabel",
            "WithCircleRadiusScale",
            "WithCircleStrokeScale",
            "WithRadialBarRadiusScale",
            "WithRadialBarStrokeScale",
            "ChartBrandKit",
            "WithBrandKit",
            "AddWordCloud",
            "ChartWordCloudItem",
            "WithWordCloudFontRange",
            "WithWordCloudAngles",
            "WithWordCloudMaximumTerms",
            "WithWordCloudDensity",
            "AddTimelineItem",
            "AddTimelineRange",
            "AddGanttTask",
            "AddGanttMilestone",
            "WithGanttToday",
            "AddSankey",
            "ChartSankeyLink",
            "AddTree",
            "ChartTreeLink",
            "AddSunburst",
            "AddPie",
            "AddDonut"
        }) {
            Assert(readme.Contains("`" + api, StringComparison.Ordinal), "README chart catalog should document " + api + ".");
        }
    }

    private static void GitHubActionsUsePrivateRunners() {
        var workflowRoot = Path.Combine(FindRepositoryRoot(), ".github", "workflows");
        Assert(Directory.Exists(workflowRoot), "Repository should include GitHub Actions workflows.");
        var workflows = Directory.EnumerateFiles(workflowRoot, "*.yml", SearchOption.TopDirectoryOnly)
            .Concat(Directory.EnumerateFiles(workflowRoot, "*.yaml", SearchOption.TopDirectoryOnly))
            .ToArray();
        Assert(workflows.Length > 0, "Repository should include at least one GitHub Actions workflow.");
        foreach (var workflow in workflows) {
            var text = File.ReadAllText(workflow);
            Assert(text.Contains("self-hosted", StringComparison.OrdinalIgnoreCase), "GitHub Actions workflows should use self-hosted private runners: " + Path.GetFileName(workflow));
            Assert(text.Contains("Linux", StringComparison.OrdinalIgnoreCase) && text.Contains("X64", StringComparison.OrdinalIgnoreCase), "GitHub Actions workflows should require labels available on the self-hosted Linux runners: " + Path.GetFileName(workflow));
            Assert(text.Contains("actions/setup-dotnet", StringComparison.OrdinalIgnoreCase), "GitHub Actions workflows should install the expected .NET SDK: " + Path.GetFileName(workflow));
            Assert(text.Contains("actions/upload-artifact", StringComparison.OrdinalIgnoreCase), "GitHub Actions workflows should preserve packages and gallery output: " + Path.GetFileName(workflow));
            Assert(text.Contains("artifacts/packages/Release", StringComparison.OrdinalIgnoreCase), "GitHub Actions workflows should upload packages from Build.ps1 artifact output: " + Path.GetFileName(workflow));
            Assert(!ContainsAny(text, "ubuntu-latest", "windows-latest", "macos-latest"), "GitHub Actions workflows should not use public hosted runner labels: " + Path.GetFileName(workflow));
        }
    }
}
