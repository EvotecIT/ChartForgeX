using System;
using System.IO;
using System.Linq;
using ChartForgeX;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;

namespace ChartForgeX.Tests;

internal static partial class SmokeTests {
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
            Chart.Create().WithSize(640, 360).AddStepArea("Step area", Points(10, 20, 16)),
            Chart.Create().WithSize(640, 360).WithXLabels("A", "B", "C").AddBar("Bar", Points(10, 20, 16)),
            Chart.Create().WithSize(640, 360).AddScatter("Scatter", Points(10, 20, 16)).AddTrendLine("Trend", Points(10, 20, 16)),
            Chart.Create().WithSize(640, 360).WithXLabels("A", "B", "C").AddHorizontalBar("Horizontal", Points(10, 20, 16)),
            Chart.Create().WithSize(640, 360).AddBubble("Bubble", new[] { new ChartBubble(1, 10, 6), new ChartBubble(2, 20, 16), new ChartBubble(3, 16, 28) }),
            Chart.Create().WithSize(640, 360).AddErrorBar("Error", new[] { new ChartErrorBar(1, 10, 8, 14), new ChartErrorBar(2, 20, 17, 23), new ChartErrorBar(3, 16, 13, 22) }),
            Chart.Create().WithSize(640, 360).AddCandlestick("Candles", new[] { new ChartCandlestick(1, 10, 14, 8, 12), new ChartCandlestick(2, 20, 23, 17, 18), new ChartCandlestick(3, 16, 22, 13, 21) }),
            Chart.Create().WithSize(640, 360).AddOhlc("OHLC", new[] { new ChartCandlestick(1, 10, 14, 8, 12), new ChartCandlestick(2, 20, 23, 17, 18), new ChartCandlestick(3, 16, 22, 13, 21) }),
            Chart.Create().WithSize(640, 360).AddRangeBand("Band", new[] { new ChartRangeBand(1, 8, 14), new ChartRangeBand(2, 17, 23), new ChartRangeBand(3, 13, 22) }),
            Chart.Create().WithSize(640, 360).AddRangeArea("Area", new[] { new ChartRangeBand(1, 8, 14), new ChartRangeBand(2, 17, 23), new ChartRangeBand(3, 13, 22) }),
            Chart.Create().WithSize(640, 360).AddStackedArea("Passed", Points(10, 20, 16)).AddStackedArea("Warnings", Points(2, 4, 3)),
            Chart.Create().WithSize(640, 360).AddSlope("Current", 42, 88).AddSlope("Target", 58, 94),
            Chart.Create().WithSize(640, 360).AddDumbbell("Dumbbell", new[] { new ChartDumbbell(1, 8, 14), new ChartDumbbell(2, 17, 23), new ChartDumbbell(3, 13, 22) }),
            Chart.Create().WithSize(640, 360).AddPareto("Pareto", new[] { new ChartParetoItem("A", 50), new ChartParetoItem("B", 30), new ChartParetoItem("C", 20) }),
            Chart.Create().WithSize(640, 360).WithXLabels("A", "B", "C").AddHeatmapRow("Heat", Points(96, 82, 74)),
            Chart.Create().WithSize(640, 360).AddGauge("Gauge", 87),
            Chart.Create().WithSize(640, 360).AddCircle("Circle", 87),
            Chart.Create().WithSize(640, 360).WithXLabels("A", "B", "C").AddRadialBar("Radial", Points(96, 82, 74)),
            Chart.Create().WithSize(640, 360).AddBullet("Bullet", 82, 90),
            Chart.Create().WithSize(640, 360).AddWaterfall("Waterfall", Points(18, -42, 9)),
            Chart.Create().WithSize(640, 360).WithXLabels("A", "B", "C").AddRadar("Radar", Points(92, 74, 88)),
            Chart.Create().WithSize(640, 360).WithXLabels("A", "B", "C").AddPolarArea("Polar", Points(92, 74, 88)),
            Chart.Create().WithSize(640, 360).WithXLabels("A", "B", "C").AddFunnel("Funnel", Points(420, 318, 174)),
            Chart.Create().WithSize(640, 360).AddTreemap("Treemap", new[] { new ChartTreemapItem("A", 50), new ChartTreemapItem("B", 30), new ChartTreemapItem("C", 20) }),
            Chart.Create().WithSize(640, 360).AddPictorial("Pictorial", new[] { new ChartPictorialItem("A", 50), new ChartPictorialItem("B", 30), new ChartPictorialItem("C", 20) }, ChartPictorialShape.Diamond),
            Chart.Create().WithSize(640, 360).AddWordCloud("WordCloud", new[] { new ChartWordCloudItem("Alpha", 50), new ChartWordCloudItem("Beta", 30), new ChartWordCloudItem("Gamma", 20) }),
            Chart.Create().WithSize(640, 360).AddTimelineItem("Timeline", timelineStart, timelineStart.AddDays(14)),
            Chart.Create().WithSize(640, 360).WithGanttToday(timelineStart.AddDays(8)).AddGanttTask("Gantt", timelineStart, timelineStart.AddDays(14), 0.5),
            Chart.Create().WithSize(640, 360).AddSankey("Sankey", new[] { new ChartSankeyLink("A", "B", 10), new ChartSankeyLink("B", "C", 7) }),
            Chart.Create().WithSize(640, 360).AddTree("Tree", new[] { new ChartTreeLink("A", "B"), new ChartTreeLink("A", "C") }),
            Chart.Create().WithSize(640, 360).AddSunburst("Sunburst", new[] { new ChartTreeLink("A", "B", 10), new ChartTreeLink("A", "C", 7), new ChartTreeLink("B", "D", 4) }),
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

