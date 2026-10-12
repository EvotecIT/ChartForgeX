using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class RadarCurveClosingEdgeTests {
    [Theory]
    [InlineData(null)]
    [InlineData(0d)]
    [InlineData(.25)]
    public void ColumnCaptionsAvoidEveryEdgeOfTheClosedRadarOutline(double? tension) {
        var chart = Chart.Create().WithSize(240, 180).WithHeader(false).WithLegend(false).WithAxes(false).WithGrid(false)
            .WithYAxisBounds(0, 100).WithDataLabels().WithDataLabelPlacement(ChartDataLabelPlacement.Above)
            .AddRadialColumn("Observed", Enumerable.Range(1, 4).Select(index => new ChartPoint(index, 40)))
            .AddRadarLine("Target", Enumerable.Range(1, 4).Select(index => new ChartPoint(index, 80)));
        chart.Series[1].ShowDataLabels = false; chart.Series[1].Markers.Enabled = false;
        chart.Series[1].Radar.CurveTension = tension;
        var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        var path = prepared.Scene.Nodes.OfType<VisualScenePath>().Single(node => node.Role == "radar-outline");
        var points = new ChartPath(path.Commands).Flatten(); points.Add(points[0]);
        var outline = new LabelMarkShape(new[] { points }, false, path.StrokeWidth);
        var captions = prepared.Scene.Nodes.OfType<VisualSceneText>().Where(node => node.Role == "radial-data-label").ToArray();
        var captures = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (!string.IsNullOrWhiteSpace(captures)) {
            Directory.CreateDirectory(captures);
            File.WriteAllBytes(Path.Combine(captures, "radar-closing-edge-" + (tension?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? "default") + ".png"), prepared.ToPng());
        }
        Assert.Equal(4, prepared.Regions.Count(region => region.Role == "radial-data-label"));
        if (captions.Length < 4) Assert.Contains(prepared.Scene.Diagnostics, diagnostic => diagnostic.Code == "numeric-radial.label-overflow");
        Assert.All(captions, text => Assert.False(outline.Intersects(CartesianReversalLabelTests.Bounds(text)), text.Id + " overlaps the radar outline."));
    }
}
