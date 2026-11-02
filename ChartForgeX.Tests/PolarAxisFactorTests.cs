using ChartForgeX.Core;
using ChartForgeX.Mermaid;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Protects native axis geometry independently of source syntax and output backends.</summary>
public sealed class PolarAxisFactorTests {
    [Theory]
    [InlineData(-1)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void FactorsRejectNegativeOrNonFiniteInputs(double value) {
        Assert.Throws<ArgumentOutOfRangeException>(() => new ChartOptions { PolarAxisScaleFactor = value });
        Assert.Throws<ArgumentOutOfRangeException>(() => new ChartOptions { PolarAxisLabelFactor = value });
        Assert.Throws<ArgumentOutOfRangeException>(() => new MermaidRadarRenderOptions { AxisScaleFactor = value });
        Assert.Throws<ArgumentOutOfRangeException>(() => new MermaidRadarRenderOptions { AxisLabelFactor = value });
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ShorterSpokesAndLabelAnchorsPreservePointsAndValueBounds(bool polar) {
        var chart = Native(polar);
        var original = Prepare(chart);
        var originalSpoke = Spokes(original).First();
        chart.Options.PolarAxisScaleFactor = .5;
        chart.Options.PolarAxisLabelFactor = .75;
        var changed = Prepare(chart);
        var spoke = Spokes(changed).First();
        Assert.Equal((originalSpoke.End.X - originalSpoke.Start.X) / 2, spoke.End.X - spoke.Start.X, 8);
        Assert.Equal((originalSpoke.End.Y - originalSpoke.Start.Y) / 2, spoke.End.Y - spoke.Start.Y, 8);
        Assert.Equal(original.Regions.Where(r => r.Role.EndsWith("-point")).Select(r => r.Bounds),
            changed.Regions.Where(r => r.Role.EndsWith("-point")).Select(r => r.Bounds));
        var right = original.Scene.Nodes.OfType<VisualSceneText>().Single(n => n.Role == (polar ? "polar-angle-label" : "radar-axis-label") && n.Text.Lines[0].Text == "East");
        var moved = changed.Scene.Nodes.OfType<VisualSceneText>().Single(n => n.Role == right.Role && n.Text.Lines[0].Text == "East");
        Assert.True(moved.X < right.X);
        Assert.Equal(0, chart.Options.YAxis.Minimum); Assert.Equal(100, chart.Options.YAxis.Maximum);
        Assert.Equal(new[] { 90d, 45, 80, 60 }, chart.Series[0].Points.Select(p => p.Y));
    }

    [Theory]
    [InlineData(false, 2)]
    [InlineData(true, 2)]
    [InlineData(false, double.MaxValue)]
    public void LongerSpokesFitTheSharedPlotWithoutLosingObservations(bool polar, double factor) {
        var chart = Native(polar);
        chart.Options.PolarAxisScaleFactor = factor;
        var prepared = Prepare(chart);
        var plot = prepared.Scene.Nodes.OfType<VisualSceneGroup>().Last(g => g.Clip.HasValue).Clip!.Value;
        Assert.Equal(4, Spokes(prepared).Count());
        foreach (var spoke in Spokes(prepared)) {
            Assert.InRange(spoke.End.X, plot.Left, plot.Right);
            Assert.InRange(spoke.End.Y, plot.Top, plot.Bottom);
        }
        Assert.Equal(4, prepared.Regions.Count(r => r.Role.EndsWith("-point")));
        Assert.DoesNotContain("NaN", prepared.ToSvg()); Assert.DoesNotContain("Infinity", prepared.ToSvg());
    }

    [Fact]
    public void HiddenFactorsDoNotChangeLayoutAndSubnormalGuideCountsRemainFinite() {
        var chart = Native(false).WithAxes(false).WithGrid(false);
        var expected = Prepare(chart).ToPng();
        chart.Options.PolarAxisScaleFactor = double.MaxValue;
        chart.Options.PolarAxisLabelFactor = double.MaxValue;
        Assert.Equal(expected, Prepare(chart).ToPng());
        chart.WithGrid(true); chart.Options.PolarGridRingCount = double.Epsilon;
        var visible = Prepare(chart);
        Assert.Single(visible.Scene.Nodes.OfType<VisualSceneEllipse>(), n => n.Role == "radar-ring");
        Assert.DoesNotContain("NaN", visible.ToSvg()); Assert.DoesNotContain("Infinity", visible.ToSvg());
        Assert.NotEmpty(visible.ToPng());
    }

    private static Chart Native(bool polar) {
        var chart = Chart.Create().WithSize(420, 360).WithHeader(false).WithLegend(false).WithDataLabels(false)
            .WithYAxisBounds(0, 100).WithXLabels("North", "East", "South", "West").WithPolarGridShape(ChartPolarGridShape.Circle);
        chart.Options.YAxis.Visible = false;
        var values = new[] { 90d, 45, 80, 60 };
        if (polar) {
            chart.Options.XAxis.TickCount = 4;
            chart.WithXLabels(new[] { "East", "North", "West", "South" }.Select((label, i) => new ChartAxisLabel(i * Math.PI / 2, label)));
            chart.AddPolar("Current", values.Select((v, i) => new ChartPoint(i * Math.PI / 2, v)));
        }
        else chart.AddRadarLine("Current", values.Select((v, i) => new ChartPoint(i + 1, v)));
        return chart;
    }
    private static IEnumerable<VisualSceneLine> Spokes(PreparedVisual prepared) => prepared.Scene.Nodes.OfType<VisualSceneLine>().Where(n => n.Role?.EndsWith("-spoke") == true);
    private static PreparedVisual Prepare(Chart chart) => chart.Prepare(VisualExportRequest.ForChart(chart).Context);
}
