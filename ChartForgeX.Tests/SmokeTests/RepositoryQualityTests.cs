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
        var aotSmokeProject = Path.Combine(root, "ChartForgeX.AotSmoke", "ChartForgeX.AotSmoke.csproj");
        Assert(File.Exists(aotSmokeProject), "Repository should include a Native AOT consumer smoke project.");
        Assert(HasXmlProperty(aotSmokeProject, "PublishAot", "true"), "Native AOT smoke project should publish with Native AOT enabled.");
        Assert(HasXmlProperty(aotSmokeProject, "TrimMode", "full"), "Native AOT smoke project should use full trimming.");

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

    private static void ChartKindTraitsCentralizeRendererClassification() {
        var root = FindRepositoryRoot();
        var traits = File.ReadAllText(Path.Combine(root, "ChartForgeX", "Core", "ChartSeriesKindTraits.cs"));
        Assert(traits.Contains("IsExclusive", StringComparison.Ordinal), "Chart-kind exclusivity should live in the shared trait table.");
        Assert(traits.Contains("UsesCartesianXAxis", StringComparison.Ordinal), "Shared-axis compatibility should live in the shared trait table.");
        Assert(traits.Contains("IsMapKind", StringComparison.Ordinal), "Map renderer axis suppression should live in the shared trait table.");
        Assert(traits.Contains("IsLineLikeLegendKind", StringComparison.Ordinal), "Legend symbol classification should live in the shared trait table.");

        var guards = File.ReadAllText(Path.Combine(root, "ChartForgeX", "Core", "ChartGuards.cs"));
        var grid = File.ReadAllText(Path.Combine(root, "ChartForgeX", "Core", "ChartGrid.cs"));
        var range = File.ReadAllText(Path.Combine(root, "ChartForgeX", "Rendering", "ChartRange.cs"));
        var svgHelpers = File.ReadAllText(Path.Combine(root, "ChartForgeX", "Svg", "SvgChartRenderer.Helpers.cs"));
        var pngRenderer = File.ReadAllText(Path.Combine(root, "ChartForgeX", "Raster", "PngChartRenderer.cs"));

        Assert(!guards.Contains("ExclusiveSeriesKinds", StringComparison.Ordinal), "Chart guards should not keep a second specialized-kind table.");
        Assert(grid.Contains("ChartSeriesKindTraits.UsesCartesianXAxis", StringComparison.Ordinal) && grid.Contains("ChartSeriesKindTraits.UsesCartesianYAxis", StringComparison.Ordinal), "Chart grids should use shared cartesian compatibility traits.");
        Assert(range.Contains("ChartSeriesKindTraits.IsExclusive", StringComparison.Ordinal), "Range calculation should skip specialized renderers through the shared trait table.");
        Assert(svgHelpers.Contains("ChartSeriesKindTraits.IsMapKind", StringComparison.Ordinal) && pngRenderer.Contains("ChartSeriesKindTraits.IsMapKind", StringComparison.Ordinal), "SVG and PNG renderers should share map-kind classification.");
    }

    private static void LinePolishLayersStaySharedAcrossSvgAndPng() {
        var root = FindRepositoryRoot();
        var layers = File.ReadAllText(Path.Combine(root, "ChartForgeX", "Rendering", "ChartLineVisualLayers.cs"));
        var svgLines = File.ReadAllText(Path.Combine(root, "ChartForgeX", "Svg", "SvgChartRenderer.LineStyling.cs"));
        var svgRange = File.ReadAllText(Path.Combine(root, "ChartForgeX", "Svg", "SvgChartRenderer.RangeArea.cs"));
        var pngCartesian = File.ReadAllText(Path.Combine(root, "ChartForgeX", "Raster", "PngChartRenderer.Cartesian.cs"));
        var pngRange = File.ReadAllText(Path.Combine(root, "ChartForgeX", "Raster", "PngChartRenderer.RangeArea.cs"));

        Assert(layers.Contains("RoleSuffix", StringComparison.Ordinal) && layers.Contains("ColorWithOpacity", StringComparison.Ordinal), "Shared line layers should expose role suffixes for SVG and alpha-adjusted colors for PNG.");
        Assert(svgLines.Contains("ChartLineVisualLayers.Build", StringComparison.Ordinal) && svgRange.Contains("ChartLineVisualLayers.Build", StringComparison.Ordinal), "SVG line emitters should use the shared premium line layer model.");
        Assert(pngCartesian.Contains("ChartLineVisualLayers.Build", StringComparison.Ordinal) && pngRange.Contains("ChartLineVisualLayers.Build", StringComparison.Ordinal), "PNG line emitters should use the shared premium line layer model.");
        Assert(!svgLines.Contains("LineHighlightOpacity(", StringComparison.Ordinal), "SVG line styling should not duplicate highlight-opacity math.");
        Assert(!pngCartesian.Contains("PngLineHighlight", StringComparison.Ordinal) && !pngCartesian.Contains("PngStrokeAmbientHalo", StringComparison.Ordinal), "PNG cartesian line styling should not duplicate premium layer color math.");
    }

    private static void MarkSurfacePolishStaysSharedAcrossSvgAndPng() {
        var root = FindRepositoryRoot();
        var surface = File.ReadAllText(Path.Combine(root, "ChartForgeX", "Rendering", "ChartMarkSurface.cs"));
        var primitives = File.ReadAllText(Path.Combine(root, "ChartForgeX", "Rendering", "ChartVisualPrimitives.cs"));
        var svgRenderer = File.ReadAllText(Path.Combine(root, "ChartForgeX", "Svg", "SvgChartRenderer.cs"));
        var svgHelpers = File.ReadAllText(Path.Combine(root, "ChartForgeX", "Svg", "SvgChartRenderer.Helpers.cs"));
        var pngCartesian = File.ReadAllText(Path.Combine(root, "ChartForgeX", "Raster", "PngChartRenderer.Cartesian.cs"));
        var svgFunnel = File.ReadAllText(Path.Combine(root, "ChartForgeX", "Svg", "SvgChartRenderer.Funnel.cs"));
        var pngFunnel = File.ReadAllText(Path.Combine(root, "ChartForgeX", "Raster", "PngChartRenderer.Funnel.cs"));
        var svgTreemap = File.ReadAllText(Path.Combine(root, "ChartForgeX", "Svg", "SvgChartRenderer.Treemap.cs"));
        var pngTreemap = File.ReadAllText(Path.Combine(root, "ChartForgeX", "Raster", "PngChartRenderer.Treemap.cs"));
        var svgSankey = File.ReadAllText(Path.Combine(root, "ChartForgeX", "Svg", "SvgChartRenderer.Sankey.cs"));
        var pngSankey = File.ReadAllText(Path.Combine(root, "ChartForgeX", "Raster", "PngChartRenderer.Sankey.cs"));
        var svgTree = File.ReadAllText(Path.Combine(root, "ChartForgeX", "Svg", "SvgChartRenderer.Tree.cs"));
        var pngTree = File.ReadAllText(Path.Combine(root, "ChartForgeX", "Raster", "PngChartRenderer.Tree.cs"));
        var svgTimeline = File.ReadAllText(Path.Combine(root, "ChartForgeX", "Svg", "SvgChartRenderer.Timeline.cs"));
        var pngTimeline = File.ReadAllText(Path.Combine(root, "ChartForgeX", "Raster", "PngChartRenderer.Timeline.cs"));

        Assert(surface.Contains("BarGradientTop", StringComparison.Ordinal) && surface.Contains("FunnelSegmentGradientTop", StringComparison.Ordinal) && surface.Contains("TreemapTileGradientBottom", StringComparison.Ordinal), "Reusable mark-surface gradient roles should live in one rendering helper.");
        Assert(primitives.Contains("BarGradientTopBlend", StringComparison.Ordinal) && primitives.Contains("FunnelSegmentGradientTopBlend", StringComparison.Ordinal) && primitives.Contains("TreemapTileGradientBottomBlend", StringComparison.Ordinal), "Mark-surface blend strengths should live in shared visual primitive tokens.");
        Assert(svgRenderer.Contains("AppendBarSurfaceGradient", StringComparison.Ordinal) && svgRenderer.Contains("seriesFill{i}-point{pointIndex}", StringComparison.Ordinal), "SVG bar fills, including point colors, should use reusable mark-surface gradient definitions.");
        Assert(svgHelpers.Contains("seriesFill{seriesIndex}-point{pointIndex}", StringComparison.Ordinal), "SVG point-colored bars should not fall back to flat one-off fill strings.");
        Assert(pngCartesian.Contains("ChartMarkSurface.BarGradientTop", StringComparison.Ordinal) && pngCartesian.Contains("ChartMarkSurface.BarGradientBottom", StringComparison.Ordinal), "PNG bars should use shared mark-surface gradient roles.");
        foreach (var renderer in new[] { svgFunnel, pngFunnel, svgTreemap, pngTreemap, svgSankey, pngSankey, svgTree, pngTree, svgTimeline, pngTimeline }) {
            Assert(renderer.Contains("ChartMarkSurface.", StringComparison.Ordinal), "Specialized SVG and PNG mark renderers should delegate surface gradients to ChartMarkSurface.");
        }
    }

    private static void SvgFittedTextPolishStaysSharedAcrossSpecializedCharts() {
        var root = FindRepositoryRoot();
        var writerHelpers = File.ReadAllText(Path.Combine(root, "ChartForgeX", "Svg", "SvgChartRenderer.MarkupWriterHelpers.cs"));
        var timeline = File.ReadAllText(Path.Combine(root, "ChartForgeX", "Svg", "SvgChartRenderer.Timeline.cs"));
        var gantt = File.ReadAllText(Path.Combine(root, "ChartForgeX", "Svg", "SvgChartRenderer.Gantt.cs"));
        var dottedMap = File.ReadAllText(Path.Combine(root, "ChartForgeX", "Svg", "SvgChartRenderer.DottedMap.cs"));

        Assert(writerHelpers.Contains("ChartTextStyle? style", StringComparison.Ordinal) && writerHelpers.Contains("WriteSvgTextStyleAttributes", StringComparison.Ordinal), "SVG markup-writer text helpers should support shared fitting and text-style attributes.");
        Assert(timeline.Contains("DrawSvgTextCenteredX(writer", StringComparison.Ordinal) && gantt.Contains("DrawSvgTextCenteredX(writer", StringComparison.Ordinal), "Timeline and Gantt centered labels should use the shared SVG fitted-text helper.");
        Assert(!timeline.Contains("DrawTimelineSvgTextCenteredX", StringComparison.Ordinal) && !gantt.Contains("DrawGanttSvgTextCenteredX", StringComparison.Ordinal), "Specialized SVG renderers should not keep duplicate fitted centered-text helpers.");
        Assert(!timeline.Contains("WriteTimelineSvgTextStyleAttributes", StringComparison.Ordinal) && !gantt.Contains("WriteGanttSvgTextStyleAttributes", StringComparison.Ordinal), "Specialized SVG renderers should not duplicate text-style attribute writers.");
        Assert(!dottedMap.Contains("AppendLine($\"<text", StringComparison.Ordinal) && dottedMap.Contains("WriteSvgTextStyleAttributes(writer, style)", StringComparison.Ordinal), "Dotted-map SVG labels should be emitted through SvgMarkupWriter instead of interpolated text markup.");
    }

    private static void ColorReadabilityMathStaysSharedAcrossSvgAndPng() {
        var root = FindRepositoryRoot();
        var colorMath = File.ReadAllText(Path.Combine(root, "ChartForgeX", "Rendering", "ChartColorMath.cs"));
        var markSurface = File.ReadAllText(Path.Combine(root, "ChartForgeX", "Rendering", "ChartMarkSurface.cs"));
        var dottedMapSurface = File.ReadAllText(Path.Combine(root, "ChartForgeX", "Rendering", "ChartDottedMapSurface.cs"));
        var heatmapSurface = File.ReadAllText(Path.Combine(root, "ChartForgeX", "Rendering", "ChartHeatmapSurface.cs"));
        var svgHeatmap = File.ReadAllText(Path.Combine(root, "ChartForgeX", "Svg", "SvgChartRenderer.Heatmap.cs"));
        var pngHeatmap = File.ReadAllText(Path.Combine(root, "ChartForgeX", "Raster", "PngChartRenderer.Heatmap.cs"));
        var svgCalendar = File.ReadAllText(Path.Combine(root, "ChartForgeX", "Svg", "SvgChartRenderer.CalendarHeatmap.cs"));
        var pngCalendar = File.ReadAllText(Path.Combine(root, "ChartForgeX", "Raster", "PngChartRenderer.CalendarHeatmap.cs"));
        var svgDottedMap = File.ReadAllText(Path.Combine(root, "ChartForgeX", "Svg", "SvgChartRenderer.DottedMap.cs"));
        var pngDottedMap = File.ReadAllText(Path.Combine(root, "ChartForgeX", "Raster", "PngChartRenderer.DottedMap.cs"));
        var svgFunnel = File.ReadAllText(Path.Combine(root, "ChartForgeX", "Svg", "SvgChartRenderer.Funnel.cs"));
        var pngFunnel = File.ReadAllText(Path.Combine(root, "ChartForgeX", "Raster", "PngChartRenderer.Funnel.cs"));
        var svgTree = File.ReadAllText(Path.Combine(root, "ChartForgeX", "Svg", "SvgChartRenderer.Tree.cs"));
        var pngTree = File.ReadAllText(Path.Combine(root, "ChartForgeX", "Raster", "PngChartRenderer.Tree.cs"));
        var svgRegionMap = File.ReadAllText(Path.Combine(root, "ChartForgeX", "Svg", "SvgChartRenderer.RegionMap.cs"));
        var pngRegionMap = File.ReadAllText(Path.Combine(root, "ChartForgeX", "Raster", "PngChartRenderer.RegionMap.cs"));
        var svgTileMap = File.ReadAllText(Path.Combine(root, "ChartForgeX", "Svg", "SvgChartRenderer.TileMap.cs"));
        var pngTileMap = File.ReadAllText(Path.Combine(root, "ChartForgeX", "Raster", "PngChartRenderer.TileMap.cs"));
        var pngText = File.ReadAllText(Path.Combine(root, "ChartForgeX", "Raster", "PngChartRenderer.Text.cs"));

        Assert(colorMath.Contains("Blend", StringComparison.Ordinal) && colorMath.Contains("TextOnBackground", StringComparison.Ordinal) && colorMath.Contains("RelativeLuminance", StringComparison.Ordinal), "Shared color math should own blend and readable foreground decisions.");
        Assert(markSurface.Contains("ChartColorMath.Blend", StringComparison.Ordinal), "Mark-surface gradients should use the shared color blend helper.");
        Assert(dottedMapSurface.Contains("ChartColorMath.RelativeLuminance", StringComparison.Ordinal) && dottedMapSurface.Contains("LandDotColor", StringComparison.Ordinal) && dottedMapSurface.Contains("BoundaryOpacity", StringComparison.Ordinal), "Dotted-map land and boundary surface contrast should stay centralized.");
        Assert(heatmapSurface.Contains("ChartColorMath.Blend", StringComparison.Ordinal) && heatmapSurface.Contains("CalendarEmptyColor", StringComparison.Ordinal) && heatmapSurface.Contains("MapNoDataColor", StringComparison.Ordinal), "Heatmap and map-adjacent color scales should stay centralized.");
        Assert(svgHeatmap.Contains("ChartHeatmapSurface.Color", StringComparison.Ordinal) && pngHeatmap.Contains("ChartHeatmapSurface.Color", StringComparison.Ordinal), "SVG and PNG heatmap color scales should share heatmap surface math.");
        Assert(svgHeatmap.Contains("ChartColorMath.TextOnBackground", StringComparison.Ordinal) && pngHeatmap.Contains("ChartColorMath.TextOnBackground", StringComparison.Ordinal), "SVG and PNG heatmap labels should share foreground contrast decisions.");
        Assert(svgFunnel.Contains("ChartColorMath.TextOnBackground(color, 0.58)", StringComparison.Ordinal) && pngFunnel.Contains("ChartColorMath.TextOnBackground(color, 0.58)", StringComparison.Ordinal), "SVG and PNG funnel labels should share foreground contrast thresholds.");
        Assert(svgTree.Contains("ChartColorMath.TextOnBackground(labelColor, 0.70)", StringComparison.Ordinal) && pngTree.Contains("ChartColorMath.TextOnBackground(labelColor, 0.70)", StringComparison.Ordinal), "SVG and PNG tree label halos should share luminance thresholds.");
        Assert(svgCalendar.Contains("ChartHeatmapSurface.CalendarColor", StringComparison.Ordinal) && pngCalendar.Contains("ChartHeatmapSurface.CalendarColor", StringComparison.Ordinal), "SVG and PNG calendar heatmaps should share color and empty-cell math.");
        Assert(svgRegionMap.Contains("ChartHeatmapSurface.MapColor", StringComparison.Ordinal) && pngRegionMap.Contains("ChartHeatmapSurface.MapColor", StringComparison.Ordinal), "SVG and PNG region maps should share heatmap surface color decisions.");
        Assert(svgTileMap.Contains("ChartHeatmapSurface.MapNoDataColor", StringComparison.Ordinal) && pngTileMap.Contains("ChartHeatmapSurface.MapNoDataColor", StringComparison.Ordinal), "SVG and PNG tile maps should share no-data color decisions.");
        Assert(svgDottedMap.Contains("ChartDottedMapSurface.LandDotColor", StringComparison.Ordinal) && pngDottedMap.Contains("ChartDottedMapSurface.LandDotColor", StringComparison.Ordinal), "SVG and PNG dotted maps should use shared land-dot surface color decisions.");
        Assert(svgDottedMap.Contains("ChartDottedMapSurface.BoundaryColor", StringComparison.Ordinal) && pngDottedMap.Contains("ChartDottedMapSurface.BoundaryColor", StringComparison.Ordinal), "SVG and PNG dotted maps should use shared boundary surface color decisions.");
        Assert(pngText.Contains("ChartColorMath.WithOpacity", StringComparison.Ordinal), "PNG label halo opacity should use shared color alpha math.");
        Assert(!svgHeatmap.Contains("private static ChartColor Blend", StringComparison.Ordinal) && !pngHeatmap.Contains("private static ChartColor Blend", StringComparison.Ordinal), "Heatmap renderers should not carry duplicate private blend helpers.");
        Assert(!svgHeatmap.Contains("private static ChartColor ChartColorMath.TextOnBackground", StringComparison.Ordinal) && !pngHeatmap.Contains("private static ChartColor ChartColorMath.TextOnBackground", StringComparison.Ordinal), "Heatmap renderers should not carry duplicate readable-foreground helpers.");
        Assert(!svgDottedMap.Contains("IsLightDottedMapSurface", StringComparison.Ordinal) && !pngDottedMap.Contains("IsLightDottedMapSurface", StringComparison.Ordinal), "Dotted-map renderers should not carry duplicate surface luminance helpers.");
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
        Assert(HasXmlProperty(libraryProject, "PackageLicenseExpression", "MIT"), "Core package should declare the repository license.");
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
        Assert(File.Exists(Path.Combine(FindRepositoryRoot(), "CONTRIBUTING.md")), "Repository should include contribution guidance.");
        Assert(File.Exists(Path.Combine(FindRepositoryRoot(), "TODO.md")), "Repository should include centralized follow-up guidance.");
        Assert(File.Exists(Path.Combine(FindRepositoryRoot(), "AGENTS.md")), "Repository should include agent guidance.");
        Assert(File.Exists(Path.Combine(FindRepositoryRoot(), "LICENSE")), "Repository should include a root license file.");
        Assert(!File.Exists(Path.Combine(FindRepositoryRoot(), "CHANGELOG.md")), "GitHub Releases should be the release-note source of truth instead of a second repository changelog.");
        Assert(!File.Exists(Path.Combine(FindRepositoryRoot(), "docs", "dashboard-pattern-expansion-plan.md")), "Completed dashboard implementation plans should be folded into TODO or focused docs instead of staying as stale plan files.");
        Assert(!Directory.Exists(Path.Combine(FindRepositoryRoot(), "experiments")), "Release branches should not ship loose experiment folders; keep durable decisions in docs or TODO.");
        foreach (var packageProject in new[] {
            libraryProject,
            Path.Combine(FindRepositoryRoot(), "ChartForgeX.Interactivity", "ChartForgeX.Interactivity.csproj"),
            Path.Combine(FindRepositoryRoot(), "ChartForgeX.Interactivity.Html", "ChartForgeX.Interactivity.Html.csproj")
        }) {
            Assert(HasXmlProperty(packageProject, "PackageLicenseExpression", "MIT"), "Package should declare the MIT license: " + Path.GetRelativePath(FindRepositoryRoot(), packageProject));
            Assert(HasXmlProperty(packageProject, "IsAotCompatible", "true"), "Modern package assets should declare AOT compatibility: " + Path.GetRelativePath(FindRepositoryRoot(), packageProject));
            Assert(HasXmlProperty(packageProject, "EnableTrimAnalyzer", "true"), "Modern package assets should enable trim analysis: " + Path.GetRelativePath(FindRepositoryRoot(), packageProject));
            Assert(HasXmlProperty(packageProject, "EnableAotAnalyzer", "true"), "Modern package assets should enable AOT analysis: " + Path.GetRelativePath(FindRepositoryRoot(), packageProject));
        }
        var tags = GetXmlValue(libraryProject, "PackageTags");
        foreach (var tag in new[] { "charts", "svg", "reports", "zero-dependency", "aot", "nativeaot", "trimming" }) {
            Assert(tags.Contains(tag, StringComparison.OrdinalIgnoreCase), "Package tags should include " + tag + ".");
        }
    }

    private static void ReadmeDocumentsChartCatalog() {
        var root = FindRepositoryRoot();
        var readme = File.ReadAllText(Path.Combine(root, "README.md"));
        var nugetReadme = File.ReadAllText(Path.Combine(root, "README.nuget.md"));
        Assert(readme.Contains("## Visual Tour", StringComparison.Ordinal), "README should include rendered visuals near the top of the page.");
        foreach (var asset in new[] {
            "dashboard-restaurant-overview-grid.html",
            "visual-geographic-topology-map.html",
            "dashboard-saas-mrr-grid.html",
            "control-scorecards-grid.html",
            "visual-replication-mesh-explorer.html",
            "theme-font-showcase-grid.html",
            "catalog.html",
            "svg-png-comparison.html",
            "quality-dashboard.html"
        }) {
            var localPath = Path.Combine(root, "Website", "static", "examples", "generated", asset);
            Assert(readme.Contains(asset, StringComparison.Ordinal), "README visual tour should advertise generated example asset: " + asset);
            Assert(File.Exists(localPath), "README visual tour asset should exist locally: " + Path.GetRelativePath(root, localPath));
        }
        foreach (var asset in new[] {
            "dashboard-restaurant-overview-grid.png",
            "dashboard-restaurant-overview-grid.svg",
            "visual-geographic-topology-map.png",
            "visual-geographic-topology-map.svg",
            "dashboard-saas-mrr-grid.png",
            "dashboard-saas-mrr-grid.svg",
            "control-scorecards-grid.png",
            "control-scorecards-grid.svg",
            "visual-replication-mesh-explorer.png",
            "visual-replication-mesh-explorer.svg",
            "theme-font-showcase-grid.png",
            "theme-font-showcase-grid.svg"
        }) {
            var localPath = Path.Combine(root, "Website", "static", "examples", "generated", asset);
            Assert(readme.Contains("Website/static/examples/generated/" + asset, StringComparison.Ordinal), "README visual tour should use repository-relative generated asset link: " + asset);
            Assert(File.Exists(localPath), "README visual tour asset should exist locally: " + Path.GetRelativePath(root, localPath));
        }
        Assert(nugetReadme.Contains("## Visual Tour", StringComparison.Ordinal), "NuGet README should include rendered visuals near the top of the package page.");
        Assert(!nugetReadme.Contains("](Website/static/examples/generated/", StringComparison.Ordinal), "NuGet README should not use repository-relative generated asset links because it is rendered outside the repo checkout.");
        foreach (var preview in new[] {
            "dashboard-restaurant-overview-grid.png",
            "visual-geographic-topology-map.png",
            "dashboard-saas-mrr-grid.png",
            "control-scorecards-grid.png"
        }) {
            var previewUrl = "https://raw.githubusercontent.com/EvotecIT/ChartForgeX/main/Website/static/examples/generated/" + preview;
            Assert(nugetReadme.Contains(previewUrl, StringComparison.Ordinal), "NuGet README visual tour should use absolute package-safe renderable asset URL: " + previewUrl);
        }
        Assert(readme.Contains("## Examples", StringComparison.Ordinal) && readme.Contains("svg-png-comparison.html", StringComparison.Ordinal) && readme.Contains("C# snippets", StringComparison.Ordinal), "README should explain how generated examples expose SVG, PNG, HTML, and C# source snippets.");
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
        Assert(readme.Contains("## Project Status", StringComparison.Ordinal) && readme.Contains("TODO.md", StringComparison.Ordinal), "README should summarize project status without keeping internal release-boundary prose on the front page.");
        Assert(readme.Contains("## Output API", StringComparison.Ordinal) && readme.Contains("chart.Save(\"chart.svg\")", StringComparison.Ordinal) && readme.Contains("SaveRasterImage", StringComparison.Ordinal) && readme.Contains("RasterImageOptions", StringComparison.Ordinal), "README should document the export API behavior contract directly.");
        Assert(readme.Contains("## Native AOT and Trimming", StringComparison.Ordinal) && readme.Contains("ChartForgeX.AotSmoke", StringComparison.Ordinal) && readme.Contains("Native AOT executable", StringComparison.Ordinal), "README should document AOT and trimming support as a verified release contract.");
        Assert(readme.Contains("GitHub Releases", StringComparison.Ordinal) && readme.Contains("NuGet package notes", StringComparison.Ordinal), "README should explain where release notes belong.");
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
            "ChartMapCatalogEntry",
            "ChartMapCatalogEntryKind",
            "EmbeddedEntries",
            "ExternalEntries",
            "Load",
            "FromAssetDirectory",
            "ChartMapDefinition",
            "ChartMapRegion",
            "ChartTileMapCatalog",
            "ChartTileMapDefinition",
            "ChartTileMapRegion",
            "ChartRegionMapItem",
            "WithMapLabels",
            "WithMapScaleLegend",
            "WithMapScaleLegendPosition",
            "WithMapSurface",
            "WithMapRegionStroke",
            "WithRegionMapBounds",
            "WithRegionMapCoordinateBounds",
            "AddMapBaseLayer",
            "AddMapBoundaryLayer",
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
