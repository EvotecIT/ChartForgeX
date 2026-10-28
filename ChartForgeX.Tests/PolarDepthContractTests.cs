using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Public form/configuration contracts preserve defaults and retain only authored observations.</summary>
public sealed class PolarDepthContractTests {
    [Fact]
    public void ExplicitDefaultPolicyPreservesTheExistingOpenStraightNativeArtifact() {
        var chart = Fixture(); var original = Prepare(chart); var svg = original.ToSvg(); var png = original.ToPng();
        chart.Series[0].ConfigurePolar(options => { options.Form = ChartLineAreaForm.Line; options.ClosePath = false; options.CurveTension = 0; });
        var explicitDefault = Prepare(chart);
        Assert.Equal(svg, explicitDefault.ToSvg()); Assert.Equal(png, explicitDefault.ToPng());
        Assert.False(Assert.Single(explicitDefault.Scene.Nodes.OfType<VisualScenePath>(), path => path.Role == "polar-line").Close);
        Assert.DoesNotContain(explicitDefault.Scene.Nodes, node => node.Role == "polar-area");
    }

    [Fact]
    public void ConfigurationIsDetachedAndThrowingCallbackDoesNotReplaceInstalledState() {
        var series = Fixture().Series[0]; ChartPolarOptions? retained = null;
        series.ConfigurePolar(options => { retained = options; options.CurveTension = .25; options.ClosePath = true; });
        retained!.CurveTension = 1; Assert.Equal(.25, series.Polar.CurveTension);
        var installed = series.Polar;
        Assert.Throws<InvalidOperationException>(() => series.ConfigurePolar(options => { options.Form = ChartLineAreaForm.Area; throw new InvalidOperationException(); }));
        Assert.Same(installed, series.Polar); Assert.Equal(ChartLineAreaForm.Line, series.Polar.Form);
        Assert.Throws<ArgumentNullException>(() => series.ConfigurePolar(null!));
    }

    [Fact]
    public void InvalidEnumOpacityTensionAndForeignFamilyConfigurationReject() {
        var chart = Fixture(); var options = chart.Series[0].Polar;
        Assert.Throws<ArgumentOutOfRangeException>(() => options.Form = (ChartLineAreaForm)99);
        foreach (var value in new[] { double.NaN, double.PositiveInfinity, -.01, 1.01 }) {
            Assert.Throws<ArgumentOutOfRangeException>(() => options.FillOpacity = value);
            Assert.Throws<ArgumentOutOfRangeException>(() => options.CurveTension = value);
        }
        options.FillOpacity = .2; Assert.Throws<InvalidOperationException>(() => Prepare(chart));
        var foreign = Chart.Create().AddScatter("Points", Values()); foreign.Series[0].Polar.ClosePath = true;
        Assert.Throws<InvalidOperationException>(() => Prepare(foreign));
    }

    [Fact]
    public void ClosedLineUsesTheExistingCurveOwnerWithoutAddingFillOrSourcePoints() {
        var chart = Fixture(); chart.Series[0].ConfigurePolar(options => { options.ClosePath = true; options.CurveTension = .2; });
        var prepared = Prepare(chart); var outline = Assert.Single(prepared.Scene.Nodes.OfType<VisualScenePath>(), path => path.Role == "polar-line");
        Assert.True(outline.Close); Assert.Equal(3, outline.Commands.Count(command => command.Kind == ChartPathCommandKind.CubicTo));
        Assert.DoesNotContain(prepared.Scene.Nodes, node => node.Role == "polar-area");
        Assert.Equal(3, prepared.Regions.Count(region => region.Role == "polar-point"));
    }

    [Fact]
    public void ShortAreaRunsRetainPointsOpenOutlinesAndTruthfulLegendWithoutInventedFill() {
        var chart = Fixture(); var series = chart.Series[0]; series.Points.RemoveAt(2);
        series.ConfigurePolar(options => options.Form = ChartLineAreaForm.Area);
        var prepared = Prepare(chart.WithLegend());
        Assert.Contains(prepared.Diagnostics, diagnostic => diagnostic.Code == "polar.open-area");
        Assert.DoesNotContain(prepared.Scene.Nodes, node => node.Role is "polar-area" or "legend-area");
        Assert.False(Assert.Single(prepared.Scene.Nodes.OfType<VisualScenePath>(), path => path.Role == "polar-line").Close);
        Assert.Equal(2, prepared.Regions.Count(region => region.Role == "polar-point"));
        series.Points.Clear(); Assert.Contains(Prepare(chart).Diagnostics, diagnostic => diagnostic.Code == "polar.no-data");
        series.Points.Add(new ChartPoint(0, 0));
        Assert.Single(Prepare(chart).Regions, region => region.Role == "polar-point");
    }

    internal static Chart Fixture() => Chart.Create().WithSize(400, 320).WithHeader(false).WithLegend(false)
        .WithDataLabels(false).WithAxes(false).WithGrid(false).WithYAxisBounds(0, 100).AddPolar("Signal", Values());
    private static ChartPoint[] Values() => new[] { new ChartPoint(0, 30), new ChartPoint(1, 80), new ChartPoint(2, 60) };
    internal static PreparedVisual Prepare(Chart chart) => chart.Prepare(VisualExportRequest.ForChart(chart).Context);
}
