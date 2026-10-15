using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
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
    [InlineData(VisualThemeMode.Light)]
    [InlineData(VisualThemeMode.Dark)]
    public void NarrowZoomedThresholdKeepsItsCompleteShortCaptionClearOfBothLines(VisualThemeMode mode) {
        var start = new DateTime(2026, 9, 13, 0, 0, 0, DateTimeKind.Utc);
        var chart = Chart.Create().WithSize(360, 260).WithTransparentBackground().WithHeader(false)
            .WithLegend(true).WithPadding(64, 14, 20, 74).WithThemeMode(mode)
            .WithXAxisTimeScale(TimeZoneInfo.Utc, true, "UTC").WithYAxis("Score").WithYAxisBounds(60, 90)
            .AddLine("Security", Enumerable.Range(0, 30).Select(day => new ChartPoint(start.AddDays(day),
                72 + 8 * Math.Sin(day / 5.0 + 1) - (day > 12 && day < 24 ? 10 * Math.Sin((day - 12) / 12.0 * Math.PI) : 0))))
            .AddLine("Health", Enumerable.Range(0, 30).Select(day => new ChartPoint(start.AddDays(day), 80 + 5 * Math.Cos(day / 5.0))))
            .AddHorizontalLine(80, "Target 80");
        var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        ScatterBubbleColorRenderingTests.Capture(prepared, "threshold-narrow-zoom-" + mode);
        var text = Assert.Single(prepared.Scene.Nodes.OfType<VisualSceneText>(), node => node.Role == "annotation-label");
        Assert.Equal("Target 80", Assert.Single(text.Text.Lines).Text);
        var plate = Assert.Single(prepared.Scene.Nodes.OfType<VisualSceneRectangle>(), node => node.Role == "annotation-label-backplate");
        Assert.InRange(plate.Bounds.Left, 0, 360); Assert.InRange(plate.Bounds.Right, 0, 360);
        Assert.InRange(plate.Bounds.Top, 0, 260); Assert.InRange(plate.Bounds.Bottom, 0, 260);
        foreach (var line in prepared.Scene.Nodes.OfType<VisualScenePath>().Where(node => node.Role == "line"))
            Assert.False(new LabelMarkShape(VisualSceneGeometry.Flatten(line, 1), false, line.StrokeWidth).Intersects(plate.Bounds));
        Assert.DoesNotContain(prepared.Diagnostics, diagnostic => diagnostic.Code == "cartesian.annotation-label-overflow");
        Assert.Contains(">Target 80</text>", prepared.ToSvg(), StringComparison.Ordinal);
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
        chart.ClearSeries(); chart.Annotations.Clear();
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

    private static ChartPoint[] Points(params double[] values) => values.Select((value, index) => new ChartPoint(index + 1, value)).ToArray();
    private static ChartRect TextBounds(VisualSceneText label) => new(label.X, label.Baseline - label.Text.Ascent, label.Text.Metrics.Width, label.Text.Metrics.Height);
    private static bool Overlaps(ChartRect first, ChartRect second) => first.Width > 0 && first.Height > 0 && second.Width > 0 && second.Height > 0
        && first.Left < second.Right && first.Right > second.Left && first.Top < second.Bottom && first.Bottom > second.Top;
}
