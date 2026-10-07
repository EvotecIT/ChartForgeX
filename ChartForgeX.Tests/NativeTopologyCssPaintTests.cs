using System;
using System.Linq;
using System.Xml.Linq;
using ChartForgeX.Topology;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class NativeTopologyCssPaintTests {
    [Fact]
    public void AuthoredVariablePaintRetainsItsLiteralRasterFallbackOnRoutesAndMarkers() {
        var chart = TopologyChart.Create().WithId("variable-route").WithViewport(480, 240, 20).WithLegend(null)
            .AddNode("source", "Source", 40, 90, width: 80, height: 50)
            .AddNode("target", "Target", 340, 90, width: 80, height: 50)
            .AddEdge("route", "source", "target", routing: TopologyEdgeRouting.Straight)
            .WithEdgeColor("route", "var(--relationship, #CC224480)")
            .WithEdgeStroke("route", opacity: .5)
            .WithEdgeMarkers("route", TopologyMarkerKind.Circle, TopologyMarkerKind.Diamond);
        var options = new TopologyRenderOptions { IncludeLegend = false, IncludeNodeLabels = false }.WithPlainTopologyEdges();
        var prepared = chart.Prepare(options);
        var svg = XDocument.Parse(prepared.ToSvg());
        var route = Assert.Single(svg.Descendants(), element => (string?)element.Attribute("data-cfx-role") == "topology-edge-line");
        Assert.Contains("var(--relationship,", (string?)route.Attribute("stroke"));
        Assert.Contains("50%", (string?)route.Attribute("stroke"));
        var markers = svg.Descendants().Where(element => (string?)element.Attribute("data-cfx-role") == "topology-marker").ToArray();
        Assert.Equal(2, markers.Length);
        Assert.All(markers, marker => Assert.Equal((string?)route.Attribute("stroke"), (string?)marker.Attribute("stroke")));
        chart.WithEdgeColor("route", "#CC224480");
        Assert.Equal(chart.ToPng(options), prepared.ToPng());
        Assert.Contains("var(--relationship,", prepared.ToSvg());
        Assert.Equal("var(--relationship, #CC224480)", prepared.ToInterchangeEnvelope().Edges.Single().Color);
    }
}
