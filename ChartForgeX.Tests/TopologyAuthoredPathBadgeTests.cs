using System.Text.Json;
using System.Xml.Linq;
using ChartForgeX.Primitives;
using ChartForgeX.Topology;
using ChartForgeX.Typography;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

[Trait("BrowserSuite", "Compatibility")]
public sealed class TopologyAuthoredPathBadgeTests {
    private static TopologyChart Ordinary() {
        var chart = TopologyChart.Create().WithTheme(ChartForgeX.Themes.VisualTheme.Graphite().WithFontFamily(BrowserProofFonts.Family)).WithId("handoff").WithViewport(720, 420, 24);
        chart.AddNode("gateway", "Gateway", 60, 70, width: 200, height: 160);
        chart.AddNode("worker", "Worker", 450, 70, width: 200, height: 160);
        chart.AddEdge("dispatch", "gateway", "worker", "Dispatch", direction: VisualLinkDirection.Forward);
        return chart;
    }
    [Fact]
    public void OrdinaryPublicHandoffUsesOneCurveAndDetachedBadgeFactsThroughPortableRestore() {
        var chart = Ordinary();
        const string path = "M 260.12345678901232 150 C 320.98765432109877 30 390 30 450 150";
        chart.Edges[0].PathGeometry = new TopologyPathGeometry(path); chart.Edges[0].StrokeWidth = 2;
        chart.Edges[0].DashPattern.AddRange(new[] { 5d, 3d }); chart.Edges[0].Color = "#456789";
        var style = new TopologyDetailBadgeStyle { Fill = ChartColor.FromHex("#EEF4FC"), Stroke = ChartColor.FromHex("#456789"), MinimumHeight = 30 };
        chart.Nodes[0].Details.Add(new TopologyNodeDetail { Text = "Queued", BadgeStyle = style, TextStyle = new TextStyleOverride { FontSize = 16, Italic = true } });
        chart.Nodes[0].FitLabelToContent = true;
        var sourcePath = chart.Edges[0].PathGeometry!.PathData; var artifact = chart.ToVisualArtifact();
        var svg = artifact.ToSvg(); var xml = XDocument.Parse(svg);
        var line = Assert.Single(xml.Descendants(), element => (string?)element.Attribute("data-cfx-role") == "topology-edge-line");
        Assert.Contains("C", line.Attribute("d")!.Value);
        Assert.Contains(xml.Descendants(), element => (string?)element.Attribute("data-cfx-role") == "topology-detail-badge");
        var envelope = artifact.ToInterchangeEnvelope(); Assert.Equal(2, envelope.Version);
        var frozenStyle = envelope.Nodes.Single(node => node.Id == "gateway").Details[0].BadgeStyle!;
        style.MinimumHeight = 60; Assert.Equal(30, frozenStyle.MinimumHeight);
        var json = envelope.ToJson(); using var data = JsonDocument.Parse(json);
        var commands = data.RootElement.GetProperty("edges")[0].GetProperty("topology").GetProperty("pathGeometry").GetProperty("commands");
        Assert.Equal(double.Parse(envelope.Edges[0].Topology!.PathGeometry!.PathData.Split(' ')[1], System.Globalization.CultureInfo.InvariantCulture), commands[0][1].GetDouble());
        Assert.Contains("260.12345678901232", sourcePath, StringComparison.Ordinal);
        var restored = VisualArtifactInterchangeEnvelope.FromJson(json).ToTopologyChart();
        Assert.Equal(envelope.Edges[0].Topology!.PathGeometry!.PathData, restored.Edges[0].PathGeometry!.PathData);
        Assert.Equal(new[] { 5d, 3d }, restored.Edges[0].DashPattern);
        Assert.Equal("Queued", restored.Nodes.Single(node => node.Id == "gateway").Details[0].Text);
        Assert.Equal(sourcePath, chart.Edges[0].PathGeometry!.PathData);
        Assert.True(artifact.ToPng().Length > 64); Assert.True(restored.ToPng().Length > 64);
        Assert.Throws<NotSupportedException>(() => VisualArtifactInterchangeEnvelope.FromJson(json.Replace("\"version\":2", "\"version\":1", StringComparison.Ordinal)));
    }

    [Fact]
    public void NullGeometryAndBadgeKeepExactOrdinarySvgPngAndCapturedWireVersion() {
        var chart = Ordinary(); chart.Nodes[0].Details.Add(new TopologyNodeDetail { Text = "Queued" });
        var beforeSvg = chart.ToSvg(); var beforePng = chart.ToPng(); var beforeJson = chart.ToVisualArtifact().ToInterchangeJson();
        chart.Edges[0].PathGeometry = new TopologyPathGeometry("M 260 150 C 320 40 390 40 450 150");
        chart.Nodes[0].Details[0].BadgeStyle = new TopologyDetailBadgeStyle();
        chart.Edges[0].PathGeometry = null; chart.Nodes[0].Details[0].BadgeStyle = null;
        Assert.Equal(beforeSvg, chart.ToSvg()); Assert.Equal(beforePng, chart.ToPng()); Assert.Equal(beforeJson, chart.ToVisualArtifact().ToInterchangeJson());
        Assert.Equal(VisualArtifactInterchangeEnvelope.FromJson(beforeJson).Version, chart.ToVisualArtifact().ToInterchangeEnvelope().Version);
    }

    [Fact]
    public void ContradictoryAuthoredRouteRejectsBeforePaintAndDoesNotBecomeWaypoints() {
        var chart = Ordinary(); chart.Edges[0].PathGeometry = new TopologyPathGeometry("M 260 150 C 320 40 390 40 450 150");
        chart.Edges[0].Waypoints.Add(new ChartPoint(350, 10));
        Assert.False(new TopologyChartValidator().Validate(chart).IsValid);
        Assert.Throws<TopologyValidationException>(() => chart.ToSvg());
        chart.Edges[0].Waypoints.Clear(); chart.Edges[0].PathGeometry = new TopologyPathGeometry("M 260 150 L 300 100 M 350 100 L 450 150");
        Assert.False(new TopologyChartValidator().Validate(chart).IsValid);
    }

    [Theory]
    [InlineData("M 0 0 Q 10 10 20 0")]
    [InlineData("L 0 0 L 10 10")]
    [InlineData("M 0 0 M 1 1 L 2 2")]
    [InlineData("M 0 0 C 1 1 2 2 1000000001 0")]
    public void GeometryRejectsUnsupportedCommandsOrInvalidSubpaths(string path) => Assert.ThrowsAny<ArgumentException>(() => new TopologyPathGeometry(path));
}