    private static void PngSupersamplingScaleIsConfigurable() {
        var low = Chart.Create()
            .WithSize(180, 120)
            .WithPngSupersampling(1)
            .AddLine("Diagonal", new[] { new ChartPoint(1, 1), new ChartPoint(3, 3) }, ChartColor.FromRgb(96, 165, 250));
        var high = Chart.Create()
            .WithSize(180, 120)
            .WithPngSupersampling(4)
            .AddLine("Diagonal", new[] { new ChartPoint(1, 1), new ChartPoint(3, 3) }, ChartColor.FromRgb(96, 165, 250));
        foreach (var chart in new[] { low, high }) {
            chart.Options.ShowAxes = false;
            chart.Options.ShowCard = false;
            chart.Options.ShowGrid = false;
            chart.Options.ShowHeader = false;
            chart.Options.ShowLegend = false;
            chart.Options.ShowPlotBackground = false;
        }

        var lowPng = low.ToPng();
        var highPng = high.ToPng();
        Assert(ReadBigEndianInt32(lowPng, 16) == 180 && ReadBigEndianInt32(lowPng, 20) == 120, "PNG supersampling should not alter low-quality output dimensions.");
        Assert(ReadBigEndianInt32(highPng, 16) == 180 && ReadBigEndianInt32(highPng, 20) == 120, "PNG supersampling should not alter high-quality output dimensions.");
        Assert(!lowPng.SequenceEqual(highPng), "Changing PNG supersampling should affect raster output.");
        AssertThrows<ArgumentOutOfRangeException>(() => Chart.Create().WithPngSupersampling(0), "PNG supersampling should reject values below one.");
        AssertThrows<ArgumentOutOfRangeException>(() => Chart.Create().Options.PngSupersamplingScale = 5, "PNG supersampling should reject values above four.");
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
        var renderer = string.Join("\n", Directory.EnumerateFiles(Path.Combine(root, "ChartForgeX", "Raster"), "PngChartRenderer*.cs", SearchOption.TopDirectoryOnly)
            .OrderBy(file => file, StringComparer.Ordinal)
            .Select(File.ReadAllText));
        Assert(canvas.Contains("DrawTextEmphasized", StringComparison.Ordinal), "PNG raster canvas should expose an emphasized text path for SVG font-weight parity.");
        Assert(canvas.Contains("MeasureTextEmphasizedWidth", StringComparison.Ordinal), "PNG raster canvas should measure emphasized text with its extra painted width.");
        Assert(canvas.Contains("SampleImageBilinear", StringComparison.Ordinal), "PNG raster canvas should scale composed grid panels with bilinear sampling instead of nearest-neighbor aliasing.");
        Assert(renderer.Contains("DrawTextEmphasized", StringComparison.Ordinal), "PNG chart renderer should use emphasized text for report-grade title, legend, and data labels.");
        Assert(renderer.Contains("EstimatePngEmphasizedTextWidth", StringComparison.Ordinal), "PNG chart renderer should center and clamp emphasized labels using emphasized text width.");
        Assert(renderer.Contains("TextFontSizeForEmphasizedWidth", StringComparison.Ordinal), "PNG chart renderer should size emphasized labels using emphasized width so future long labels fit.");
        Assert(renderer.Contains("FitReadablePngLabelFontSize", StringComparison.Ordinal), "PNG chart renderer should shrink clamped readable labels before positioning them.");
        Assert(renderer.Contains("TrimReadablePngLabelToWidth", StringComparison.Ordinal), "PNG chart renderer should shorten readable labels that cannot fit after shrinking.");
        Assert(renderer.Contains("DrawReadablePngLabelCentered", StringComparison.Ordinal), "PNG chart renderer should share centered bounded readable label placement for rectangular chart surfaces.");
        Assert(renderer.Contains("DrawPngTextEmphasizedCenteredX", StringComparison.Ordinal), "PNG chart renderer should share centered emphasized text placement for center labels.");
        Assert(renderer.Contains("double maxWidth", StringComparison.Ordinal), "PNG centered emphasized text should support bounded fitting for center labels.");
        Assert(renderer.Contains("PngLegendLabel", StringComparison.Ordinal), "PNG chart renderer should draw and measure bounded legend labels consistently.");
        Assert(renderer.Contains("TrimPngLabelToWidth", StringComparison.Ordinal), "PNG chart renderer should trim regular-weight text such as subtitles after fitting font size.");
        var pngAxes = File.ReadAllText(Path.Combine(root, "ChartForgeX", "Raster", "PngChartRenderer.Axes.cs"));
        Assert(pngAxes.Contains("DrawPngXAxisTitle", StringComparison.Ordinal), "PNG chart renderer should share bounded x-axis title placement.");
        Assert(pngAxes.Contains("TrimReadablePngLabelToWidth(chart.YAxisTitle", StringComparison.Ordinal), "PNG chart renderer should trim y-axis titles after fitting font size.");
        var svgHelpers = File.ReadAllText(Path.Combine(root, "ChartForgeX", "Svg", "SvgChartRenderer.Helpers.cs"));
        Assert(svgHelpers.Contains("TrimSvgLabelToWidth", StringComparison.Ordinal), "SVG chart renderer should share bounded label trimming for long formatter output.");
        Assert(svgHelpers.Contains("TextFontSizeForSvgWidth", StringComparison.Ordinal), "SVG chart renderer should shrink labels before trimming when chart surfaces are constrained.");
        Assert(svgHelpers.Contains("DrawSvgTextCenteredX", StringComparison.Ordinal), "SVG chart renderer should share centered bounded label placement for specialized chart surfaces.");
        Assert(svgHelpers.Contains("DrawSvgTextLeft", StringComparison.Ordinal), "SVG chart renderer should share left-aligned bounded label placement for header text.");
        Assert(svgHelpers.Contains("SvgLegendLabel", StringComparison.Ordinal), "SVG chart renderer should draw and measure bounded legend labels consistently.");
        Assert(svgHelpers.Contains("DrawSvgXAxisTitle", StringComparison.Ordinal), "SVG chart renderer should share bounded x-axis title placement.");
        Assert(svgHelpers.Contains("DrawSvgYAxisTitle", StringComparison.Ordinal), "SVG chart renderer should share bounded y-axis title placement.");
    }

    private static void PngChartRendererUsesThemeSizedText() {
        var root = FindRepositoryRoot();
        var rendererFiles = Directory.EnumerateFiles(Path.Combine(root, "ChartForgeX", "Raster"), "PngChartRenderer*.cs", SearchOption.TopDirectoryOnly).ToArray();
        foreach (var file in rendererFiles) {
            var source = File.ReadAllText(file);
            Assert(!source.Contains("DrawTextTiny", StringComparison.Ordinal), "PNG chart renderers should use theme-sized outline text instead of tiny text calls: " + Path.GetRelativePath(root, file));
        }
    }

