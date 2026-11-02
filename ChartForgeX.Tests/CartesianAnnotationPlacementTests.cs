using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using ChartForgeX.Typography;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class CartesianAnnotationPlacementTests {
    [Fact]
    public void BlockedThresholdUsesTheMeasuredRightMarginWithoutClippingItsCompleteCaption() {
        var chart = Chart.Create().WithSize(640, 300).WithHeader(false).WithLegend(false)
            .WithYAxisBounds(0, 10).WithDataLabels(false)
            .AddBar("Observed", Enumerable.Range(0, 20).Select(index => new ChartPoint(index, 10)))
            .AddHorizontalLine(5, "Target 5");
        chart.Options.XAxis.LabelFormatter = _ => "Inspection reference period";
        var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        ScatterBubbleColorRenderingTests.Capture(prepared, "threshold-right-margin");
        var line = Assert.Single(prepared.Scene.Nodes.OfType<VisualScenePath>(), node => node.Role == "annotation-line");
        var plate = Assert.Single(prepared.Scene.Nodes.OfType<VisualSceneRectangle>(), node => node.Role == "annotation-label-backplate");
        var caption = Assert.Single(prepared.Scene.Nodes.OfType<VisualSceneText>(), node => node.Role == "annotation-label");
        Assert.Equal("Target 5", Assert.Single(caption.Text.Lines).Text);
        Assert.True(plate.Bounds.Left > line.Commands[1].X);
        Assert.Contains(prepared.Scene.Nodes, node => node.Role == "annotation-label-leader");
        var exported = XDocument.Parse(prepared.ToSvg()).Descendants().Single(node => (string?)node.Attribute("data-cfx-role") == "annotation-label");
        Assert.DoesNotContain(exported.Ancestors(), node => node.Attribute("clip-path") != null);
        Assert.NotEmpty(prepared.ToPng(new VisualRenderOptions(supersampling: 1)));
    }

    [Theory]
    [InlineData(VisualThemeMode.Light)]
    [InlineData(VisualThemeMode.Dark)]
    public void DefaultReferenceLinesFollowThePreparedThemeAndRemainDistinctFromTheGrid(VisualThemeMode mode) {
        var points = new[] { new ChartPoint(0, 2), new ChartPoint(1, 8) };
        var theme = VisualTheme.Graphite();
        var chart = Chart.Create().WithHeader(false).WithLegend(false).WithTheme(theme)
            .WithXAxisBounds(0, 10).WithYAxisBounds(0, 10).AddLine("Observed", points)
            .AddHorizontalLine(5, "Target 5").AddVerticalLine(4, "Review")
            .AddMeanLine("Mean", points).AddMedianLine("Median", points)
            .AddHorizontalLine(7, "Authored", ChartColor.FromHex("#FF00FF"));
        var prepared = chart.Prepare(new VisualRenderContext(new VisualLayoutOptions(new VisualSize(640, 360)), theme, mode));
        ScatterBubbleColorRenderingTests.Capture(prepared, "threshold-contrast-" + mode);
        var lines = prepared.Scene.Nodes.OfType<VisualScenePath>().Where(node => node.Role == "annotation-line").ToArray();
        Assert.Equal(5, lines.Length);
        var colors = theme.Resolve(mode);
        Assert.All(lines.Take(4), line => Assert.Equal(colors.MutedForeground, line.Stroke));
        Assert.NotEqual(colors.Grid, lines[0].Stroke);
        Assert.Equal(ChartColor.FromHex("#FF00FF"), lines[4].Stroke);
        Assert.NotEmpty(prepared.ToPng(new VisualRenderOptions(supersampling: 1)));
        var exported = XDocument.Parse(prepared.ToSvg()).Descendants().Where(node => (string?)node.Attribute("data-cfx-role") == "annotation-line").ToArray();
        Assert.All(exported.Take(4), line => Assert.Equal(colors.MutedForeground.ToCss(), (string?)line.Attribute("stroke")));
        Assert.Equal("#FF00FF", (string?)exported[4].Attribute("stroke"));
    }

    [Theory]
    [InlineData(360, 12, VisualThemeMode.Light)]
    [InlineData(360, 12, VisualThemeMode.Dark)]
    [InlineData(320, 12, VisualThemeMode.Light)]
    [InlineData(320, 12, VisualThemeMode.Dark)]
    [InlineData(360, 16, VisualThemeMode.Light)]
    [InlineData(360, 16, VisualThemeMode.Dark)]
    public void NarrowZoomedThresholdKeepsItsCompleteShortCaptionClearOfBothLines(int width, int axisSize, VisualThemeMode mode) {
        var chart = NarrowThreshold(width, mode);
        if (axisSize != 12) chart.WithTheme(VisualTheme.Graphite().WithTypography(new VisualTypography(axisSize: axisSize)));
        var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        ScatterBubbleColorRenderingTests.Capture(prepared, $"threshold-narrow-zoom-{width}-{axisSize}-{mode}");
        var text = Assert.Single(prepared.Scene.Nodes.OfType<VisualSceneText>(), node => node.Role == "annotation-label");
        Assert.Equal("Target 80", Assert.Single(text.Text.Lines).Text);
        var plate = Assert.Single(prepared.Scene.Nodes.OfType<VisualSceneRectangle>(), node => node.Role == "annotation-label-backplate");
        Assert.Equal(axisSize, text.Text.Size);
        Assert.InRange(plate.Bounds.Left, 0, width); Assert.InRange(plate.Bounds.Right, 0, width);
        Assert.InRange(plate.Bounds.Top, 0, 260); Assert.InRange(plate.Bounds.Bottom, 0, 260);
        foreach (var line in prepared.Scene.Nodes.OfType<VisualScenePath>().Where(node => node.Role == "line"))
            Assert.False(new LabelMarkShape(VisualSceneGeometry.Flatten(line, 1), false, line.StrokeWidth).Intersects(plate.Bounds));
        foreach (var axisText in prepared.Scene.Nodes.OfType<VisualSceneText>().Where(node => node.Role?.StartsWith("axis-", StringComparison.Ordinal) == true || node.Role == "legend-label"))
            Assert.False(Overlaps(plate.Bounds, TextBounds(axisText)));
        var guide = Assert.Single(prepared.Scene.Nodes.OfType<VisualScenePath>(), node => node.Role == "annotation-line").Commands[0].Y;
        Assert.True(plate.Bounds.Bottom <= guide - 2 || plate.Bounds.Top >= guide + 2);
        Assert.DoesNotContain(prepared.Diagnostics, diagnostic => diagnostic.Code == "cartesian.annotation-label-overflow");
        Assert.Contains(">Target 80</text>", prepared.ToSvg(), StringComparison.Ordinal);
        Assert.NotEmpty(prepared.ToPng(new VisualRenderOptions(supersampling: 1)));
    }

    [Theory]
    [InlineData(false, VisualThemeMode.Light, false)]
    [InlineData(false, VisualThemeMode.Dark, false)]
    [InlineData(true, VisualThemeMode.Light, false)]
    [InlineData(true, VisualThemeMode.Dark, false)]
    [InlineData(false, VisualThemeMode.Light, true)]
    [InlineData(false, VisualThemeMode.Dark, true)]
    public void WideMeasuredThresholdUsesOnlyUnoccupiedTitleSpaceAndKeepsOmittedSourceFacts(bool occupiedTitle, VisualThemeMode mode, bool header) {
        const string family = "CFX Threshold Wide Face";
        var glyphs = Enumerable.Range(32, 95).ToDictionary(value => value, _ => OpenTypeTestFonts.H);
        FontRegistry.Register(family, OpenTypeTestFonts.NameKeyed(extraGlyphs: glyphs));
        var chart = NarrowThreshold(360, mode).WithFontFamily(family);
        if (occupiedTitle) chart.WithYAxis("Coverage and availability scores");
        if (header) chart.WithSize(360, 320).WithHeader(true).WithTitle("Coverage").WithSubtitle("Service checks");
        var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        ScatterBubbleColorRenderingTests.Capture(prepared, $"threshold-wide-face-{occupiedTitle}-{header}-{mode}");
        var plot = Assert.Single(prepared.Regions, region => region.Role == "cartesian-plot").Bounds;
        var captions = prepared.Scene.Nodes.OfType<VisualSceneText>().Where(node => node.Role == "annotation-label").ToArray();
        if (occupiedTitle) {
            Assert.Empty(captions);
            Assert.Contains(prepared.Diagnostics, diagnostic => diagnostic.Code == "cartesian.annotation-label-overflow");
        } else {
            var caption = Assert.Single(captions);
            Assert.Equal("Target 80", Assert.Single(caption.Text.Lines).Text);
            Assert.Equal(12, caption.Text.Size);
            var plate = Assert.Single(prepared.Scene.Nodes.OfType<VisualSceneRectangle>(), node => node.Role == "annotation-label-backplate");
            Assert.True(plate.Bounds.Bottom <= plot.Top);
            Assert.Contains(prepared.Scene.Nodes, node => node.Role == "annotation-label-leader");
            foreach (var text in prepared.Scene.Nodes.OfType<VisualSceneText>().Where(node => node.Role is "axis-y-title" or "legend-label" or "frame-heading" or "frame-heading-continuation"))
                Assert.False(Overlaps(plate.Bounds, TextBounds(text)));
            Assert.DoesNotContain(prepared.Diagnostics, diagnostic => diagnostic.Code == "cartesian.annotation-label-overflow");
        }
        var source = Assert.Single(XDocument.Parse(prepared.ToSvg()).Descendants(), node => (string?)node.Attribute("data-cfx-role") == "annotation");
        Assert.Equal("Target 80", (string?)source.Attribute("data-cfx-label"));
        Assert.Contains("Target 80", Assert.Single(prepared.Regions, region => region.Role == "annotation").Label);
        Assert.NotEmpty(prepared.ToPng(new VisualRenderOptions(supersampling: 1)));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void MultilineReferenceCaptionCanShortenIntoItsOriginalGuideCandidates(bool band) {
        const string source = "Long label\nWith words\nAcross lines";
        var chart = Chart.Create().WithSize(320, 80).WithPadding(0, 0, 0, 0)
            .WithHeader(false).WithLegend(false).WithAxes(false).WithGrid(false).WithYAxisBounds(0, 10);
        if (band) chart.AddHorizontalBand(5, 5.5, source);
        else chart.AddHorizontalLine(5, source);
        var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        ScatterBubbleColorRenderingTests.Capture(prepared, "threshold-multiline-shortened-" + band);
        var caption = Assert.Single(prepared.Scene.Nodes.OfType<VisualSceneText>(), node => node.Role == "annotation-label");
        var displayed = Assert.Single(caption.Text.Lines).Text;
        Assert.StartsWith("Long label", displayed, StringComparison.Ordinal);
        Assert.EndsWith("…", displayed, StringComparison.Ordinal);
        Assert.Equal(12, caption.Text.Size);
        var plate = Assert.Single(prepared.Scene.Nodes.OfType<VisualSceneRectangle>(), node => node.Role == "annotation-label-backplate");
        Assert.InRange(plate.Bounds.Left, 0, 320); Assert.InRange(plate.Bounds.Right, 0, 320);
        Assert.InRange(plate.Bounds.Top, 0, 80); Assert.InRange(plate.Bounds.Bottom, 0, 80);
        Assert.Contains(source, Assert.Single(prepared.Regions, region => region.Role == "annotation").Label);
        Assert.Contains(prepared.Diagnostics, diagnostic => diagnostic.Code == "cartesian.annotation-label-overflow");
        Assert.Contains(displayed, prepared.ToSvg(), StringComparison.Ordinal);
        Assert.NotEmpty(prepared.ToPng(new VisualRenderOptions(supersampling: 1)));
    }

    [Theory]
    [InlineData(320, VisualThemeMode.Light)]
    [InlineData(320, VisualThemeMode.Dark)]
    [InlineData(800, VisualThemeMode.Light)]
    [InlineData(800, VisualThemeMode.Dark)]
    public void LongReferenceCaptionCanShortenInTheMeasuredTitleStripWhenMarksBlockThePlot(int width, VisualThemeMode mode) {
        const string source = "A detailed service level objective for every selected region and its monitored production services, including all regional availability targets, response-time budgets and error-rate requirements";
        var chart = Chart.Create().WithSize(width, 300).WithPadding(64, 14, 20, 50)
            .WithHeader(false).WithLegend(true).WithDataLabels(false).WithThemeMode(mode)
            .WithYAxis("Y").WithYAxisBounds(0, 10)
            .AddBar("Observed", Enumerable.Range(0, 20).Select(index => new ChartPoint(index, 10)))
            .AddHorizontalLine(5, source);
        var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        ScatterBubbleColorRenderingTests.Capture(prepared, $"threshold-title-strip-ellipsis-{width}-{mode}");
        var caption = Assert.Single(prepared.Scene.Nodes.OfType<VisualSceneText>(), node => node.Role == "annotation-label");
        var displayed = Assert.Single(caption.Text.Lines).Text;
        Assert.StartsWith("A detailed service level", displayed, StringComparison.Ordinal);
        Assert.EndsWith("…", displayed, StringComparison.Ordinal);
        Assert.Equal(12, caption.Text.Size);
        var plate = Assert.Single(prepared.Scene.Nodes.OfType<VisualSceneRectangle>(), node => node.Role == "annotation-label-backplate");
        var plot = Assert.Single(prepared.Regions, region => region.Role == "cartesian-plot").Bounds;
        Assert.True(plate.Bounds.Bottom <= plot.Top);
        Assert.All(prepared.Scene.Nodes.OfType<VisualSceneRectangle>().Where(node => node.Role == "bar"), bar => Assert.False(Overlaps(plate.Bounds, bar.Bounds)));
        Assert.All(prepared.Scene.Nodes.OfType<VisualSceneText>().Where(node => node.Role is "axis-y-title" or "legend-label"), text => Assert.False(Overlaps(plate.Bounds, TextBounds(text))));
        Assert.Contains(prepared.Scene.Nodes, node => node.Role == "annotation-label-leader");
        Assert.Contains(prepared.Diagnostics, diagnostic => diagnostic.Code == "cartesian.annotation-label-overflow");
        Assert.Contains(source, Assert.Single(prepared.Regions, region => region.Role == "annotation").Label);
        Assert.Contains(displayed, prepared.ToSvg(), StringComparison.Ordinal);
        Assert.NotEmpty(prepared.ToPng(new VisualRenderOptions(supersampling: 1)));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void PeakWindowCaptionAvoidsBarsAndTheirValueLabelsWithoutLosingSourceFacts(bool dataLabels) {
        const string caption = "Review concentration";
        var values = new[] { 3d, 1, 3, 1, 0, 0, 0, 9, 10, 9, 7, 0, 0 };
        var chart = Chart.Create().WithSize(656, 360).WithHeader(false).WithLegend(false).WithAxes(false).WithGrid(false)
            .WithTheme(VisualTheme.Dashboard()).WithDashboardBarPanelStyle()
            .WithYAxisBounds(0, 10).WithDataLabels(dataLabels)
            .AddVerticalBand(7.5, 11.5, caption, opacity: .08)
            .AddBar("Reviews", Points(values));
        var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        var plate = Assert.Single(prepared.Scene.Nodes.OfType<VisualSceneRectangle>(), node => node.Role == "annotation-label-backplate");
        var text = Assert.Single(prepared.Scene.Nodes.OfType<VisualSceneText>(), node => node.Role == "annotation-label");
        Assert.Equal(caption, Assert.Single(text.Text.Lines).Text);
        var bars = prepared.Scene.Nodes.OfType<VisualSceneRectangle>().Where(node => node.Role == "bar").ToArray();
        Assert.Equal(values.Length, bars.Length);
        Assert.All(bars, bar => Assert.False(Overlaps(plate.Bounds, bar.Bounds)));
        foreach (var cap in prepared.Scene.Nodes.OfType<VisualSceneLine>().Where(node => node.Role == "bar-cap")) {
            var stroke = new LabelMarkShape(new[] { new List<ChartPoint> { cap.Start, cap.End } }, false, cap.StrokeWidth);
            Assert.False(stroke.Intersects(plate.Bounds));
        }
        Assert.All(prepared.Scene.Nodes.OfType<VisualSceneText>().Where(node => node.Role == "data-label"),
            label => Assert.False(Overlaps(plate.Bounds, TextBounds(label))));
        Assert.DoesNotContain(prepared.Diagnostics, diagnostic => diagnostic.Code == "cartesian.annotation-label-overflow");
        var nodes = prepared.Scene.Nodes.ToArray();
        Assert.True(Array.FindIndex(nodes, node => node.Role == "annotation-band") < Array.FindIndex(nodes, node => node.Role == "bar"));
        Assert.Equal(values.Length, prepared.Regions.Count(region => region.Role == "point"));
        var svg = prepared.ToSvg(); var png = prepared.ToPng();
        var group = XDocument.Parse(svg).Descendants().Single(node => (string?)node.Attribute("data-cfx-role") == "annotation");
        Assert.Equal("7.5", (string?)group.Attribute("data-cfx-value"));
        Assert.Equal("11.5", (string?)group.Attribute("data-cfx-end-value"));
        Assert.Equal(caption, (string?)group.Attribute("data-cfx-label"));
        Assert.Contains(caption, Assert.Single(prepared.Regions, region => region.Role == "annotation").Label);
        chart.ClearSeries(); chart.ClearAnnotations();
        Assert.Equal(svg, prepared.ToSvg()); Assert.Equal(png, prepared.ToPng());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void AnnotationCaptionAvoidsAnUnmarkedLineEvenWhenDataLabelsAreDisabled(bool band) {
        const string caption = "Milestone";
        var chart = Chart.Create().WithSize(520, 300).WithHeader(false).WithLegend(false).WithAxes(false).WithGrid(false)
            .WithXAxisBounds(0, 10).WithYAxisBounds(0, 10).WithDataLabels(false)
            .AddLine("Observed", new[] { new ChartPoint(0, 9), new ChartPoint(10, 9) });
        chart.Series[0].ConfigureMarkers(markers => markers.Radius = 0);
        if (band) chart.AddVerticalBand(5, 7, caption, opacity: .08);
        else chart.AddVerticalLine(5, caption);
        var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        var plate = Assert.Single(prepared.Scene.Nodes.OfType<VisualSceneRectangle>(), node => node.Role == "annotation-label-backplate");
        var text = Assert.Single(prepared.Scene.Nodes.OfType<VisualSceneText>(), node => node.Role == "annotation-label");
        Assert.Equal(caption, Assert.Single(text.Text.Lines).Text);
        var line = Assert.Single(prepared.Scene.Nodes.OfType<VisualScenePath>(), node => node.Role == "line");
        var stroke = new LabelMarkShape(VisualSceneGeometry.Flatten(line, 1), false, line.StrokeWidth);
        Assert.False(stroke.Intersects(plate.Bounds));
        Assert.DoesNotContain(prepared.Scene.Nodes, node => node.Role == "data-label");
        Assert.DoesNotContain(prepared.Diagnostics, diagnostic => diagnostic.Code == "cartesian.annotation-label-overflow");
        Assert.Equal(2, prepared.Regions.Count(region => region.Role == "point"));
        Assert.Single(prepared.Regions, region => region.Id == "annotation-0");
    }

    [Fact]
    public void AdjacentTargetCaptionsKeepFullTextAndDoNotOverlapEachOther() {
        var chart = Chart.Create().WithSize(640, 360).WithHeader(false).WithLegend(false).WithAxes(false).WithGrid(false)
            .WithYAxisBounds(0, 10).WithDataLabels(false)
            .AddBar("Observed", Points(2, 2, 2, 9, 2, 2, 2))
            .AddHorizontalLine(7.5, "Service target")
            .AddHorizontalLine(7.5, "Warning threshold");
        var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        var plates = prepared.Scene.Nodes.OfType<VisualSceneRectangle>().Where(node => node.Role == "annotation-label-backplate").ToArray();
        Assert.Equal(2, plates.Length);
        Assert.False(Overlaps(plates[0].Bounds, plates[1].Bounds));
        Assert.Equal(new[] { "Service target", "Warning threshold" }, prepared.Scene.Nodes.OfType<VisualSceneText>()
            .Where(node => node.Role == "annotation-label").Select(node => Assert.Single(node.Text.Lines).Text));
        var bars = prepared.Scene.Nodes.OfType<VisualSceneRectangle>().Where(node => node.Role == "bar").ToArray();
        Assert.All(plates, plate => Assert.All(bars, bar => Assert.False(Overlaps(plate.Bounds, bar.Bounds))));
        Assert.DoesNotContain(prepared.Diagnostics, diagnostic => diagnostic.Code == "cartesian.annotation-label-overflow");
    }

    private static Chart NarrowThreshold(int width, VisualThemeMode mode) {
        var start = new DateTime(2026, 9, 13, 0, 0, 0, DateTimeKind.Utc);
        return Chart.Create().WithSize(width, 260).WithTransparentBackground().WithHeader(false)
            .WithLegend(true).WithPadding(64, 14, 20, 74).WithThemeMode(mode)
            .WithXAxisTimeScale(TimeZoneInfo.Utc, true, "UTC").WithYAxis("Score").WithYAxisBounds(60, 90)
            .AddLine("Security", Enumerable.Range(0, 30).Select(day => new ChartPoint(start.AddDays(day),
                72 + 8 * Math.Sin(day / 5.0 + 1) - (day > 12 && day < 24 ? 10 * Math.Sin((day - 12) / 12.0 * Math.PI) : 0))))
            .AddLine("Health", Enumerable.Range(0, 30).Select(day => new ChartPoint(start.AddDays(day), 80 + 5 * Math.Cos(day / 5.0))))
            .AddHorizontalLine(80, "Target 80");
    }

    private static ChartPoint[] Points(params double[] values) => values.Select((value, index) => new ChartPoint(index + 1, value)).ToArray();
    private static ChartRect TextBounds(VisualSceneText label) => new(label.X, label.Baseline - label.Text.Ascent, label.Text.Metrics.Width, label.Text.Metrics.Height);
    private static bool Overlaps(ChartRect first, ChartRect second) => first.Width > 0 && first.Height > 0 && second.Width > 0 && second.Height > 0
        && first.Left < second.Right && first.Right > second.Left && first.Top < second.Bottom && first.Bottom > second.Top;
}
