using System.Globalization;
using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class CartesianMarkerBoundsTests {
    [Theory]
    [InlineData(ChartSeriesKind.Bubble, ChartScaleKind.Linear, false, 320, 220)]
    [InlineData(ChartSeriesKind.Bubble, ChartScaleKind.Linear, true, 640, 360)]
    [InlineData(ChartSeriesKind.Bubble, ChartScaleKind.Logarithmic, false, 320, 220)]
    [InlineData(ChartSeriesKind.Scatter, ChartScaleKind.Linear, false, 320, 220)]
    [InlineData(ChartSeriesKind.Scatter, ChartScaleKind.Logarithmic, true, 320, 220)]
    [InlineData(ChartSeriesKind.Line, ChartScaleKind.Linear, true, 320, 220)]
    [InlineData(ChartSeriesKind.Line, ChartScaleKind.Logarithmic, false, 320, 220)]
    public void AutomaticBoundsContainTheCompleteMarkerSurface(ChartSeriesKind kind, ChartScaleKind scale,
        bool secondary, int width, int height) {
        var chart = Create(kind, secondary, width, height, negativeMinimum: kind != ChartSeriesKind.Bubble && scale == ChartScaleKind.Linear);
        chart.Options.XAxis.Scale = scale;
        (secondary ? chart.Options.SecondaryYAxis : chart.Options.YAxis).Scale = scale;
        var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        var plot = Plot(prepared.Scene);
        var markers = prepared.Scene.Nodes.OfType<VisualSceneEllipse>().Where(node => node.Role is "marker" or "bubble").ToArray();
        Assert.Equal(3, markers.Length);
        foreach (var marker in markers) {
            var stroke = marker.Stroke.HasValue ? marker.StrokeWidth / 2 : 0;
            Assert.True(marker.Cx - marker.Rx - stroke >= plot.Left - 1e-7);
            Assert.True(marker.Cx + marker.Rx + stroke <= plot.Right + 1e-7);
            Assert.True(marker.Cy - marker.Ry - stroke >= plot.Top - 1e-7);
            Assert.True(marker.Cy + marker.Ry + stroke <= plot.Bottom + 1e-7);
        }
        // SVG serializes the same geometry that native PNG consumes; no backend padding path exists.
        var svgMarkers = XDocument.Parse(prepared.ToSvg()).Descendants().Where(element => element.Name.LocalName == "ellipse"
            && (string?)element.Attribute("data-cfx-role") is "marker" or "bubble").ToArray();
        Assert.Equal(markers.Length, svgMarkers.Length);
        for (var index = 0; index < markers.Length; index++) {
            Assert.InRange(Math.Abs(Number(svgMarkers[index], "cx") - markers[index].Cx), 0, .001);
            Assert.InRange(Math.Abs(Number(svgMarkers[index], "cy") - markers[index].Cy), 0, .001);
            Assert.InRange(Math.Abs(Number(svgMarkers[index], "rx") - markers[index].Rx), 0, .001);
        }
        Assert.True(prepared.ToPng().Length > 64);
    }

    [Theory]
    [InlineData(ChartSeriesKind.Bubble, false)]
    [InlineData(ChartSeriesKind.Scatter, true)]
    [InlineData(ChartSeriesKind.Line, false)]
    public void ExplicitBoundsKeepExtremaAtTheAuthoredPlotEdges(ChartSeriesKind kind, bool secondary) {
        var chart = Create(kind, secondary, 320, 220).WithXAxisBounds(1, 5);
        (secondary ? chart.Options.SecondaryYAxis : chart.Options.YAxis).WithBounds(20, 45);
        var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        var plot = Plot(prepared.Scene);
        var markers = prepared.Scene.Nodes.OfType<VisualSceneEllipse>().Where(node => node.Role is "marker" or "bubble").ToArray();
        Assert.Equal(3, markers.Length);
        Assert.Equal(plot.Left, markers[0].Cx, 7);
        Assert.Equal(plot.Bottom, markers[0].Cy, 7);
        Assert.Equal(plot.Right, markers[2].Cx, 7);
        Assert.Equal(plot.Top, markers[2].Cy, 7);
        Assert.True(markers[2].Cy - markers[2].Ry < plot.Top);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void OneExplicitEndpointKeepsOnlyThatEndpointFixed(bool explicitMaximum) {
        var chart = Create(ChartSeriesKind.Scatter, false, 320, 220);
        var axis = chart.Options.XAxis;
        if (explicitMaximum) axis.Maximum = 5;
        else axis.Minimum = 1;
        var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        var plot = Plot(prepared.Scene);
        var markers = prepared.Scene.Nodes.OfType<VisualSceneEllipse>().Where(node => node.Role == "marker").ToArray();
        if (explicitMaximum) {
            Assert.Equal(plot.Right, markers[2].Cx, 7);
            Assert.True(markers[0].Cx - markers[0].Rx >= plot.Left - 1e-7);
        } else {
            Assert.Equal(plot.Left, markers[0].Cx, 7);
            Assert.True(markers[2].Cx + markers[2].Rx <= plot.Right + 1e-7);
        }
    }

    private static Chart Create(ChartSeriesKind kind, bool secondary, int width, int height, bool negativeMinimum = false) {
        var chart = Chart.Create().WithSize(width, height).WithHeader(false).WithLegend(false).WithDataLabels(false)
            .WithLineMarkers(ChartLineMarkerMode.All);
        var points = new[] { new ChartPoint(1, negativeMinimum ? -20 : 20), new ChartPoint(3, 35), new ChartPoint(5, 45) };
        if (kind == ChartSeriesKind.Bubble)
            chart.AddBubble("Samples", new[] { new ChartBubble(1, 20, 2), new ChartBubble(3, 35, 10), new ChartBubble(5, 45, 40) });
        else {
            if (kind == ChartSeriesKind.Scatter) chart.AddScatter("Samples", points);
            else chart.AddLine("Samples", points);
            chart.Series[0].WithMarkerRadius(12);
        }
        if (secondary) chart.Series[0].UseSecondaryYAxis();
        return chart;
    }

    private static ChartRect Plot(VisualScene scene) {
        var horizontal = Assert.Single(scene.Nodes.OfType<VisualSceneLine>(), node => node.Role == "axis-x");
        var vertical = Assert.Single(scene.Nodes.OfType<VisualSceneLine>(), node => node.Role == "axis-y");
        return new ChartRect(horizontal.Start.X, vertical.Start.Y, horizontal.End.X - horizontal.Start.X,
            vertical.End.Y - vertical.Start.Y);
    }

    private static double Number(XElement element, string attribute) => double.Parse(element.Attribute(attribute)!.Value, CultureInfo.InvariantCulture);
}