    private static void PngAndSvgStrokePrimitivesStayAligned() {
        var root = FindRepositoryRoot();
        var primitives = File.ReadAllText(Path.Combine(root, "ChartForgeX", "Rendering", "ChartVisualPrimitives.cs"));
        var svg = File.ReadAllText(Path.Combine(root, "ChartForgeX", "Svg", "SvgChartRenderer.cs"));
        var svgRadar = File.ReadAllText(Path.Combine(root, "ChartForgeX", "Svg", "SvgChartRenderer.Radar.cs"));
        var svgRadial = File.ReadAllText(Path.Combine(root, "ChartForgeX", "Svg", "SvgChartRenderer.RadialBar.cs"));
        var svgPolar = File.ReadAllText(Path.Combine(root, "ChartForgeX", "Svg", "SvgChartRenderer.PolarArea.cs"));
        var svgTimeline = File.ReadAllText(Path.Combine(root, "ChartForgeX", "Svg", "SvgChartRenderer.Timeline.cs"));
        var svgGantt = File.ReadAllText(Path.Combine(root, "ChartForgeX", "Svg", "SvgChartRenderer.Gantt.cs"));
        var svgSankey = File.ReadAllText(Path.Combine(root, "ChartForgeX", "Svg", "SvgChartRenderer.Sankey.cs"));
        var svgFunnel = File.ReadAllText(Path.Combine(root, "ChartForgeX", "Svg", "SvgChartRenderer.Funnel.cs"));
        var svgTreemap = File.ReadAllText(Path.Combine(root, "ChartForgeX", "Svg", "SvgChartRenderer.Treemap.cs"));
        var svgHeatmap = File.ReadAllText(Path.Combine(root, "ChartForgeX", "Svg", "SvgChartRenderer.Heatmap.cs"));
        var svgTree = File.ReadAllText(Path.Combine(root, "ChartForgeX", "Svg", "SvgChartRenderer.Tree.cs"));
        var svgBullet = File.ReadAllText(Path.Combine(root, "ChartForgeX", "Svg", "SvgChartRenderer.Bullet.cs"));
        var svgRangeBand = File.ReadAllText(Path.Combine(root, "ChartForgeX", "Svg", "SvgChartRenderer.RangeBand.cs"));
        var svgRangeArea = File.ReadAllText(Path.Combine(root, "ChartForgeX", "Svg", "SvgChartRenderer.RangeArea.cs"));
        var svgWaterfall = File.ReadAllText(Path.Combine(root, "ChartForgeX", "Svg", "SvgChartRenderer.Waterfall.cs"));
        var svgLegend = File.ReadAllText(Path.Combine(root, "ChartForgeX", "Svg", "SvgChartRenderer.Helpers.cs"));
        var canvas = File.ReadAllText(Path.Combine(root, "ChartForgeX", "Raster", "RgbaCanvas.cs"));
        var png = File.ReadAllText(Path.Combine(root, "ChartForgeX", "Raster", "PngChartRenderer.cs"));
        var cartesian = File.ReadAllText(Path.Combine(root, "ChartForgeX", "Raster", "PngChartRenderer.Cartesian.cs"));
        var radar = File.ReadAllText(Path.Combine(root, "ChartForgeX", "Raster", "PngChartRenderer.Radar.cs"));
        var radial = File.ReadAllText(Path.Combine(root, "ChartForgeX", "Raster", "PngChartRenderer.RadialBar.cs"));
        var polar = File.ReadAllText(Path.Combine(root, "ChartForgeX", "Raster", "PngChartRenderer.PolarArea.cs"));
        var timeline = File.ReadAllText(Path.Combine(root, "ChartForgeX", "Raster", "PngChartRenderer.Timeline.cs"));
        var gantt = File.ReadAllText(Path.Combine(root, "ChartForgeX", "Raster", "PngChartRenderer.Gantt.cs"));
        var sankey = File.ReadAllText(Path.Combine(root, "ChartForgeX", "Raster", "PngChartRenderer.Sankey.cs"));
        var funnel = File.ReadAllText(Path.Combine(root, "ChartForgeX", "Raster", "PngChartRenderer.Funnel.cs"));
        var treemap = File.ReadAllText(Path.Combine(root, "ChartForgeX", "Raster", "PngChartRenderer.Treemap.cs"));
        var heatmap = File.ReadAllText(Path.Combine(root, "ChartForgeX", "Raster", "PngChartRenderer.Heatmap.cs"));
        var tree = File.ReadAllText(Path.Combine(root, "ChartForgeX", "Raster", "PngChartRenderer.Tree.cs"));
        var bullet = File.ReadAllText(Path.Combine(root, "ChartForgeX", "Raster", "PngChartRenderer.Bullet.cs"));
        var rangeBand = File.ReadAllText(Path.Combine(root, "ChartForgeX", "Raster", "PngChartRenderer.RangeBand.cs"));
        var rangeArea = File.ReadAllText(Path.Combine(root, "ChartForgeX", "Raster", "PngChartRenderer.RangeArea.cs"));
        var waterfall = File.ReadAllText(Path.Combine(root, "ChartForgeX", "Raster", "PngChartRenderer.Waterfall.cs"));
        var legend = File.ReadAllText(Path.Combine(root, "ChartForgeX", "Raster", "PngChartRenderer.Legend.cs"));
        Assert(primitives.Contains("StrokeHaloOpacity", StringComparison.Ordinal) && primitives.Contains("PngTextHaloOuterOpacity", StringComparison.Ordinal) && primitives.Contains("MarkerStrokeWidth", StringComparison.Ordinal) && primitives.Contains("RadarOutlineStrokeWidth", StringComparison.Ordinal) && primitives.Contains("TreemapTileBorderOpacity", StringComparison.Ordinal) && primitives.Contains("OhlcStrokeWidth", StringComparison.Ordinal) && primitives.Contains("WaterfallConnectorStrokeWidth", StringComparison.Ordinal), "Shared visual primitive constants should define stroke halo, text halo, marker, radial, flow, finance, range, and tile contracts.");
        Assert(canvas.Contains("DrawLine(double x0, double y0, double x1, double y1, ChartColor color, double thickness)", StringComparison.Ordinal), "PNG canvas should preserve fractional stroke widths for SVG/PNG parity.");
        Assert(canvas.Contains("DrawArc(double cx, double cy, double radius, double startAngle, double endAngle, ChartColor color, double thickness)", StringComparison.Ordinal), "PNG canvas should preserve fractional arc widths for SVG/PNG parity.");
        Assert(canvas.Contains("StrokeRoundedRect(double x, double y, double width, double height, double radius, ChartColor color, double thickness", StringComparison.Ordinal), "PNG canvas should preserve fractional rounded-rectangle stroke widths for SVG/PNG parity.");
        Assert(svg.Contains("ChartVisualPrimitives.AxisStrokeWidth", StringComparison.Ordinal) && png.Contains("ChartVisualPrimitives.AxisStrokeWidth", StringComparison.Ordinal), "SVG and PNG axes should use the same shared stroke widths.");
        Assert(svg.Contains("ChartVisualPrimitives.AnnotationLineStrokeWidth", StringComparison.Ordinal) && png.Contains("ChartVisualPrimitives.AnnotationLineStrokeWidth", StringComparison.Ordinal), "SVG and PNG annotation lines should share overlay stroke width.");
        Assert(svgLegend.Contains("ChartVisualPrimitives.LegendLineStrokeWidth", StringComparison.Ordinal) && legend.Contains("ChartVisualPrimitives.LegendLineStrokeWidth", StringComparison.Ordinal), "SVG and PNG legends should share line swatch stroke width.");
        Assert(svgLegend.Contains("ChartVisualPrimitives.LegendMarkerRadius", StringComparison.Ordinal) && legend.Contains("ChartVisualPrimitives.LegendMarkerRadius", StringComparison.Ordinal), "SVG and PNG legends should share marker sizing contracts.");
        Assert(svgBullet.Contains("ChartVisualPrimitives.BulletAxisStrokeWidth", StringComparison.Ordinal) && bullet.Contains("ChartVisualPrimitives.BulletAxisStrokeWidth", StringComparison.Ordinal), "SVG and PNG bullet axes should share tick stroke width.");
        Assert(svg.Contains("ChartVisualPrimitives.LineHaloStrokeExtra", StringComparison.Ordinal) && cartesian.Contains("ChartVisualPrimitives.LineHaloStrokeExtra", StringComparison.Ordinal), "SVG and PNG line halos should use the same shared halo width.");
        Assert(svg.Contains("ChartVisualPrimitives.StrokeHaloOpacity", StringComparison.Ordinal) && cartesian.Contains("ChartVisualPrimitives.StrokeHaloOpacity", StringComparison.Ordinal), "SVG and PNG stroke halos should use the same shared opacity.");
        Assert(svg.Contains("ChartVisualPrimitives.MarkerStrokeWidth", StringComparison.Ordinal) && cartesian.Contains("ChartVisualPrimitives.PngMarkerOutlineRadiusExtra", StringComparison.Ordinal), "SVG and PNG markers should use the same shared marker outline contract.");
        Assert(svg.Contains("chart.Options.Theme.MarkerRadius", StringComparison.Ordinal) && cartesian.Contains("chart.Options.Theme.MarkerRadius", StringComparison.Ordinal), "SVG and PNG line markers should use theme marker radius.");
        Assert(svgRadar.Contains("ChartVisualPrimitives.RadarOutlineStrokeWidth", StringComparison.Ordinal) && radar.Contains("ChartVisualPrimitives.RadarOutlineStrokeWidth", StringComparison.Ordinal), "SVG and PNG radar outlines should share stroke width.");
        Assert(svgRadar.Contains("ChartVisualPrimitives.RadarRingOpacity", StringComparison.Ordinal) && radar.Contains("ChartVisualPrimitives.RadarRingOpacity", StringComparison.Ordinal), "SVG and PNG radar grids should share opacity.");
        Assert(svgRadial.Contains("ChartVisualPrimitives.RadialTrackOpacity", StringComparison.Ordinal) && radial.Contains("ChartVisualPrimitives.RadialTrackOpacity", StringComparison.Ordinal), "SVG and PNG radial tracks should share opacity.");
        Assert(!radial.Contains("DrawRadialBarEndpoint", StringComparison.Ordinal), "PNG radial bars should rely on arc caps instead of drawing a second endpoint marker.");
        Assert(svgPolar.Contains("ChartVisualPrimitives.SliceSeparatorStrokeWidth", StringComparison.Ordinal) && polar.Contains("ChartVisualPrimitives.SliceSeparatorStrokeWidth", StringComparison.Ordinal), "SVG and PNG polar/pie slice separators should share stroke width.");
        Assert(svgTimeline.Contains("ChartVisualPrimitives.TimelineRowGridOpacity", StringComparison.Ordinal) && timeline.Contains("ChartVisualPrimitives.TimelineRowGridOpacity", StringComparison.Ordinal), "SVG and PNG timelines should share row grid opacity.");
        Assert(svgTimeline.Contains("ChartVisualPrimitives.TimelineItemBorderStrokeWidth", StringComparison.Ordinal) && timeline.Contains("ChartVisualPrimitives.TimelineItemBorderStrokeWidth", StringComparison.Ordinal), "SVG and PNG timeline items should share border stroke width.");
        Assert(svgGantt.Contains("ChartVisualPrimitives.GanttTaskBorderOpacity", StringComparison.Ordinal) && gantt.Contains("ChartVisualPrimitives.GanttTaskBorderOpacity", StringComparison.Ordinal), "SVG and PNG Gantt tasks should share border opacity.");
        Assert(svgGantt.Contains("ChartVisualPrimitives.GanttTaskBorderStrokeWidth", StringComparison.Ordinal) && gantt.Contains("ChartVisualPrimitives.GanttTaskBorderStrokeWidth", StringComparison.Ordinal), "SVG and PNG Gantt tasks should share border stroke width.");
        Assert(svgGantt.Contains("ChartVisualPrimitives.GanttDependencyStrokeWidth", StringComparison.Ordinal) && gantt.Contains("ChartVisualPrimitives.GanttDependencyStrokeWidth", StringComparison.Ordinal), "SVG and PNG Gantt dependencies should share stroke width.");
        Assert(svgGantt.Contains("ChartVisualPrimitives.GanttTodayStrokeWidth", StringComparison.Ordinal) && gantt.Contains("ChartVisualPrimitives.GanttTodayStrokeWidth", StringComparison.Ordinal), "SVG and PNG Gantt today markers should share stroke width.");
        Assert(svgSankey.Contains("ChartVisualPrimitives.SankeyLinkFillOpacity", StringComparison.Ordinal) && sankey.Contains("ChartVisualPrimitives.SankeyLinkFillOpacity", StringComparison.Ordinal), "SVG and PNG Sankey links should share fill opacity.");
        Assert(svgSankey.Contains("ChartVisualPrimitives.SankeyNodeBorderStrokeWidth", StringComparison.Ordinal) && sankey.Contains("ChartVisualPrimitives.SankeyNodeBorderStrokeWidth", StringComparison.Ordinal), "SVG and PNG Sankey nodes should share border stroke width.");
        Assert(svgFunnel.Contains("ChartVisualPrimitives.FunnelSegmentStrokeWidth", StringComparison.Ordinal) && funnel.Contains("ChartVisualPrimitives.FunnelSegmentStrokeWidth", StringComparison.Ordinal), "SVG and PNG funnel segments should share stroke width.");
        Assert(svgTreemap.Contains("ChartVisualPrimitives.TreemapTileBorderOpacity", StringComparison.Ordinal) && treemap.Contains("ChartVisualPrimitives.TreemapTileBorderOpacity", StringComparison.Ordinal), "SVG and PNG treemap tiles should share border opacity.");
        Assert(svgTreemap.Contains("ChartVisualPrimitives.TreemapTileBorderStrokeWidth", StringComparison.Ordinal) && treemap.Contains("ChartVisualPrimitives.TreemapTileBorderStrokeWidth", StringComparison.Ordinal), "SVG and PNG treemap tiles should share border stroke width.");
        Assert(svgHeatmap.Contains("ChartVisualPrimitives.HeatmapCellBorderOpacity", StringComparison.Ordinal) && heatmap.Contains("ChartVisualPrimitives.HeatmapCellBorderOpacity", StringComparison.Ordinal), "SVG and PNG heatmap cells should share border opacity.");
        Assert(svgHeatmap.Contains("ChartVisualPrimitives.HeatmapCellBorderStrokeWidth", StringComparison.Ordinal) && heatmap.Contains("ChartVisualPrimitives.HeatmapCellBorderStrokeWidth", StringComparison.Ordinal), "SVG and PNG heatmap cells should share border stroke width.");
        Assert(svgTree.Contains("ChartVisualPrimitives.TreeNodeMinWidth", StringComparison.Ordinal) && tree.Contains("ChartVisualPrimitives.TreeNodeMinWidth", StringComparison.Ordinal), "SVG and PNG tree layouts should share readable node sizing.");
        Assert(svgTree.Contains("ChartVisualPrimitives.TreeNodeLabelMinFontSize", StringComparison.Ordinal) && tree.Contains("ChartVisualPrimitives.TreeNodeLabelMinFontSize", StringComparison.Ordinal) && svgTree.Contains("TreeNodeLabelLines", StringComparison.Ordinal) && tree.Contains("TreeNodeLabelLines", StringComparison.Ordinal), "SVG and PNG tree labels should share readability sizing and wrapping.");
        Assert(svgTree.Contains("ChartVisualPrimitives.TreeNodeBorderOpacity", StringComparison.Ordinal) && tree.Contains("ChartVisualPrimitives.TreeNodeBorderOpacity", StringComparison.Ordinal), "SVG and PNG tree nodes should share border opacity.");
        Assert(svgTree.Contains("ChartVisualPrimitives.TreeNodeBorderStrokeWidth", StringComparison.Ordinal) && tree.Contains("ChartVisualPrimitives.TreeNodeBorderStrokeWidth", StringComparison.Ordinal), "SVG and PNG tree nodes should share border stroke width.");
        Assert(!tree.Contains("(int)Math.Round(width)", StringComparison.Ordinal), "PNG tree links should not quantize hierarchy stroke widths.");
        Assert(svgTree.Contains("ChartVisualPrimitives.TreeLinkStrokeOpacity", StringComparison.Ordinal) && tree.Contains("ChartVisualPrimitives.TreeLinkStrokeOpacity", StringComparison.Ordinal), "SVG and PNG tree links should share stroke opacity.");
        Assert(tree.Contains("ChartVisualPrimitives.TreeLinkCurveSegments", StringComparison.Ordinal), "PNG tree links should use the shared curve segment count to resemble SVG Bezier links.");
        Assert(svgRangeBand.Contains("ChartVisualPrimitives.RangeBandBoundaryStrokeWidth", StringComparison.Ordinal) && rangeBand.Contains("ChartVisualPrimitives.RangeBandBoundaryStrokeWidth", StringComparison.Ordinal), "SVG and PNG range bands should share boundary stroke width.");
        Assert(svgRangeArea.Contains("ChartVisualPrimitives.RangeAreaMidlineStrokeWidth", StringComparison.Ordinal) && rangeArea.Contains("ChartVisualPrimitives.RangeAreaMidlineStrokeWidth", StringComparison.Ordinal), "SVG and PNG range areas should share midline stroke width.");
        Assert(svgWaterfall.Contains("ChartVisualPrimitives.WaterfallConnectorStrokeWidth", StringComparison.Ordinal) && waterfall.Contains("ChartVisualPrimitives.WaterfallConnectorStrokeWidth", StringComparison.Ordinal), "SVG and PNG waterfall connectors should share stroke width.");
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

    private static void PngReadableLabelsFitInsidePlotBounds() {
        var chart = Chart.Create()
            .WithSize(180, 120)
            .WithPadding(24, 16, 18, 24)
            .WithDataLabels()
            .WithValueFormatter(_ => "Extremely long remediation status label that must fit")
            .AddBar("Values", Points(72), ChartColor.FromRgb(37, 99, 235));
        chart.Options.ShowAxes = false;
        chart.Options.ShowCard = false;
        chart.Options.ShowGrid = false;
        chart.Options.ShowHeader = false;
        chart.Options.ShowLegend = false;
        chart.Options.ShowPlotBackground = false;

        var pixels = ReadPngRgba(chart.ToPng(), out var width, out var height);
        var labelPixels = CountNearColor(pixels, 15, 23, 42, 32);
        var rightEdgeLabelPixels = CountNearColorInRect(pixels, width, width - 4, 0, 4, height, 15, 23, 42, 32);

        Assert(labelPixels > 8, "PNG readable labels should remain visible after fitting long formatter output.");
        Assert(rightEdgeLabelPixels == 0, $"PNG readable labels should fit before clamping instead of being clipped at the canvas edge. Actual right-edge label pixels: {rightEdgeLabelPixels}.");
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
            File.WriteAllText(Path.Combine(output, "alpha 2.html"), "<!doctype html><title>Alpha Copy</title><svg></svg>");
            File.WriteAllText(Path.Combine(output, "alpha 2.svg"), alpha.ToSvg());
            File.WriteAllBytes(Path.Combine(output, "alpha 2.png"), alpha.ToPng());
            File.WriteAllText(Path.Combine(output, "zeta.html"), "<!doctype html><title>Zeta</title><svg></svg>");
            File.WriteAllText(Path.Combine(output, "zeta.svg"), Chart.Create().WithSize(640, 360).WithTitle("Zeta").AddBar("Values", Points(1, 2, 3)).ToSvg());
            File.WriteAllBytes(Path.Combine(output, "zeta.png"), Chart.Create().WithSize(640, 360).WithTitle("Zeta").AddBar("Values", Points(1, 2, 3)).ToPng());
            File.WriteAllText(Path.Combine(output, "dashboard-chart-portfolio-grid.html"), "<!doctype html><title>Dashboard Chart Portfolio</title><svg></svg>"); File.WriteAllText(Path.Combine(output, "dashboard-chart-portfolio-grid.svg"), alpha.ToSvg()); File.WriteAllBytes(Path.Combine(output, "dashboard-chart-portfolio-grid.png"), alpha.ToPng());
            File.WriteAllText(Path.Combine(output, "travel-dotted-map-dark.html"), "<!doctype html><title>Travel Dotted Map</title><svg></svg>");
            File.WriteAllText(Path.Combine(output, "report.html"), "<!doctype html><title>Report</title><svg></svg>");
            File.WriteAllText(Path.Combine(output, "visual-baseline.json"), "{\"version\":1,\"charts\":[{\"name\":\"alpha\",\"width\":320,\"height\":180,\"svg\":{\"minVisualNodes\":2,\"maxClippedTextNodes\":0,\"maxNearEdgeTextNodes\":999},\"png\":{\"outputScale\":1,\"minVisiblePixels\":64,\"minDistinctColors\":8,\"maxEdgeInkPixels\":0}},{\"name\":\"zeta\",\"width\":640,\"height\":360,\"svg\":{\"minVisualNodes\":2,\"maxClippedTextNodes\":0,\"maxNearEdgeTextNodes\":999},\"png\":{\"outputScale\":1,\"minVisiblePixels\":64,\"minDistinctColors\":8,\"maxEdgeInkPixels\":0}}]}");

            GalleryWriter.Write(output);
            var gallery = File.ReadAllText(Path.Combine(output, "index.html"));
            Assert(gallery.Contains("<title>ChartForgeX Examples</title>", StringComparison.Ordinal), "Gallery should render a stable title.");
            Assert(CountOccurrences(gallery, "<article class=\"card\">") == 5, "Gallery should render one card per generated chart page.");
            Assert(CountOccurrences(gallery, "<img loading=\"lazy\"") == 3, "Gallery should preview generated SVG artifacts directly when they exist.");
            Assert(CountOccurrences(gallery, "<iframe ") == 2, "Gallery should keep iframe previews only for HTML-only reports.");
            Assert(gallery.Contains("--preview-aspect:", StringComparison.Ordinal), "Gallery SVG previews should carry bounded per-chart aspect ratios.");
            Assert(gallery.Contains("align-items:start", StringComparison.Ordinal), "Gallery cards should avoid row stretching that creates empty card interiors.");
            Assert(!gallery.Contains("<script", StringComparison.OrdinalIgnoreCase), "Gallery should remain JavaScript-free.");
            AssertSelfContainedMarkup(gallery, "example gallery");
            Assert(gallery.Contains("alpha.html", StringComparison.Ordinal), "Gallery should link chart HTML output.");
            Assert(gallery.Contains("alpha.svg", StringComparison.Ordinal), "Gallery should link chart SVG output.");
            Assert(gallery.Contains("alpha.png", StringComparison.Ordinal), "Gallery should link chart PNG output.");
            Assert(gallery.Contains("catalog.html", StringComparison.Ordinal), "Gallery should link the grouped catalog page.");
            Assert(gallery.Contains("quality-dashboard.html", StringComparison.Ordinal), "Gallery should link the artifact quality dashboard.");
            Assert(gallery.Contains("svg-png-comparison.html", StringComparison.Ordinal), "Gallery should link the SVG/PNG comparison page.");
            Assert(gallery.Contains("report.html", StringComparison.Ordinal), "Gallery should link HTML-only report output.");
            Assert(!gallery.Contains("alpha 2.html", StringComparison.Ordinal), "Gallery should ignore duplicate conflict-copy chart pages when the canonical page exists.");
            Assert(!gallery.Contains("report.svg", StringComparison.Ordinal), "Gallery should not link missing SVG output for HTML-only reports.");
            Assert(!gallery.Contains("report.png", StringComparison.Ordinal), "Gallery should not link missing PNG output for HTML-only reports.");

            var catalog = File.ReadAllText(Path.Combine(output, "catalog.html"));
            Assert(catalog.Contains("<title>ChartForgeX Chart Catalog</title>", StringComparison.Ordinal), "Catalog should render a stable title.");
            Assert(catalog.Contains("Additional Examples", StringComparison.Ordinal), "Catalog should include uncategorized generated outputs.");
            Assert(catalog.Contains("Maps and Geography", StringComparison.Ordinal), "Catalog should expose maps as a named chart family.");
            Assert(catalog.Contains("travel-dotted-map-dark.html", StringComparison.Ordinal), "Catalog should route map examples into the map family.");
            Assert(catalog.Contains("Dashboard Patterns", StringComparison.Ordinal) && catalog.Contains("dashboard-chart-portfolio-grid.html", StringComparison.Ordinal), "Catalog should route dashboard outputs into the dashboard family.");
            Assert(catalog.Contains("alpha.html", StringComparison.Ordinal), "Catalog should link chart HTML output.");
            Assert(catalog.Contains("alpha.svg", StringComparison.Ordinal), "Catalog should link chart SVG output.");
            Assert(catalog.Contains("alpha.png", StringComparison.Ordinal), "Catalog should link chart PNG output.");
            Assert(catalog.Contains("report.html", StringComparison.Ordinal), "Catalog should link HTML-only report output.");
            Assert(!catalog.Contains("alpha 2.html", StringComparison.Ordinal), "Catalog should ignore duplicate conflict-copy chart pages when the canonical page exists.");
            Assert(!catalog.Contains("report.svg", StringComparison.Ordinal), "Catalog should not link missing SVG output for HTML-only reports.");
            Assert(!catalog.Contains("report.png", StringComparison.Ordinal), "Catalog should not link missing PNG output for HTML-only reports.");
            Assert(catalog.Contains("svg-png-comparison.html", StringComparison.Ordinal), "Catalog should link the SVG/PNG comparison page.");
            Assert(catalog.Contains("quality-dashboard.html", StringComparison.Ordinal), "Catalog should link the artifact quality dashboard.");
            Assert(CountOccurrences(catalog, "<article class=\"card\">") == 5, "Catalog should render one card per generated chart page.");
            Assert(CountOccurrences(catalog, "<img loading=\"lazy\"") == 3, "Catalog should preview generated SVG artifacts directly when they exist.");
            Assert(catalog.Contains("--preview-aspect:", StringComparison.Ordinal), "Catalog SVG previews should carry bounded per-chart aspect ratios.");
            Assert(catalog.Contains("align-items:start", StringComparison.Ordinal), "Catalog cards should avoid row stretching that creates empty card interiors.");
            Assert(!catalog.Contains("<script", StringComparison.OrdinalIgnoreCase), "Catalog should remain JavaScript-free.");
            AssertSelfContainedMarkup(catalog, "chart catalog");

            var dashboard = File.ReadAllText(Path.Combine(output, "quality-dashboard.html"));
            Assert(dashboard.Contains("<title>ChartForgeX Quality Dashboard</title>", StringComparison.Ordinal), "Quality dashboard should render a stable title.");
            Assert(dashboard.Contains("Chart pairs", StringComparison.Ordinal) && dashboard.Contains("Clean pairs", StringComparison.Ordinal), "Quality dashboard should summarize generated artifact health.");
            Assert(dashboard.Contains("2 pairs", StringComparison.Ordinal) && dashboard.Contains("2 clean", StringComparison.Ordinal), "Quality dashboard should summarize clean SVG/PNG pairs.");
            Assert(dashboard.Contains("min text", StringComparison.Ordinal), "Quality dashboard should expose SVG text readability statistics.");
            Assert(dashboard.Contains("min stroke", StringComparison.Ordinal), "Quality dashboard should expose SVG stroke readability statistics.");
            Assert(dashboard.Contains("min marker", StringComparison.Ordinal), "Quality dashboard should expose SVG marker readability statistics.");
            Assert(dashboard.Contains("bounds", StringComparison.Ordinal) && dashboard.Contains("fg", StringComparison.Ordinal), "Quality dashboard should expose PNG foreground bounds statistics.");
            Assert(dashboard.Contains("alpha.svg", StringComparison.Ordinal) && dashboard.Contains("alpha.png", StringComparison.Ordinal), "Quality dashboard should link generated SVG/PNG artifacts.");
            Assert(dashboard.Contains("svg-png-comparison.html#alpha", StringComparison.Ordinal), "Quality dashboard should deep-link chart review sections.");
            Assert(!dashboard.Contains("<script", StringComparison.OrdinalIgnoreCase), "Quality dashboard should remain JavaScript-free.");
            AssertSelfContainedMarkup(dashboard, "quality dashboard");

            var comparison = File.ReadAllText(Path.Combine(output, "svg-png-comparison.html"));
            Assert(comparison.Contains("ChartForgeX SVG/PNG visual comparison", StringComparison.Ordinal), "Gallery writer should generate an SVG/PNG comparison page.");
            Assert(comparison.Contains("alpha.svg", StringComparison.Ordinal) && comparison.Contains("alpha.png", StringComparison.Ordinal), "Comparison page should pair SVG and PNG outputs.");
            Assert(comparison.Contains("zeta.svg", StringComparison.Ordinal) && comparison.Contains("zeta.png", StringComparison.Ordinal), "Comparison page should include every generated chart pair.");
            Assert(!comparison.Contains("alpha 2.svg", StringComparison.Ordinal), "Comparison page should ignore duplicate conflict-copy SVG/PNG pairs when the canonical pair exists.");
            Assert(comparison.Contains("3 chart pairs", StringComparison.Ordinal), "Comparison page should summarize generated SVG/PNG pairs.");
            Assert(comparison.Contains("3 dimension matches", StringComparison.Ordinal), "Comparison page should summarize SVG/PNG dimension parity.");
            Assert(comparison.Contains("3 healthy SVGs", StringComparison.Ordinal), "Comparison page should summarize SVG artifact health.");
            Assert(comparison.Contains("3 healthy PNGs", StringComparison.Ordinal), "Comparison page should summarize PNG artifact health.");
            Assert(comparison.Contains("0 warnings", StringComparison.Ordinal), "Comparison page should summarize review warnings.");
            Assert(comparison.Contains("2 baseline passes", StringComparison.Ordinal), "Comparison page should summarize visual-baseline matches.");
            Assert(comparison.Contains("1 baseline warnings", StringComparison.Ordinal), "Comparison page should summarize visual-baseline warnings.");
            Assert(comparison.Contains("Comparison families", StringComparison.Ordinal), "Comparison page should expose family jump navigation.");
            Assert(comparison.Contains("Additional Visual Checks", StringComparison.Ordinal), "Comparison page should group uncategorized generated pairs.");
            Assert(comparison.Contains("comparison-family-additional-visual-checks", StringComparison.Ordinal), "Comparison page family groups should expose stable anchors.");
            Assert(comparison.Contains("Additional Visual Checks (2/2 clean)", StringComparison.Ordinal), "Comparison family navigation should summarize clean pair counts.");
            Assert(comparison.Contains("2 pairs / 2 clean / 0 warnings", StringComparison.Ordinal), "Comparison family headers should summarize family health.");
            Assert(comparison.Contains("<a class=\"format\" href=\"alpha.svg\">SVG</a>", StringComparison.Ordinal), "Comparison page should link directly to SVG assets.");
            Assert(comparison.Contains("<a class=\"format\" href=\"alpha.png\">PNG</a>", StringComparison.Ordinal), "Comparison page should link directly to PNG assets.");
            Assert(comparison.Contains("<span class=\"format\">WIPE</span>", StringComparison.Ordinal), "Comparison page should include a center-wipe pane for SVG/PNG visual parity review.");
            Assert(comparison.Contains("<figure class=\"wipe-figure\">", StringComparison.Ordinal), "Comparison page should promote wipe previews to a full-width primary review pane.");
            Assert(comparison.Contains("class=\"wipe-controls\"", StringComparison.Ordinal), "Comparison page should include script-free wipe controls for SVG/PNG parity review.");
            Assert(comparison.Contains("SVG 25%", StringComparison.Ordinal) && comparison.Contains("SVG 75%", StringComparison.Ordinal), "Comparison page should offer fixed wipe positions without requiring JavaScript.");
            Assert(comparison.Contains("clip-path:inset(0 calc(100% - var(--wipe)) 0 0)", StringComparison.Ordinal), "Comparison page should keep SVG and PNG framed together while moving the wipe split.");
            Assert(comparison.Contains(".pair{display:grid;grid-template-columns:repeat(2", StringComparison.Ordinal), "Comparison page should avoid squeezing SVG, PNG, and wipe panes into three narrow columns.");
            Assert(comparison.Contains("href=\"catalog.html\"", StringComparison.Ordinal), "Comparison page should link the grouped catalog page.");
            Assert(comparison.Contains("href=\"quality-dashboard.html\"", StringComparison.Ordinal), "Comparison page should link the artifact quality dashboard.");
            Assert(comparison.Contains("href=\"svg-png-comparison.json\"", StringComparison.Ordinal), "Comparison page should link its parity manifest.");
            Assert(comparison.Contains("<section id=\"alpha\"", StringComparison.Ordinal), "Comparison page should expose stable chart anchors.");
            Assert(CountOccurrences(comparison, "Review clean") == 3, "Comparison page should label warning-free chart pairs.");
            Assert(comparison.Contains("aspect-ratio:320/180", StringComparison.Ordinal) && comparison.Contains("max-width:320px", StringComparison.Ordinal), "Comparison page should preserve chart aspect ratios without upscaling PNG previews beyond their exported dimensions.");
            Assert(comparison.Contains("320x180", StringComparison.Ordinal), "Comparison page should show asset dimensions for SVG/PNG parity review.");
            Assert(comparison.Contains("visual nodes", StringComparison.Ordinal), "Comparison page should expose simple SVG visibility statistics.");
            Assert(comparison.Contains("min text", StringComparison.Ordinal), "Comparison page should expose SVG text readability statistics.");
            Assert(comparison.Contains("min stroke", StringComparison.Ordinal), "Comparison page should expose SVG stroke readability statistics.");
            Assert(comparison.Contains("min marker", StringComparison.Ordinal), "Comparison page should expose SVG marker readability statistics.");
            Assert(comparison.Contains("visible px", StringComparison.Ordinal), "Comparison page should expose simple PNG visibility statistics.");
            Assert(!comparison.Contains("<script", StringComparison.OrdinalIgnoreCase), "SVG/PNG comparison page should remain JavaScript-free.");
            var manifest = File.ReadAllText(Path.Combine(output, "svg-png-comparison.json"));
            Assert(manifest.Contains("\"chartPairs\": 3", StringComparison.Ordinal), "Comparison manifest should summarize generated SVG/PNG pairs.");
            Assert(manifest.Contains("\"dimensionMatches\": 3", StringComparison.Ordinal), "Comparison manifest should summarize SVG/PNG dimension parity.");
            Assert(manifest.Contains("\"healthySvgs\": 3", StringComparison.Ordinal), "Comparison manifest should summarize healthy SVG artifacts.");
            Assert(manifest.Contains("\"healthyPngs\": 3", StringComparison.Ordinal), "Comparison manifest should summarize healthy PNG artifacts.");
            Assert(manifest.Contains("\"warnings\": 0", StringComparison.Ordinal), "Comparison manifest should summarize review warnings.");
            Assert(manifest.Contains("\"baseline\":", StringComparison.Ordinal), "Comparison manifest should summarize visual-baseline status.");
            Assert(manifest.Contains("\"chartMatches\": 2", StringComparison.Ordinal), "Comparison manifest should include visual-baseline match counts.");
            Assert(manifest.Contains("\"clean\": false", StringComparison.Ordinal), "Comparison manifest should flag visual-baseline status when generated pairs are not yet baselined.");
            Assert(manifest.Contains("\"healthThresholds\":", StringComparison.Ordinal), "Comparison manifest should describe artifact health thresholds.");
            Assert(manifest.Contains("\"svgVisualNodes\": 2", StringComparison.Ordinal), "Comparison manifest should describe the minimum SVG visual-node threshold.");
            Assert(manifest.Contains("\"svgMinimumTextFontSize\": 8", StringComparison.Ordinal), "Comparison manifest should describe the minimum readable SVG text threshold.");
            Assert(manifest.Contains("\"svgMinimumStrokeWidth\": 0.75", StringComparison.Ordinal), "Comparison manifest should describe the minimum readable SVG stroke threshold.");
            Assert(manifest.Contains("\"svgMinimumMarkerRadius\": 3", StringComparison.Ordinal), "Comparison manifest should describe the minimum readable SVG marker threshold.");
            Assert(manifest.Contains("\"pngDistinctColors\": 8", StringComparison.Ordinal) && manifest.Contains("\"pngEdgeInkPixels\": 0", StringComparison.Ordinal), "Comparison manifest should describe PNG health thresholds.");
            Assert(manifest.Contains("\"center-wipe\"", StringComparison.Ordinal), "Comparison manifest should describe available parity review modes.");
            Assert(manifest.Contains("\"preset-wipe\"", StringComparison.Ordinal), "Comparison manifest should describe script-free preset wipe review.");
            Assert(manifest.Contains("\"name\": \"alpha\"", StringComparison.Ordinal), "Comparison manifest should list chart assets by name.");
            Assert(!manifest.Contains("\"name\": \"alpha 2\"", StringComparison.Ordinal), "Comparison manifest should ignore duplicate conflict-copy SVG/PNG pairs when the canonical pair exists.");
            Assert(manifest.Contains("\"dimensionsMatch\": true", StringComparison.Ordinal), "Comparison manifest should flag dimension parity per chart.");
            Assert(manifest.Contains("\"warnings\": []", StringComparison.Ordinal), "Comparison manifest should include per-chart warning lists.");
            Assert(manifest.Contains("\"bytes\":", StringComparison.Ordinal), "Comparison manifest should include asset byte sizes.");
            Assert(manifest.Contains("\"visualNodes\":", StringComparison.Ordinal), "Comparison manifest should include SVG visual-node statistics.");
            Assert(manifest.Contains("\"textNodes\":", StringComparison.Ordinal), "Comparison manifest should include SVG text-node statistics.");
            Assert(manifest.Contains("\"minimumTextFontSize\":", StringComparison.Ordinal) && manifest.Contains("\"tinyTextNodes\":", StringComparison.Ordinal), "Comparison manifest should include SVG text readability statistics.");
            Assert(manifest.Contains("\"minimumStrokeWidth\":", StringComparison.Ordinal) && manifest.Contains("\"tinyStrokeNodes\":", StringComparison.Ordinal), "Comparison manifest should include SVG stroke readability statistics.");
            Assert(manifest.Contains("\"minimumMarkerRadius\":", StringComparison.Ordinal) && manifest.Contains("\"tinyMarkerNodes\":", StringComparison.Ordinal), "Comparison manifest should include SVG marker readability statistics.");
            Assert(manifest.Contains("\"clippedTextNodes\":", StringComparison.Ordinal) && manifest.Contains("\"nearEdgeTextNodes\":", StringComparison.Ordinal), "Comparison manifest should include SVG text-edge statistics.");
            Assert(manifest.Contains("\"visiblePixels\":", StringComparison.Ordinal) && manifest.Contains("\"foregroundPixels\":", StringComparison.Ordinal) && manifest.Contains("\"edgeInkPixels\":", StringComparison.Ordinal), "Comparison manifest should include PNG visibility, foreground, and edge statistics.");
            Assert(manifest.Contains("\"contentBounds\":", StringComparison.Ordinal), "Comparison manifest should include PNG content bounds.");
            Assert(manifest.Contains("\"distinctColors\":", StringComparison.Ordinal), "Comparison manifest should include PNG color diversity statistics.");
            Assert(manifest.Contains("\"healthy\": true", StringComparison.Ordinal), "Comparison manifest should flag healthy PNG artifacts.");
        } finally {
            Directory.Delete(output, true);
        }
    }

