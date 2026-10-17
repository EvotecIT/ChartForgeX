using System.Globalization;
using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class V2LineEndpointMarkerTests {
    [Theory]
    [InlineData(ChartSeriesKind.Line, ChartStepPosition.End)]
    [InlineData(ChartSeriesKind.Area, ChartStepPosition.End)]
    [InlineData(ChartSeriesKind.StepLine, ChartStepPosition.Start)]
    [InlineData(ChartSeriesKind.StepLine, ChartStepPosition.Middle)]
    [InlineData(ChartSeriesKind.StepLine, ChartStepPosition.End)]
    [InlineData(ChartSeriesKind.StepArea, ChartStepPosition.Start)]
    [InlineData(ChartSeriesKind.StepArea, ChartStepPosition.Middle)]
    [InlineData(ChartSeriesKind.StepArea, ChartStepPosition.End)]
    public void SharedLastMarkerEndsExactlyOnTheFinalObservationAndKeepsItsLabel(ChartSeriesKind kind, ChartStepPosition position) {
        var series = new ChartSeries(new ChartSeriesId("observed"), "Observed", kind,
            new[] { new ChartPoint(1, 3), new ChartPoint(2, 9), new ChartPoint(3, 5) });
        if (kind is ChartSeriesKind.StepLine or ChartSeriesKind.StepArea) series.WithInterpolation(ChartInterpolation.Step, position);
        series.WithPointLabel(2, "Final observation").WithDataLabels();
        var chart = Chart.Create().WithSize(640, 360).WithHeader(false).WithLegend(false).AddSeries(series);
        var context = new VisualRenderContext(new VisualLayoutOptions(new VisualSize(640, 360)));
        var prepared = chart.Prepare(context); var scene = prepared.Scene;
        var marker = Assert.Single(scene.Nodes.OfType<VisualSceneEllipse>(), node => node.Role == "marker");
        var line = Assert.Single(scene.Nodes.OfType<VisualScenePath>(), node => node.Role == "line");
        var terminal = line.Commands[line.Commands.Count - 1];
        Assert.Equal(terminal.X, marker.Cx, 7); Assert.Equal(terminal.Y, marker.Cy, 7);
        Assert.Equal(line.Commands.Max(command => command.X), marker.Cx, 7);
        var observation = Assert.Single(scene.Nodes.OfType<VisualSceneGroup>(), group => group.Id == "series-0-point-2");
        Assert.Equal("3", observation.Metadata["data-cfx-x"]); Assert.Equal("5", observation.Metadata["data-cfx-y"]);
        Assert.Contains(scene.Nodes.OfType<VisualSceneText>(), text => text.Role == "data-label" && text.Text.Lines.Any(line => line.Text == "Final observation"));
        var svgMarker = Assert.Single(XDocument.Parse(prepared.ToSvg()).Descendants(), element => (string?)element.Attribute("data-cfx-role") == "marker");
        Assert.Equal(marker.Cx, double.Parse(svgMarker.Attribute("cx")!.Value, CultureInfo.InvariantCulture), 3);
        Assert.Equal(marker.Cy, double.Parse(svgMarker.Attribute("cy")!.Value, CultureInfo.InvariantCulture), 3);
        chart.WithLineMarkers(ChartLineMarkerMode.None);
        Assert.DoesNotContain(chart.Prepare(context).Scene.Nodes, node => node.Role == "marker");
        series.Markers.Enabled = true;
        Assert.Equal(3, chart.Prepare(context).Scene.Nodes.Count(node => node.Role == "marker"));
    }
}
