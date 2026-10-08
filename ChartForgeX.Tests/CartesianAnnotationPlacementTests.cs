using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class CartesianAnnotationPlacementTests {
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void PeakWindowCaptionAvoidsBarsAndTheirValueLabelsWithoutLosingSourceFacts(bool dataLabels) {
        const string caption = "Review concentration";
        var values = new[] { 3d, 1, 3, 1, 0, 0, 0, 9, 10, 9, 7, 0, 0 };
        var chart = Chart.Create().WithSize(656, 360).WithHeader(false).WithLegend(false).WithAxes(false).WithGrid(false)
            .WithTheme(ChartTheme.DashboardLight()).WithDashboardBarPanelStyle()
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
        chart.Series.Clear(); chart.Annotations.Clear();
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
        chart.Series[0].WithMarkerRadius(0);
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