    private static void ExampleGalleryBaselineFlagsHighDensityRegressions() {
        var output = Path.Combine(Path.GetTempPath(), "ChartForgeX-gallery-baseline-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(output);
        try {
            var chart = Chart.Create().WithSize(320, 180).WithTitle("Alpha").AddLine("Values", Points(1, 2, 3));
            File.WriteAllText(Path.Combine(output, "alpha.html"), "<!doctype html><title>Alpha</title><svg></svg>");
            File.WriteAllText(Path.Combine(output, "alpha.svg"), chart.ToSvg());
            File.WriteAllBytes(Path.Combine(output, "alpha.png"), chart.ToPng());
            File.WriteAllText(Path.Combine(output, "visual-baseline.json"), "{\"version\":1,\"charts\":[{\"name\":\"alpha\",\"width\":320,\"height\":180,\"svg\":{\"minVisualNodes\":2,\"maxClippedTextNodes\":0,\"maxNearEdgeTextNodes\":999},\"png\":{\"outputScale\":2,\"minVisiblePixels\":64,\"minDistinctColors\":8,\"maxEdgeInkPixels\":0}}]}");

            GalleryWriter.Write(output);

            var manifest = File.ReadAllText(Path.Combine(output, "svg-png-comparison.json"));
            Assert(manifest.Contains("\"chartMatches\": 0", StringComparison.Ordinal), "Gallery manifest should fail baseline matches when PNG output density regresses.");
            Assert(manifest.Contains("\"warnings\": 1", StringComparison.Ordinal), "Gallery manifest should count high-DPI visual-baseline warnings.");
            Assert(manifest.Contains("\"clean\": false", StringComparison.Ordinal), "Gallery manifest should flag the visual baseline as not clean.");
            var dashboard = File.ReadAllText(Path.Combine(output, "quality-dashboard.html"));
            Assert(dashboard.Contains("Baseline warnings", StringComparison.Ordinal) && dashboard.Contains("<div class=\"value\">1</div>", StringComparison.Ordinal), "Quality dashboard should surface high-DPI visual-baseline warnings.");
        } finally {
            Directory.Delete(output, true);
        }
    }
}
