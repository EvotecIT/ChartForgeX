using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class HorizontalViewportBoundaryTests {
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void HorizontalLinesApplyThePhysicalValueWindow(bool step) {
        var chart = Model(step);
        var context = VisualExportRequest.ForChart(chart).Context;
        var prepared = chart.Prepare(context, new ChartCartesianViewport(x: new ChartAxisWindow(15, 35)));
        Assert.Equal(15, prepared.XProjection!.Minimum); Assert.Equal(35, prepared.XProjection.Maximum);
        Assert.Equal(0, prepared.YProjection!.Minimum); Assert.Equal(4, prepared.YProjection.Maximum);
        Assert.NotEmpty(prepared.ToPng());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void FullHorizontalScenesRetainTheirObservationsWithDetachedPhysicalProjections(bool step) {
        var chart = Model(step);
        var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        Assert.NotNull(prepared.XProjection); Assert.NotNull(prepared.YProjection);
        Assert.Equal(3, prepared.Regions.Count(region => region.Role == "point"));
        Assert.Contains(prepared.Scene.Nodes.OfType<VisualSceneText>(), label => label.Role == "data-label");
        Assert.NotEmpty(prepared.ToPng());
    }

    private static Chart Model(bool step) {
        var chart = Chart.Create().WithSize(800, 400).WithHeader(false).WithLegend(false)
            .WithXAxisBounds(0, 40).WithYAxisBounds(0, 4).WithLineMarkers(ChartLineMarkerMode.All);
        chart.Options.LineOrientation = ChartOrientation.Horizontal;
        chart.Options.ShowDataLabels = true;
        var points = new[] { new ChartPoint(1, 10), new ChartPoint(2, 20), new ChartPoint(3, 30) };
        return step ? chart.AddStepLine("Values", points) : chart.AddLine("Values", points);
    }
}
