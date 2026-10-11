using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class RadarCurveGuideIntegrationTests {
    [Fact]
    public void FractionalGuideHeadroomAvoidsUnnecessaryCurveScaling() {
        var chart = Native(.5, true);
        var straight = Prepare(chart);
        chart.Series[0].Radar.CurveTension = 1;
        var curved = Prepare(chart);
        Assert.Equal(straight.Regions.Where(region => region.Role == "radar-point").Select(region => region.Bounds),
            curved.Regions.Where(region => region.Role == "radar-point").Select(region => region.Bounds));
        Assert.Contains(" C", curved.ToSvg());
        Assert.NotEqual(straight.ToPng(), curved.ToPng());
    }

    [Theory]
    [InlineData(.5, true)]
    [InlineData(2.5, true)]
    [InlineData(.5, false)]
    [InlineData(2.5, false)]
    public void MaximumTensionPreservesCommonDataScaleAndFitsWithVisibleOrHiddenFractionalGuides(double count, bool grid) {
        var chart = Native(count, grid);
        chart.Series[0].Radar.CurveTension = 1;
        var prepared = Prepare(chart);
        var points = prepared.Regions.Where(region => region.Role == "radar-point").ToArray();
        Assert.Equal(8, points.Length);
        var plot = prepared.Scene.Nodes.OfType<VisualSceneGroup>().Last(group => group.Clip.HasValue).Clip!.Value;
        var cx = plot.Left + plot.Width / 2; var cy = plot.Top + plot.Height / 2;
        for (var i = 0; i < 4; i++) {
            Assert.Equal((points[i].Bounds.Left + points[i].Bounds.Width / 2 - cx) / 2,
                points[i + 4].Bounds.Left + points[i + 4].Bounds.Width / 2 - cx, 7);
            Assert.Equal((points[i].Bounds.Top + points[i].Bounds.Height / 2 - cy) / 2,
                points[i + 4].Bounds.Top + points[i + 4].Bounds.Height / 2 - cy, 7);
        }
        foreach (var outline in prepared.Scene.Nodes.OfType<VisualScenePath>().Where(node => node.Role == "radar-outline")) {
            var bounds = new ChartPath(outline.Commands).Bounds();
            Assert.InRange(bounds.Left, plot.Left + outline.StrokeWidth / 2, plot.Right);
            Assert.InRange(bounds.Right, plot.Left, plot.Right - outline.StrokeWidth / 2);
            Assert.InRange(bounds.Top, plot.Top + outline.StrokeWidth / 2, plot.Bottom);
            Assert.InRange(bounds.Bottom, plot.Top, plot.Bottom - outline.StrokeWidth / 2);
        }
        if (grid) {
            var rings = prepared.Scene.Nodes.OfType<VisualSceneEllipse>().Where(node => node.Role == "radar-ring").ToArray();
            Assert.Equal((int)Math.Ceiling(count), rings.Length);
            var spoke = prepared.Scene.Nodes.OfType<VisualSceneLine>().First(node => node.Role == "radar-spoke");
            var radius = Math.Sqrt(Math.Pow(spoke.End.X - cx, 2) + Math.Pow(spoke.End.Y - cy, 2));
            Assert.Equal(Math.Ceiling(count) / count, rings.Last().Rx / radius, 7);
        } else {
            Assert.DoesNotContain(prepared.Scene.Nodes, node => node.Role == "radar-ring");
            chart.Options.PolarGridRingCount = null;
            Assert.Equal(prepared.ToPng(), Prepare(chart).ToPng());
        }
    }

    private static Chart Native(double count, bool grid) {
        var chart = Chart.Create().WithSize(320, 240).WithHeader(false).WithLegend(false).WithAxes(false).WithGrid(grid).WithDataLabels(false)
            .WithYAxisBounds(0, 100).WithPolarGridShape(ChartPolarGridShape.Circle)
            .AddRadarLine("Maximum", Enumerable.Range(1, 4).Select(index => new ChartPoint(index, 100)))
            .AddRadarLine("Half", Enumerable.Range(1, 4).Select(index => new ChartPoint(index, 50)));
        chart.Options.PolarGridRingCount = count;
        return chart;
    }

    private static PreparedVisual Prepare(Chart chart) => chart.Prepare(VisualExportRequest.ForChart(chart).Context);
}
