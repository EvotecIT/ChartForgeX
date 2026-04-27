using System;
using System.IO;
using System.Linq;
using ChartForgeX;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;

namespace ChartForgeX.Tests;

internal static partial class SmokeTests {
    private static void SourceFilesStayUnderArchitectureLineBudget() {
        const int lineBudget = 800;
        var root = FindRepositoryRoot();
        var oversized = new[] { "ChartForgeX", "ChartForgeX.Examples", "ChartForgeX.Tests" }
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

    private static void BuildScriptVerifiesReleaseArtifacts() {
        var buildScript = Path.Combine(FindRepositoryRoot(), "Build.ps1");
        var script = File.ReadAllText(buildScript);
        Assert(script.Contains("ChartForgeX.*.nupkg", StringComparison.Ordinal), "Build script should verify NuGet package creation.");
        Assert(script.Contains("ChartForgeX.*.snupkg", StringComparison.Ordinal), "Build script should verify symbol package creation.");
        Assert(script.Contains("Expected exactly one package", StringComparison.Ordinal), "Build script should reject stale or duplicate packages.");
        Assert(script.Contains("README.md", StringComparison.Ordinal), "Build script should verify README package inclusion.");
        Assert(script.Contains("CHANGELOG.md", StringComparison.Ordinal), "Build script should verify changelog package inclusion.");
        Assert(script.Contains("lib/$framework/ChartForgeX.$extension", StringComparison.Ordinal), "Build script should verify package framework assets.");
        Assert(script.Contains("ChartForgeX-package-consumer", StringComparison.Ordinal), "Build script should verify package consumption from a clean project.");
        Assert(script.Contains("dotnet add package ChartForgeX", StringComparison.Ordinal), "Build script should install the freshly packed package in the consumer smoke test.");
        Assert(script.Contains("<dependency\\s", StringComparison.Ordinal), "Build script should verify the core package has no runtime dependencies.");
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
        Assert(File.Exists(Path.Combine(FindRepositoryRoot(), "CHANGELOG.md")), "Repository should include a changelog.");
        Assert(File.Exists(Path.Combine(FindRepositoryRoot(), "CONTRIBUTING.md")), "Repository should include contribution guidance.");
        Assert(File.Exists(Path.Combine(FindRepositoryRoot(), "RELEASING.md")), "Repository should include release guidance.");
        var tags = GetXmlValue(libraryProject, "PackageTags");
        foreach (var tag in new[] { "charts", "svg", "reports", "zero-dependency" }) {
            Assert(tags.Contains(tag, StringComparison.OrdinalIgnoreCase), "Package tags should include " + tag + ".");
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
            Assert(text.Contains("private", StringComparison.OrdinalIgnoreCase), "GitHub Actions workflows should require the private runner label: " + Path.GetFileName(workflow));
            Assert(text.Contains("actions/setup-dotnet", StringComparison.OrdinalIgnoreCase), "GitHub Actions workflows should install the expected .NET SDK: " + Path.GetFileName(workflow));
            Assert(text.Contains("actions/upload-artifact", StringComparison.OrdinalIgnoreCase), "GitHub Actions workflows should preserve packages and gallery output: " + Path.GetFileName(workflow));
            Assert(!ContainsAny(text, "ubuntu-latest", "windows-latest", "macos-latest"), "GitHub Actions workflows should not use public hosted runner labels: " + Path.GetFileName(workflow));
        }
    }

    private static void HtmlPageIsStatic() {
        var html = SampleChart().ToHtmlPage();
        Assert(html.Contains("<!doctype html>", StringComparison.OrdinalIgnoreCase), "HTML page should include a document type.");
        Assert(html.Contains("<svg", StringComparison.Ordinal), "HTML page should include inline SVG.");
        Assert(!html.Contains("<script", StringComparison.OrdinalIgnoreCase), "Static HTML renderer should not emit JavaScript.");
    }

    private static void RenderedMarkupStaysSelfContained() {
        var chart = SampleChart();
        AssertSelfContainedMarkup(chart.ToSvg(), "SVG output");
        AssertSelfContainedMarkup(chart.ToHtmlPage(), "HTML page output");
        AssertSelfContainedMarkup(chart.ToHtmlFragment(), "HTML fragment output");
        AssertSelfContainedMarkup(Chart.Create().WithTitle("Radar").WithXLabels("A", "B", "C").AddRadar("Values", Points(80, 60, 90)).ToSvg(), "radar SVG output");
        AssertSelfContainedMarkup(Chart.Create().WithTitle("Funnel").WithXLabels("A", "B", "C").AddFunnel("Values", Points(90, 60, 30)).ToHtmlPage(), "funnel HTML output");
    }

    private static void PngIsValid() {
        var png = SampleChart().ToPng();
        Assert(png.Length > 64, "PNG output should not be empty.");
        Assert(png[0] == 137 && png[1] == 80 && png[2] == 78 && png[3] == 71, "PNG signature should be valid.");
        Assert(ReadBigEndianInt32(png, 16) == 640, "PNG width should match chart width.");
        Assert(ReadBigEndianInt32(png, 20) == 360, "PNG height should match chart height.");
    }

    private static void PngOutputIsCompressed() {
        var png = SampleChart().ToPng();
        Assert(png.Length < 200000, "PNG output should be deflate-compressed rather than stored as raw scanlines.");
    }

    private static void SvgAndPngPreserveRequestedDimensionsAcrossChartKinds() {
        var timelineStart = new DateTime(2026, 1, 1);
        var charts = new[] {
            Chart.Create().WithSize(640, 360).AddLine("Line", Points(10, 20, 16)),
            Chart.Create().WithSize(640, 360).WithXLabels("A", "B", "C").AddBar("Bar", Points(10, 20, 16)),
            Chart.Create().WithSize(640, 360).WithXLabels("A", "B", "C").AddHorizontalBar("Horizontal", Points(10, 20, 16)),
            Chart.Create().WithSize(640, 360).WithXLabels("A", "B", "C").AddHeatmapRow("Heat", Points(96, 82, 74)),
            Chart.Create().WithSize(640, 360).AddGauge("Gauge", 87),
            Chart.Create().WithSize(640, 360).AddBullet("Bullet", 82, 90),
            Chart.Create().WithSize(640, 360).AddWaterfall("Waterfall", Points(18, -42, 9)),
            Chart.Create().WithSize(640, 360).WithXLabels("A", "B", "C").AddRadar("Radar", Points(92, 74, 88)),
            Chart.Create().WithSize(640, 360).WithXLabels("A", "B", "C").AddFunnel("Funnel", Points(420, 318, 174)),
            Chart.Create().WithSize(640, 360).AddTimelineItem("Timeline", timelineStart, timelineStart.AddDays(14)),
            Chart.Create().WithSize(640, 360).WithXLabels("Passed", "Warnings", "Failed").AddDonut("Donut", Points(70, 20, 10)),
            Chart.Create().WithSize(360, 90).WithSparkline().AddSmoothArea("Spark", Points(10, 14, 13, 19))
        };

        foreach (var chart in charts) {
            var svg = chart.ToSvg();
            var png = chart.ToPng();
            var expectedWidth = chart.Options.Size.Width;
            var expectedHeight = chart.Options.Size.Height;
            Assert((int)GetAttribute(svg, "<svg", "width") == expectedWidth, "SVG width should preserve requested chart width for " + chart.Title + ".");
            Assert((int)GetAttribute(svg, "<svg", "height") == expectedHeight, "SVG height should preserve requested chart height for " + chart.Title + ".");
            Assert(ReadBigEndianInt32(png, 16) == expectedWidth, "PNG width should preserve requested chart width for " + chart.Title + ".");
            Assert(ReadBigEndianInt32(png, 20) == expectedHeight, "PNG height should preserve requested chart height for " + chart.Title + ".");
        }
    }

    private static void PngUsesReadableAxisLayout() {
        var labels = Enumerable.Range(1, 20).Select(value => "Checkpoint " + value.ToString("00", System.Globalization.CultureInfo.InvariantCulture)).ToArray();
        var values = Points(Enumerable.Range(1, 20).Select(value => (double)value).ToArray());
        var auto = Chart.Create().WithSize(420, 280).WithXLabels(labels).AddLine("Values", values).ToPng();
        values = Points(Enumerable.Range(1, 20).Select(value => (double)value).ToArray());
        var all = Chart.Create().WithSize(420, 280).WithXAxisLabelDensity(ChartLabelDensity.All).WithXLabels(labels).AddLine("Values", values).ToPng();
        Assert(!auto.SequenceEqual(all), "PNG renderer should honor x-axis label density when explicit labels are crowded.");

        var longAxis = Chart.Create()
            .WithSize(420, 280)
            .WithXAxis("Month")
            .WithYAxis("Latency")
            .WithValueFormatter(value => "$" + value.ToString("N0", System.Globalization.CultureInfo.InvariantCulture) + " ms")
            .AddLine("Latency budget", Points(1000000, 1120000, 1080000))
            .ToPng();
        Assert(longAxis.Length > 64, "PNG renderer should handle long formatted axis labels and axis titles.");
        Assert(ReadBigEndianInt32(longAxis, 16) == 420 && ReadBigEndianInt32(longAxis, 20) == 280, "PNG axis layout should preserve the requested output dimensions.");

        var rotatedAxis = Chart.Create()
            .WithSize(420, 280)
            .WithXAxis("Region")
            .WithYAxis("Certificates")
            .WithXAxisLabelDensity(ChartLabelDensity.All)
            .WithXAxisLabelAngle(-35)
            .WithXLabels("North America", "Western Europe", "Central Europe", "Asia Pacific")
            .AddBar("Logged", Points(1200000, 2350000, 1840000, 3120000))
            .ToPng();
        Assert(rotatedAxis.Length > 64, "PNG renderer should support rotated category labels and vertical axis titles.");
        Assert(ReadBigEndianInt32(rotatedAxis, 16) == 420 && ReadBigEndianInt32(rotatedAxis, 20) == 280, "PNG rotated axis layout should preserve the requested output dimensions.");
    }

    private static void PngUsesSupersampledEdges() {
        var chart = Chart.Create()
            .WithSize(140, 90)
            .AddLine("Diagonal", new[] { new ChartPoint(1, 1), new ChartPoint(3, 3) }, ChartColor.FromRgb(96, 165, 250));
        chart.Options.ShowAxes = false;
        chart.Options.ShowCard = false;
        chart.Options.ShowGrid = false;
        chart.Options.ShowHeader = false;
        chart.Options.ShowLegend = false;
        chart.Options.ShowPlotBackground = false;

        var pixels = ReadPngRgba(chart.ToPng(), out var width, out var height);
        var partialAlpha = 0;
        var solidAlpha = 0;
        for (var i = 3; i < pixels.Length; i += 4) {
            if (pixels[i] > 0 && pixels[i] < 255) partialAlpha++;
            if (pixels[i] == 255) solidAlpha++;
        }

        Assert(width == 140 && height == 90, "PNG supersampling should preserve requested dimensions.");
        Assert(solidAlpha > 0, "PNG smoke chart should draw an opaque stroke core.");
        Assert(partialAlpha > 0, "PNG supersampling should create partially transparent edge pixels around diagonal strokes.");
    }

    private static void PngSmoothSeriesUseCurvedRasterPaths() {
        var points = new[] { new ChartPoint(1, 10), new ChartPoint(2, 90), new ChartPoint(3, 20), new ChartPoint(4, 82), new ChartPoint(5, 24) };
        var straight = Chart.Create()
            .WithSize(260, 160)
            .AddLine("Values", points, ChartColor.FromRgb(96, 165, 250));
        var smooth = Chart.Create()
            .WithSize(260, 160)
            .AddSmoothLine("Values", points, ChartColor.FromRgb(96, 165, 250));
        foreach (var chart in new[] { straight, smooth }) {
            chart.Options.ShowAxes = false;
            chart.Options.ShowCard = false;
            chart.Options.ShowGrid = false;
            chart.Options.ShowHeader = false;
            chart.Options.ShowLegend = false;
            chart.Options.ShowPlotBackground = false;
        }

        Assert(!straight.ToPng().SequenceEqual(smooth.ToPng()), "PNG renderer should honor smooth series instead of drawing the same angular path.");
    }

    private static void PngRendersReportChrome() {
        var chart = Chart.Create()
            .WithSize(360, 220)
            .WithTitle("Chrome")
            .WithSubtitle("Subtitle")
            .AddLine("Primary", Points(10, 20, 30), ChartColor.FromRgb(37, 99, 235))
            .AddLine("Secondary", Points(12, 18, 26), ChartColor.FromRgb(16, 185, 129));
        chart.Options.ShowAxes = false;
        chart.Options.ShowCard = false;
        chart.Options.ShowGrid = false;
        chart.Options.ShowPlotBackground = false;

        var pixels = ReadPngRgba(chart.ToPng(), out var width, out var height);
        var headerAlpha = CountAlphaInRect(pixels, width, 0, 0, width, 84);
        var legendAlpha = CountAlphaInRect(pixels, width, 0, height - 44, width, 44);

        Assert(headerAlpha > 300, "PNG renderer should include readable header title and subtitle text.");
        Assert(legendAlpha > 180, $"PNG renderer should include a readable cartesian legend when legends are enabled. Actual alpha pixels: {legendAlpha}.");
    }

    private static void PngOutlineFontsUseEmSizedText() {
        var chart = Chart.Create()
            .WithSize(360, 150)
            .WithTitle("ChartForgeX")
            .AddLine("Hidden", Points(1, 1), ChartColor.Transparent);
        chart.Options.ShowAxes = false;
        chart.Options.ShowCard = false;
        chart.Options.ShowGrid = false;
        chart.Options.ShowLegend = false;
        chart.Options.ShowPlotBackground = false;

        var pixels = ReadPngRgba(chart.ToPng(), out var width, out _);
        var bounds = FindNearColorBounds(pixels, width, 15, 23, 42, 26);
        Assert(!bounds.IsEmpty, "PNG outline font rendering should draw the title text.");
        Assert(bounds.Width >= 124, $"PNG outline title text should use CSS-like em sizing. Actual width: {bounds.Width}.");
        Assert(bounds.Height >= 18, $"PNG outline title text should not collapse below the requested title size. Actual height: {bounds.Height}.");
    }

    private static void PngRendererUsesEmphasizedReportText() {
        var root = FindRepositoryRoot();
        var canvas = File.ReadAllText(Path.Combine(root, "ChartForgeX", "Raster", "RgbaCanvas.cs"));
        var renderer = File.ReadAllText(Path.Combine(root, "ChartForgeX", "Raster", "PngChartRenderer.cs"));
        Assert(canvas.Contains("DrawTextEmphasized", StringComparison.Ordinal), "PNG raster canvas should expose an emphasized text path for SVG font-weight parity.");
        Assert(canvas.Contains("MeasureTextEmphasizedWidth", StringComparison.Ordinal), "PNG raster canvas should measure emphasized text with its extra painted width.");
        Assert(renderer.Contains("DrawTextEmphasized", StringComparison.Ordinal), "PNG chart renderer should use emphasized text for report-grade title, legend, and data labels.");
        Assert(renderer.Contains("EstimatePngEmphasizedTextWidth", StringComparison.Ordinal), "PNG chart renderer should center and clamp emphasized labels using emphasized text width.");
    }

    private static void PngChartRendererUsesThemeSizedText() {
        var root = FindRepositoryRoot();
        var rendererFiles = Directory.EnumerateFiles(Path.Combine(root, "ChartForgeX", "Raster"), "PngChartRenderer*.cs", SearchOption.TopDirectoryOnly).ToArray();
        foreach (var file in rendererFiles) {
            var source = File.ReadAllText(file);
            Assert(!source.Contains("DrawTextTiny", StringComparison.Ordinal), "PNG chart renderers should use theme-sized outline text instead of tiny text calls: " + Path.GetRelativePath(root, file));
        }
    }

    private static void PngFontPathFallsBackGracefully() {
        var missingFont = Path.Combine(Path.GetTempPath(), "ChartForgeX-missing-font-" + Guid.NewGuid().ToString("N", System.Globalization.CultureInfo.InvariantCulture) + ".ttf");
        var baseline = Chart.Create()
            .WithSize(360, 220)
            .WithTitle("PNG font")
            .WithSubtitle("Automatic fallback")
            .AddLine("Primary", Points(10, 20, 30), ChartColor.FromRgb(37, 99, 235))
            .ToPng();
        var fallback = Chart.Create()
            .WithSize(360, 220)
            .WithTitle("PNG font")
            .WithSubtitle("Automatic fallback")
            .WithPngFont(missingFont)
            .AddLine("Primary", Points(10, 20, 30), ChartColor.FromRgb(37, 99, 235))
            .ToPng();

        Assert(baseline.SequenceEqual(fallback), "PNG renderer should fall back to automatic font discovery when a preferred font path cannot be loaded.");
    }

    private static void PngFontPathSupportsTrueTypeCollections() {
        var collectionPath = "/System/Library/Fonts/HelveticaNeue.ttc";
        var baseline = Chart.Create()
            .WithSize(360, 220)
            .WithTitle("PNG collection")
            .WithSubtitle("TrueType collection")
            .AddLine("Primary", Points(10, 20, 30), ChartColor.FromRgb(37, 99, 235))
            .ToPng();
        var collection = Chart.Create()
            .WithSize(360, 220)
            .WithTitle("PNG collection")
            .WithSubtitle("TrueType collection")
            .WithPngFont(collectionPath)
            .AddLine("Primary", Points(10, 20, 30), ChartColor.FromRgb(37, 99, 235))
            .ToPng();
        var indexedCollection = Chart.Create()
            .WithSize(360, 220)
            .WithTitle("PNG collection")
            .WithSubtitle("TrueType collection")
            .WithPngFont(collectionPath, 0)
            .AddLine("Primary", Points(10, 20, 30), ChartColor.FromRgb(37, 99, 235))
            .ToPng();
        var namedCollection = Chart.Create()
            .WithSize(360, 220)
            .WithTitle("PNG collection")
            .WithSubtitle("TrueType collection")
            .WithPngFont(collectionPath, faceName: "Helvetica Neue")
            .AddLine("Primary", Points(10, 20, 30), ChartColor.FromRgb(37, 99, 235))
            .ToPng();
        var missingFace = Chart.Create()
            .WithSize(360, 220)
            .WithTitle("PNG collection")
            .WithSubtitle("TrueType collection")
            .WithPngFont(collectionPath, faceName: "Definitely Missing Face")
            .AddLine("Primary", Points(10, 20, 30), ChartColor.FromRgb(37, 99, 235))
            .ToPng();
        var outOfRangeIndex = Chart.Create()
            .WithSize(360, 220)
            .WithTitle("PNG collection")
            .WithSubtitle("TrueType collection")
            .WithPngFont(collectionPath, 9999)
            .AddLine("Primary", Points(10, 20, 30), ChartColor.FromRgb(37, 99, 235))
            .ToPng();

        Assert(collection.Length > 64, "PNG renderer should remain valid when a TrueType collection path is configured.");
        Assert(indexedCollection.Length > 64, "PNG renderer should remain valid when a TrueType collection face index is configured.");
        Assert(namedCollection.Length > 64, "PNG renderer should remain valid when a TrueType collection face name is configured.");
        if (File.Exists(collectionPath)) Assert(!baseline.SequenceEqual(collection), "PNG renderer should load supported TrueType collection paths instead of silently falling back.");
        if (File.Exists(collectionPath)) Assert(collection.SequenceEqual(indexedCollection), "PNG renderer should load the first TrueType collection face when index zero is requested.");
        if (File.Exists(collectionPath)) Assert(!baseline.SequenceEqual(namedCollection), "PNG renderer should load supported TrueType collection face names instead of silently falling back.");
        Assert(baseline.SequenceEqual(missingFace), "PNG renderer should fall back to automatic font discovery when a collection face name cannot be loaded.");
        Assert(baseline.SequenceEqual(outOfRangeIndex), "PNG renderer should fall back to automatic font discovery when a collection face index cannot be loaded.");
    }

    private static void PngFontDiagnosticsDescribeFallbackDecisions() {
        var collectionPath = "/System/Library/Fonts/HelveticaNeue.ttc";
        var automatic = Chart.Create().GetPngFontInfo();
        Assert(automatic.Source == PngFontSource.Automatic || automatic.Source == PngFontSource.BuiltIn, "PNG font diagnostics should report automatic or built-in fallback for default charts.");
        Assert(automatic.UsesOutlineFont == (automatic.Source != PngFontSource.BuiltIn), "PNG font diagnostics should describe outline font usage consistently.");

        var missingFont = Path.Combine(Path.GetTempPath(), "ChartForgeX-missing-font-" + Guid.NewGuid().ToString("N", System.Globalization.CultureInfo.InvariantCulture) + ".ttf");
        var missing = Chart.Create().WithPngFont(missingFont).GetPngFontInfo();
        Assert(missing.RequestedPath == Path.GetFullPath(missingFont), "PNG font diagnostics should include the requested font path.");
        Assert(missing.Source != PngFontSource.Requested, "PNG font diagnostics should not report requested source when the configured font cannot be loaded.");

        if (File.Exists(collectionPath)) {
            var requested = Chart.Create().WithPngFont(collectionPath, faceName: "Helvetica Neue").GetPngFontInfo();
            Assert(requested.Source == PngFontSource.Requested, "PNG font diagnostics should report requested source when a configured font loads.");
            Assert(string.Equals(requested.ResolvedPath, Path.GetFullPath(collectionPath), StringComparison.OrdinalIgnoreCase), "PNG font diagnostics should include the resolved requested path.");
            Assert(requested.ResolvedCollectionIndex.HasValue, "PNG font diagnostics should include the resolved collection index for TrueType collections.");
            Assert(!string.IsNullOrWhiteSpace(requested.ResolvedFaceName), "PNG font diagnostics should include a resolved face name when available.");
            var indexed = Chart.Create().WithPngFont(collectionPath, 0).GetPngFontInfo();
            Assert(indexed.ResolvedCollectionIndex == 0, "PNG font diagnostics should preserve explicitly selected collection indexes.");
        }
    }

    private static void PngTrueTypeRendererHandlesCompositeGlyphs() {
        var collectionPath = "/System/Library/Fonts/HelveticaNeue.ttc";
        var fallback = Chart.Create()
            .WithSize(420, 240)
            .WithTitle("München Łódź Café")
            .WithSubtitle("Zażółć gęślą jaźń")
            .WithPngFont(collectionPath, faceName: "Definitely Missing Face")
            .AddLine("Żółć", Points(10, 20, 30), ChartColor.FromRgb(37, 99, 235))
            .ToPng();
        var configured = Chart.Create()
            .WithSize(420, 240)
            .WithTitle("München Łódź Café")
            .WithSubtitle("Zażółć gęślą jaźń")
            .WithPngFont(collectionPath, faceName: "Helvetica Neue")
            .AddLine("Żółć", Points(10, 20, 30), ChartColor.FromRgb(37, 99, 235))
            .ToPng();

        Assert(configured.Length > 64, "PNG renderer should produce valid output for labels containing composite glyphs.");
        if (File.Exists(collectionPath)) Assert(!fallback.SequenceEqual(configured), "PNG renderer should render composite glyphs through the configured TrueType font instead of falling back.");
    }

    private static void PngTrueTypeRendererSupportsKerning() {
        var root = FindRepositoryRoot();
        var source = File.ReadAllText(Path.Combine(root, "ChartForgeX", "Raster", "TrueTypeFont.cs"));
        Assert(source.Contains("private readonly int _kern;", StringComparison.Ordinal), "PNG TrueType renderer should discover optional kerning tables.");
        Assert(source.Contains("private readonly int _gpos;", StringComparison.Ordinal), "PNG TrueType renderer should discover optional OpenType GPOS tables.");
        Assert(source.Contains("KerningFormat0", StringComparison.Ordinal), "PNG TrueType renderer should support classic TrueType kern format 0 pairs.");
        Assert(source.Contains("GposPairAdjustment", StringComparison.Ordinal), "PNG TrueType renderer should support common OpenType GPOS pair adjustment kerning.");
        Assert(CountOccurrences(source, "Kerning(previous.Value, glyph)") >= 2, "PNG TrueType renderer should apply kerning during both measurement and drawing.");
    }

    private static void PngSurfacesUseRoundedCorners() {
        var chart = Chart.Create()
            .WithSize(160, 100)
            .AddLine("Invisible", new[] { new ChartPoint(1, 1), new ChartPoint(2, 2) }, ChartColor.Transparent);
        chart.Options.ShowAxes = false;
        chart.Options.ShowGrid = false;
        chart.Options.ShowHeader = false;
        chart.Options.ShowLegend = false;
        chart.Options.ShowPlotBackground = false;

        var pixels = ReadPngRgba(chart.ToPng(), out var width, out _);
        Assert(CountAlphaInRect(pixels, width, 15, 15, 1, 1) == 0, "PNG card corners should stay transparent outside the rounded radius.");
        Assert(CountAlphaInRect(pixels, width, 32, 15, 1, 1) > 0, "PNG card top edge should still render after applying rounded corners.");
    }

    private static void PngAnnotationsUseReadableRasterStyling() {
        var chart = Chart.Create()
            .WithSize(260, 180)
            .WithPadding(20, 20, 20, 24)
            .AddLine("Hidden", new[] { new ChartPoint(1, 0), new ChartPoint(3, 20) }, ChartColor.Transparent)
            .AddHorizontalLine(10, "target", ChartColor.FromRgb(251, 191, 36));
        chart.Options.ShowAxes = false;
        chart.Options.ShowCard = false;
        chart.Options.ShowGrid = false;
        chart.Options.ShowHeader = false;
        chart.Options.ShowLegend = false;
        chart.Options.ShowPlotBackground = false;

        var pixels = ReadPngRgba(chart.ToPng(), out var width, out _);
        var dashedSamples = CountTransparentSamplesOnRow(pixels, width, 88, 20, 220);
        var lineSamples = 220 - dashedSamples;
        var pillAlpha = CountAlphaInRect(pixels, width, 172, 68, 68, 24);

        Assert(lineSamples > 20, "PNG annotation line should render visible dash segments.");
        Assert(dashedSamples > 20, "PNG annotation line should preserve transparent gaps between dash segments.");
        Assert(pillAlpha > 300, "PNG annotation labels should render with a readable filled pill.");
    }

    private static void PngPieLikeChartsUseReadableDetails() {
        var chart = Chart.Create()
            .WithSize(260, 180)
            .WithDataLabels()
            .WithXLabels("Passed", "Warning", "Failed")
            .AddDonut("Checks", Points(70, 20, 10));
        chart.Options.ShowCard = false;
        chart.Options.ShowHeader = false;
        chart.Options.ShowLegend = false;
        chart.Options.ShowPlotBackground = false;

        var pixels = ReadPngRgba(chart.ToPng(), out _, out _);
        var whiteDetails = CountNearColor(pixels, 255, 255, 255, 16);
        var centerLabelPixels = CountAlphaInRect(pixels, 260, 76, 96, 72, 24);

        Assert(whiteDetails > 80, "PNG donut charts should render light slice separators and percent labels.");
        Assert(centerLabelPixels > 20, "PNG donut charts should render the center total and series label.");
    }

    private static void PngDataLabelsUseReadableHalos() {
        var chart = Chart.Create()
            .WithSize(220, 140)
            .WithDataLabels()
            .AddLine("Values", new[] { new ChartPoint(1, 12), new ChartPoint(2, 18), new ChartPoint(3, 15) }, ChartColor.FromRgb(37, 99, 235));
        chart.Options.ShowAxes = false;
        chart.Options.ShowCard = false;
        chart.Options.ShowGrid = false;
        chart.Options.ShowHeader = false;
        chart.Options.ShowLegend = false;
        chart.Options.ShowPlotBackground = false;

        var pixels = ReadPngRgba(chart.ToPng(), out _, out _);
        var haloPixels = CountNearColor(pixels, 255, 255, 255, 16);
        Assert(haloPixels > 20, $"PNG data labels should render a light halo so labels stay readable over plotted marks. Actual halo pixels: {haloPixels}.");
    }

    private static void PngHeatmapsRenderCellValueLabels() {
        var chart = Chart.Create()
            .WithSize(720, 420)
            .WithPadding(70, 44, 52, 72)
            .WithDataLabels()
            .WithHeatmapScale(ChartHeatmapScale.Semantic)
            .WithValueFormatter(value => value.ToString("0", System.Globalization.CultureInfo.InvariantCulture) + "%")
            .WithXLabels("SPF", "DMARC", "DNSSEC")
            .AddHeatmapRow("Primary", Points(96, 0, 0))
            .AddHeatmapRow("Parked", Points(82, 0, 25));
        chart.Options.ShowCard = false;
        chart.Options.ShowHeader = false;
        chart.Options.ShowLegend = false;
        chart.Options.ShowPlotBackground = false;

        var pixels = ReadPngRgba(chart.ToPng(), out var width, out var height);
        var darkTextPixels = CountNearColor(pixels, 15, 23, 42, 24);
        var lightTextPixels = CountNearColor(pixels, 255, 255, 255, 80);
        var scaleNegativePixels = CountNearColorInRect(pixels, width, width - 230, height - 160, 210, 130, 239, 68, 68, 24);
        var scalePositivePixels = CountNearColorInRect(pixels, width, width - 230, height - 160, 210, 130, 16, 185, 129, 44);

        Assert(darkTextPixels > 20, "PNG heatmap labels should render dark text on light cells.");
        Assert(lightTextPixels > 20, "PNG heatmap labels should render light text on dark cells.");
        Assert(scaleNegativePixels > 8 && scalePositivePixels > 8, "PNG heatmaps should render the heat scale legend.");
    }

    private static void PngTimelinesRenderReadableRasterDetails() {
        var chart = Chart.Create()
            .WithSize(360, 220)
            .WithPadding(70, 24, 24, 50)
            .WithTheme(ChartTheme.Dark())
            .WithXAxis("Schedule")
            .WithYAxis("Workstream")
            .WithDataLabels()
            .AddTimelineRange("Alpha", 10, 90, ChartColor.FromRgb(37, 99, 235))
            .AddTimelineRange("Beta", 24, 70, ChartColor.FromRgb(16, 185, 129));
        chart.Options.ShowCard = false;
        chart.Options.ShowHeader = false;
        chart.Options.ShowLegend = false;
        chart.Options.ShowPlotBackground = false;

        var pixels = ReadPngRgba(chart.ToPng(), out var width, out _);
        var barPixels = CountNearColor(pixels, 37, 99, 235, 18);
        var durationPixels = CountNearColor(pixels, 255, 255, 255, 80);
        var barBounds = FindNearColorBounds(pixels, width, 37, 99, 235, 18);
        var roundedCornerPixels = barBounds.IsEmpty ? 0 : CountNearColorInRect(pixels, width, barBounds.Left, barBounds.Top, 3, 3, 37, 99, 235, 18);
        var roundedBodyPixels = barBounds.IsEmpty ? 0 : CountNearColorInRect(pixels, width, barBounds.Left + 6, barBounds.Top + 6, 3, 3, 37, 99, 235, 18);

        Assert(barPixels > 100, "PNG timelines should render visible range bars.");
        Assert(durationPixels > 20, $"PNG timelines should render readable duration labels. Actual duration label pixels: {durationPixels}.");
        Assert(roundedCornerPixels < roundedBodyPixels, $"PNG timeline bars should use rounded corners. Actual corner/body color pixels: {roundedCornerPixels}/{roundedBodyPixels}.");
    }

    private static void ExampleGalleryIsStaticAndLinksGeneratedArtifacts() {
        var output = Path.Combine(Path.GetTempPath(), "ChartForgeX-gallery-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(output);
        try {
            var alpha = Chart.Create().WithSize(320, 180).WithTitle("Alpha & Beta").AddLine("Values", Points(1, 2, 3));
            File.WriteAllText(Path.Combine(output, "alpha.html"), "<!doctype html><title>Alpha &amp; Beta</title><svg></svg>");
            File.WriteAllText(Path.Combine(output, "alpha.svg"), alpha.ToSvg());
            File.WriteAllBytes(Path.Combine(output, "alpha.png"), alpha.ToPng());
            File.WriteAllText(Path.Combine(output, "zeta.html"), "<!doctype html><title>Zeta</title><svg></svg>");
            File.WriteAllText(Path.Combine(output, "zeta.svg"), Chart.Create().WithSize(640, 360).WithTitle("Zeta").AddBar("Values", Points(1, 2, 3)).ToSvg());
            File.WriteAllBytes(Path.Combine(output, "zeta.png"), Chart.Create().WithSize(640, 360).WithTitle("Zeta").AddBar("Values", Points(1, 2, 3)).ToPng());

            GalleryWriter.Write(output);
            var gallery = File.ReadAllText(Path.Combine(output, "index.html"));
            Assert(gallery.Contains("<title>ChartForgeX Examples</title>", StringComparison.Ordinal), "Gallery should render a stable title.");
            Assert(CountOccurrences(gallery, "<article class=\"card\">") == 2, "Gallery should render one card per generated chart page.");
            Assert(CountOccurrences(gallery, "<iframe ") == 2, "Gallery should render chart previews.");
            Assert(!gallery.Contains("<script", StringComparison.OrdinalIgnoreCase), "Gallery should remain JavaScript-free.");
            AssertSelfContainedMarkup(gallery, "example gallery");
            Assert(gallery.Contains("alpha.html", StringComparison.Ordinal), "Gallery should link chart HTML output.");
            Assert(gallery.Contains("alpha.svg", StringComparison.Ordinal), "Gallery should link chart SVG output.");
            Assert(gallery.Contains("alpha.png", StringComparison.Ordinal), "Gallery should link chart PNG output.");

            var comparison = File.ReadAllText(Path.Combine(output, "svg-png-comparison.html"));
            Assert(comparison.Contains("ChartForgeX SVG/PNG visual comparison", StringComparison.Ordinal), "Gallery writer should generate an SVG/PNG comparison page.");
            Assert(comparison.Contains("alpha.svg", StringComparison.Ordinal) && comparison.Contains("alpha.png", StringComparison.Ordinal), "Comparison page should pair SVG and PNG outputs.");
            Assert(comparison.Contains("zeta.svg", StringComparison.Ordinal) && comparison.Contains("zeta.png", StringComparison.Ordinal), "Comparison page should include every generated chart pair.");
            Assert(comparison.Contains("2 chart pairs", StringComparison.Ordinal), "Comparison page should summarize generated SVG/PNG pairs.");
            Assert(comparison.Contains("2 dimension matches", StringComparison.Ordinal), "Comparison page should summarize SVG/PNG dimension parity.");
            Assert(comparison.Contains("<a class=\"format\" href=\"alpha.svg\">SVG</a>", StringComparison.Ordinal), "Comparison page should link directly to SVG assets.");
            Assert(comparison.Contains("<a class=\"format\" href=\"alpha.png\">PNG</a>", StringComparison.Ordinal), "Comparison page should link directly to PNG assets.");
            Assert(comparison.Contains("<span class=\"format\">OVERLAY</span>", StringComparison.Ordinal), "Comparison page should include an overlay pane for SVG/PNG visual parity review.");
            Assert(comparison.Contains("href=\"svg-png-comparison.json\"", StringComparison.Ordinal), "Comparison page should link its parity manifest.");
            Assert(CountOccurrences(comparison, "Dimension match") == 2, "Comparison page should label matching SVG/PNG dimensions per chart.");
            Assert(comparison.Contains("aspect-ratio:320/180", StringComparison.Ordinal), "Comparison page should preserve chart aspect ratios for fair SVG/PNG review.");
            Assert(comparison.Contains("320x180", StringComparison.Ordinal), "Comparison page should show asset dimensions for SVG/PNG parity review.");
            Assert(!comparison.Contains("<script", StringComparison.OrdinalIgnoreCase), "SVG/PNG comparison page should remain JavaScript-free.");

            var manifest = File.ReadAllText(Path.Combine(output, "svg-png-comparison.json"));
            Assert(manifest.Contains("\"chartPairs\": 2", StringComparison.Ordinal), "Comparison manifest should summarize generated SVG/PNG pairs.");
            Assert(manifest.Contains("\"dimensionMatches\": 2", StringComparison.Ordinal), "Comparison manifest should summarize SVG/PNG dimension parity.");
            Assert(manifest.Contains("\"overlay\"", StringComparison.Ordinal), "Comparison manifest should describe available parity review modes.");
            Assert(manifest.Contains("\"name\": \"alpha\"", StringComparison.Ordinal), "Comparison manifest should list chart assets by name.");
            Assert(manifest.Contains("\"dimensionsMatch\": true", StringComparison.Ordinal), "Comparison manifest should flag dimension parity per chart.");
            Assert(manifest.Contains("\"bytes\":", StringComparison.Ordinal), "Comparison manifest should include asset byte sizes.");
        } finally {
            Directory.Delete(output, true);
        }
    }
}
