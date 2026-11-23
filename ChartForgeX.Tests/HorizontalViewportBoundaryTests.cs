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
        var chart = Chart.Create().Size(800, 400).ShowHeader(false).Legend(false)
            .XAxis(x => x.Bounds(0, 40)).YAxis(y => y.Bounds(0, 4)).Lines(l => l.MarkerMode(ChartLineMarkerMode.All));
        chart.Options.Lines.Orientation = ChartOrientation.Horizontal;
        chart.Options.DataLabels.Visible = true;
        var points = new[] { new ChartPoint(1, 10), new ChartPoint(2, 20), new ChartPoint(3, 30) };
        return step ? chart.AddStepLine("Values", points) : chart.AddLine("Values", points);
    }
}
