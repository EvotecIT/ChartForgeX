using System;
using System.IO;
using System.Linq;
using ChartForgeX;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
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
            Chart.Create().WithSize(640, 360).AddSankey("Sankey", new[] { new ChartNode("A", "A"), new ChartNode("B", "B"), new ChartNode("C", "C") }, new[] { new ChartFlowLink("flow-1", "A", "B", 10), new ChartFlowLink("flow-2", "B", "C", 7) }),
            Chart.Create().WithSize(640, 360).AddTree("Tree", new[] { new ChartNode("A", "A"), new ChartNode("B", "B"), new ChartNode("C", "C") }, new[] { new ChartTreeLink("A", "B"), new ChartTreeLink("A", "C") }),
            Chart.Create().WithSize(640, 360).AddSunburst("Sunburst", new[] { new ChartNode("A", "A"), new ChartNode("B", "B"), new ChartNode("C", "C"), new ChartNode("D", "D") }, new[] { new ChartTreeLink("A", "B", 10), new ChartTreeLink("A", "C", 7), new ChartTreeLink("B", "D", 4) }),
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
        var chart = Chart.Create().WithTheme(ChartForgeX.Themes.ChartTheme.Light())
            .WithSize(360, 220)
            .WithTitle("Chrome")
            .WithSubtitle("Subtitle")
            .AddLine("Primary", Points(10, 20, 30), ChartColor.FromRgb(37, 99, 235))
            .AddLine("Secondary", Points(12, 18, 26), ChartColor.FromRgb(16, 185, 129));
        chart.Options.ShowAxes = false;
        chart.Options.ShowCard = false;
        chart.Options.ShowGrid = false;
        chart.Options.ShowPlotBackground = false;

        chart.Options.TransparentBackground = true;
        var prepared = PreparedFamily(chart);
        var headings = FamilyLabels(prepared, "frame-heading").ToArray();
        var legends = prepared.Regions.Where(region => region.Role == "legend" && region.Bounds.Height > 0).ToArray();
        Assert(headings.Length == 2 && legends.Length == 2, "Report chrome should retain its title, subtitle and both legend entries.");
        var pixels = ReadPngRgba(chart.ToPng(), out var width, out var height);
        var headerBottom = (int)Math.Ceiling(headings.Max(heading => heading.Baseline - heading.Text.Ascent + heading.Text.Metrics.Height));
        var legendTop = (int)Math.Floor(legends.Min(region => region.Bounds.Top));
        var legendBottom = Math.Min(height, (int)Math.Ceiling(legends.Max(region => region.Bounds.Bottom)));
        var headerAlpha = CountAlphaInRect(pixels, width, 0, 0, width, headerBottom);
        var legendAlpha = CountAlphaInRect(pixels, width, 0, legendTop, width, legendBottom - legendTop);

        Assert(headerAlpha > 300, "PNG renderer should include readable header title and subtitle text.");
        Assert(legendAlpha > 180, $"PNG renderer should include a readable cartesian legend when legends are enabled. Actual alpha pixels: {legendAlpha}.");
    }

    private static void PngOutlineFontsUseEmSizedText() {
        var chart = Chart.Create().WithTheme(ChartForgeX.Themes.ChartTheme.Light())
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

    private static void PngSurfacesUseRoundedCorners() {
        var chart = Chart.Create().WithTheme(ChartForgeX.Themes.ChartTheme.Light())
            .WithSize(160, 100)
            .AddLine("Invisible", new[] { new ChartPoint(1, 1), new ChartPoint(2, 2) }, ChartColor.Transparent);
        chart.Options.ShowAxes = false;
        chart.Options.ShowGrid = false;
        chart.Options.ShowHeader = false;
        chart.Options.ShowLegend = false;
        chart.Options.ShowPlotBackground = false;

        var pixels = ReadPngRgba(chart.ToPng(), out var width, out _);
        var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        var card = prepared.Scene.Nodes.OfType<VisualSceneRectangle>().Single(node => node.Role == "frame-card");
        Assert(card.Radius > 0, "The common frame should retain the theme's rounded card geometry.");
        Assert(CountAlphaInRect(pixels, width, 0, 0, 1, 1) == 0, "PNG card corners should stay transparent outside the rounded radius.");
        Assert(CountAlphaInRect(pixels, width, width / 2, (int)Math.Ceiling(card.Bounds.Top + 1), 1, 1) > 0,
            "PNG card top edge should paint at its declared native bounds after applying rounded corners and shadow room.");
    }

    private static void PngAnnotationsUseReadableRasterStyling() {
        var chart = Chart.Create()
            .WithSize(260, 180)
            .WithPadding(20, 20, 20, 24)
            .AddLine("Hidden", new[] { new ChartPoint(1, 0), new ChartPoint(3, 20) }, ChartColor.Transparent)
            .AddHorizontalLine(10, "target", ChartColor.FromRgb(251, 191, 36));
        // Fix the coordinate domain: automatic nice domains may expand beyond the data.
        chart.Options.YAxis.Minimum = 0;
        chart.Options.YAxis.Maximum = 20;
        chart.Options.ShowAxes = false;
        chart.Options.ShowCard = false;
        chart.Options.ShowGrid = false;
        chart.Options.ShowHeader = false;
        chart.Options.ShowLegend = false;
        chart.Options.ShowPlotBackground = false;

        var pixels = ReadPngRgba(chart.ToPng(), out var width, out _);
        var dashedSamples = CountTransparentSamplesOnRow(pixels, width, 88, 20, 220);
        var lineSamples = 220 - dashedSamples;
        var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        var plate = prepared.Scene.Nodes.OfType<VisualSceneRectangle>().Single(node => node.Role == "annotation-label-backplate");
        var label = prepared.Scene.Nodes.OfType<VisualSceneText>().Single(node => node.Role == "annotation-label");
        var pillAlpha = CountAlphaInRect(pixels, width, (int)Math.Ceiling(plate.Bounds.Left), (int)Math.Ceiling(plate.Bounds.Top),
            Math.Max(1, (int)Math.Floor(plate.Bounds.Width) - 1), Math.Max(1, (int)Math.Floor(plate.Bounds.Height) - 1));

        Assert(lineSamples > 20, "PNG annotation line should render visible dash segments.");
        Assert(dashedSamples > 20, "PNG annotation line should preserve transparent gaps between dash segments.");
        Assert(pillAlpha > 300, "PNG annotation labels should render with a readable filled pill.");
        Assert(ChartColorMath.ContrastRatio(plate.Fill!.Value, label.Color) >= 4.5, "Annotation text should contrast with its actual tinted backplate.");
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

        var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        var labels = prepared.Scene.Nodes.OfType<VisualSceneText>().Where(node => node.Role == "data-label").ToArray();
        Assert(labels.Length > 0 && labels.All(label => label.Stroke.HasValue && label.Stroke.Value.A > 0 && label.StrokeWidth > 0),
            "Visible line data labels should retain opaque native glyph outlines.");
        var pixels = ReadPngRgba(chart.ToPng(), out _, out _);
        var stroke = labels[0].Stroke!.Value;
        var haloPixels = CountNearColor(pixels, stroke.R, stroke.G, stroke.B, 32);
        Assert(haloPixels > 20, $"PNG data labels should render a light halo so labels stay readable over plotted marks. Actual halo pixels: {haloPixels}.");
    }

    private static void PngReadableLabelsFitInsidePlotBounds() {
        var chart = Chart.Create().WithTheme(ChartForgeX.Themes.ChartTheme.Light())
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
        var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        var label = prepared.Scene.Nodes.OfType<VisualSceneText>().Single(node => node.Role == "data-label");
        chart.WithDataLabels(false);
        var plain = ReadPngRgba(chart.ToPng(), out _, out _);
        var labelPixels = 0;
        var rightEdgeLabelPixels = 0;
        var measured = label.Text.Metrics;
        for (var y = 0; y < height; y++) for (var x = 0; x < width; x++) {
            var offset = (y * width + x) * 4;
            if (pixels.AsSpan(offset, 4).SequenceEqual(plain.AsSpan(offset, 4))) continue;
            labelPixels++;
            if (x >= width - 4) rightEdgeLabelPixels++;
            Assert(x >= Math.Floor(label.X) - 1 && x <= Math.Ceiling(label.X + measured.Width) + 1
                && y >= Math.Floor(label.Baseline - label.Text.Ascent) - 1 && y <= Math.Ceiling(label.Baseline - label.Text.Ascent + measured.Height) + 1,
                "Native fitted text ink should stay within the prepared label bounds.");
        }
        Assert(label.Text.Lines.Any(line => line.Text.EndsWith("…", StringComparison.Ordinal)), "Long formatter text should be visibly shortened.");
        Assert(prepared.Regions.Any(region => region.Role == "point" && region.Label?.Contains("Extremely long remediation status label that must fit", StringComparison.Ordinal) == true),
            "Fitting should preserve the complete source text in detached point semantics.");

        Assert(labelPixels > 8, "PNG readable labels should remain visible after fitting long formatter output.");
        Assert(rightEdgeLabelPixels == 0, $"PNG readable labels should fit before clamping instead of being clipped at the canvas edge. Actual right-edge label pixels: {rightEdgeLabelPixels}.");
    }

    private static void PngHeatmapsRenderCellValueLabels() {
        var chart = Chart.Create().WithTheme(ChartForgeX.Themes.ChartTheme.Light())
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

        chart.Options.ShowAxes = false;
        var prepared = PreparedFamily(chart);
        var labels = prepared.Scene.Nodes.OfType<VisualSceneText>().Where(node => node.Role == "data-label").ToArray();
        var cells = prepared.Scene.Nodes.OfType<VisualSceneRectangle>().Where(node => node.Role == "heatmap-cell-shape").ToArray();
        Assert(labels.Length == 6 && cells.Length == 6, "Enabled heatmap values should produce one native label per cell.");
        var pixels = ReadPngRgba(prepared.ToPng(), out var pixelWidth, out var pixelHeight);
        for (var index = 0; index < labels.Length; index++) {
            var label = labels[index]; var ink = label.Color; var fill = cells[index].Fill!.Value;
            Assert(ChartColorMath.ContrastRatio(ink, fill) >= 4.5, "Heatmap ink should meet the common accessible contrast policy.");
            var line = label.Text.Lines.Single();
            var left = Math.Max(0, (int)Math.Floor(label.LineLeft(line)));
            var right = Math.Min(pixelWidth, (int)Math.Ceiling(label.LineLeft(line) + line.Width));
            var top = Math.Max(0, (int)Math.Floor(label.Baseline - label.Text.Ascent));
            var bottom = Math.Min(pixelHeight, (int)Math.Ceiling(label.Baseline - label.Text.Ascent + label.Text.Metrics.Height));
            var dr = ink.R - fill.R; var dg = ink.G - fill.G; var db = ink.B - fill.B;
            var distance = dr * dr + dg * dg + db * db;
            var inkPixels = 0;
            for (var y = top; y < bottom; y++) for (var x = left; x < right; x++) {
                var offset = (y * pixelWidth + x) * 4;
                if (pixels[offset + 3] == 0) continue;
                // Supersampling blends edge pixels with the cell. Verify visible coverage of the
                // actual resolved ink in this label's geometry, without requiring solid glyph cores.
                var coverage = ((pixels[offset] - fill.R) * dr + (pixels[offset + 1] - fill.G) * dg + (pixels[offset + 2] - fill.B) * db) / (double)distance;
                if (coverage < .25 || coverage > 1.01) continue;
                if (Math.Abs(pixels[offset] - (fill.R + coverage * dr)) <= 6 &&
                    Math.Abs(pixels[offset + 1] - (fill.G + coverage * dg)) <= 6 &&
                    Math.Abs(pixels[offset + 2] - (fill.B + coverage * db)) <= 6) inkPixels++;
            }
            Assert(inkPixels >= 8, $"Heatmap label {index} should have visible resolved ink within its measured native bounds. Actual pixels: {inkPixels}.");
        }
        Assert(!prepared.Scene.Nodes.Any(node => node.Role == "heatmap-scale-step"), "Explicit legend suppression should hide the continuous heatmap scale.");
        chart.Options.ShowLegend = true;
        var withScale = PreparedFamily(chart);
        var scaleRegions = withScale.Regions.Where(region => region.Role == "heatmap-scale-step").ToArray();
        var scale = scaleRegions.Select(region => withScale.Scene.Nodes.OfType<VisualSceneRectangle>()
            .Single(node => node.Bounds.Equals(region.Bounds))).ToArray();
        Assert(scale.Length == 5 && scale.Select(node => node.Fill!.Value).Distinct().Count() >= 2,
            "The enabled semantic scale should retain visibly distinct range paints.");
        var scalePixels = ReadPngRgba(withScale.ToPng(), out var width, out _);
        foreach (var swatch in scale) {
            var fill = swatch.Fill!.Value;
            Assert(CountNearColorInRect(scalePixels, width, (int)Math.Floor(swatch.Bounds.Left), (int)Math.Floor(swatch.Bounds.Top),
                (int)Math.Ceiling(swatch.Bounds.Width), (int)Math.Ceiling(swatch.Bounds.Height), fill.R, fill.G, fill.B, 12) > 8,
                "Each retained semantic scale paint should appear at its native PNG swatch bounds.");
        }
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
            var alpha = Chart.Create().WithTheme(ChartForgeX.Themes.ChartTheme.Light()).WithSize(320, 180).WithTitle("Alpha & Beta").AddLine("Values", Points(1, 2, 3));
            File.WriteAllText(Path.Combine(output, "alpha.html"), alpha.ToHtmlPage());
            File.WriteAllText(Path.Combine(output, "alpha.svg"), alpha.ToSvg());
            File.WriteAllBytes(Path.Combine(output, "alpha.png"), alpha.ToPng());
            File.WriteAllText(Path.Combine(output, "alpha 2.html"), alpha.ToHtmlPage());
            File.WriteAllText(Path.Combine(output, "alpha 2.svg"), alpha.ToSvg());
            File.WriteAllBytes(Path.Combine(output, "alpha 2.png"), alpha.ToPng());
            var zeta = Chart.Create().WithTheme(ChartForgeX.Themes.ChartTheme.Light()).WithSize(640, 360).WithTitle("Zeta").AddBar("Values", Points(1, 2, 3));
            File.WriteAllText(Path.Combine(output, "zeta.html"), zeta.ToHtmlPage());
            File.WriteAllText(Path.Combine(output, "zeta.svg"), zeta.ToSvg());
            File.WriteAllBytes(Path.Combine(output, "zeta.png"), zeta.ToPng());
            File.WriteAllText(Path.Combine(output, "zeta.static-only"), "Static output may omit HTML.");
            File.WriteAllText(Path.Combine(output, "dashboard-chart-portfolio-grid.html"), alpha.ToHtmlPage());
            File.WriteAllText(Path.Combine(output, "dashboard-chart-portfolio-grid.svg"), alpha.ToSvg());
            File.WriteAllBytes(Path.Combine(output, "dashboard-chart-portfolio-grid.png"), alpha.ToPng());
            File.WriteAllText(Path.Combine(output, "travel-dotted-map-dark.html"), "<!doctype html><title>Travel Dotted Map</title><svg></svg>");
            File.WriteAllText(Path.Combine(output, "report.html"), "<!doctype html><title>Report</title><svg></svg>");
            File.WriteAllText(Path.Combine(output, "alpha.csharp.txt"), "var chart = Chart.Create().WithTheme(ChartForgeX.Themes.ChartTheme.Light()).WithTitle(\"Alpha & Beta\");");
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
            Assert(gallery.Contains("topology-demo.html", StringComparison.Ordinal), "Gallery should link the topology demo landing page as a checked-in preview artifact.");
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
            Assert(dashboard.Contains("Healthy HTMLs", StringComparison.Ordinal), "Quality dashboard should summarize standalone HTML artifact health.");
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
            Assert(comparison.Contains("3 healthy HTMLs", StringComparison.Ordinal), "Comparison page should summarize HTML artifact health.");
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
            Assert(comparison.Contains("class=\"wipe-frame\"", StringComparison.Ordinal), "Comparison page should keep wipe controls outside the responsive media frame.");
            Assert(comparison.Contains(".wipe-frame>.wipe-25:checked~.media", StringComparison.Ordinal), "Comparison page should avoid flex-stretching wipe labels at wide viewport sizes.");
            Assert(comparison.Contains("SVG 25%", StringComparison.Ordinal) && comparison.Contains("SVG 75%", StringComparison.Ordinal), "Comparison page should offer fixed wipe positions without requiring JavaScript.");
            Assert(comparison.Contains("clip-path:inset(0 calc(100% - var(--wipe)) 0 0)", StringComparison.Ordinal), "Comparison page should keep SVG and PNG framed together while moving the wipe split.");
            Assert(comparison.Contains("C# example code", StringComparison.Ordinal) && comparison.Contains("var chart = Chart.Create().WithTheme(ChartForgeX.Themes.ChartTheme.Light()).WithTitle(&quot;Alpha &amp; Beta&quot;);", StringComparison.Ordinal), "Comparison page should carry readable C# source snippets when examples provide them.");
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
            Assert(manifest.Contains("\"healthyHtmls\": 3", StringComparison.Ordinal), "Comparison manifest should summarize healthy HTML artifacts.");
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
            Assert(manifest.Contains("\"htmlRequiresSurfaceGradient\": false", StringComparison.Ordinal)
                && manifest.Contains("\"htmlRequiresSurfaceTreatment\": true", StringComparison.Ordinal)
                && manifest.Contains("\"htmlAllowsFlatSurface\": true", StringComparison.Ordinal)
                && manifest.Contains("\"htmlRequiresPrintCss\": true", StringComparison.Ordinal),
                "Comparison manifest should require a styled HTML surface and print CSS while accepting native flat surfaces.");
            Assert(manifest.Contains("\"htmlMayBeExplicitlyOmitted\": true", StringComparison.Ordinal) && manifest.Contains("\"required\": false", StringComparison.Ordinal), "Comparison manifests should allow explicitly marked standalone SVG/PNG artifacts without weakening HTML checks for normal chart outputs.");
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
            Assert(manifest.Contains("\"html\":", StringComparison.Ordinal) && manifest.Contains("\"hasSurfaceGradient\": true", StringComparison.Ordinal)
                && manifest.Contains("\"hasPrintCss\": true", StringComparison.Ordinal), "Comparison manifest should report the Light theme's gradient HTML surface and print statistics.");
            Assert(manifest.Contains("\"healthy\": true", StringComparison.Ordinal), "Comparison manifest should flag healthy PNG artifacts.");
        } finally {
            Directory.Delete(output, true);
        }
    }
}
