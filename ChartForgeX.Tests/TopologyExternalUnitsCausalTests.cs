using System.Text.Json;
using System.Xml.Linq;
using ChartForgeX.Topology;
using ChartForgeX.Typography;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Current public native behavior only; no new caption, glyph-scale or captured-node API is required.</summary>
[Collection(nameof(FontRegistryCollection))]
public sealed class TopologyExternalUnitsCausalTests {
    [Theory]
    [InlineData(TopologyNodeDisplayMode.Icon, TopologyIconShape.Router, false)]
    [InlineData(TopologyNodeDisplayMode.Icon, TopologyIconShape.Database, false)]
    [InlineData(TopologyNodeDisplayMode.Tile, TopologyIconShape.Router, false)]
    [InlineData(TopologyNodeDisplayMode.Tile, TopologyIconShape.Database, false)]
    [InlineData(TopologyNodeDisplayMode.Icon, TopologyIconShape.Router, true)]
    [InlineData(TopologyNodeDisplayMode.Icon, TopologyIconShape.Database, true)]
    [InlineData(TopologyNodeDisplayMode.Tile, TopologyIconShape.Router, true)]
    [InlineData(TopologyNodeDisplayMode.Tile, TopologyIconShape.Database, true)]
    public void ExistingExternalGlyphSurfaceAndCaptionRestoreAfterNativeOutputFitting(TopologyNodeDisplayMode mode, TopologyIconShape glyph, bool shrink) {
        var chart = Chart(mode, glyph, shrink); var name = "external-units-" + mode + "-" + glyph + (shrink ? "-shrink" : "-enlarge");
        CaptureInput(chart, name);
        var originalWidth = chart.Nodes[0].Width;
        var prepared = chart.Prepare(); var svg = prepared.ToSvg(); var png = prepared.ToPng();
        var envelope = prepared.ToInterchangeEnvelope(); var json = envelope.ToJson();
        Capture(name + "-direct", svg, png, json);
        var restored = VisualArtifactInterchangeEnvelope.FromJson(json).ToTopologyChart().Prepare();
        var restoredSvg = restored.ToSvg(); var restoredPng = restored.ToPng(); var restoredJson = restored.ToInterchangeEnvelope().ToJson();
        Capture(name + "-restored", restoredSvg, restoredPng, restoredJson);
        var factor = envelope.Nodes[0].Width!.Value / originalWidth;
        Write(name + ".projection.json", JsonSerializer.Serialize(new { factor, originalWidth, capturedWidth = envelope.Nodes[0].Width }));
        // Both actual native/portable observations precede these semantic regression assertions.
        if (shrink) Assert.InRange(factor, double.Epsilon, .999999); else Assert.True(factor > 1);
        Assert.Equal(Paint(svg), Paint(restoredSvg)); Assert.Equal(png, restoredPng);
        Assert.Equal(svg, prepared.ToSvg()); Assert.Equal(png, prepared.ToPng());
    }

    [Theory]
    [InlineData(TopologyNodeDisplayMode.Icon, TopologyIconShape.Router)]
    [InlineData(TopologyNodeDisplayMode.Icon, TopologyIconShape.Database)]
    [InlineData(TopologyNodeDisplayMode.Tile, TopologyIconShape.Router)]
    [InlineData(TopologyNodeDisplayMode.Tile, TopologyIconShape.Database)]
    public void OrdinaryManualScaleOneKeepsExistingExternalGeometryAndPortableSemantics(TopologyNodeDisplayMode mode, TopologyIconShape glyph) {
        var chart = Chart(mode, glyph, false); chart.LayoutMode = TopologyLayoutMode.Manual;
        chart.WithRenderOptions(new TopologyRenderOptions { IncludeLegend = false, FitContentToViewport = false });
        var name = "external-units-" + mode + "-" + glyph + "-unit-control"; CaptureInput(chart, name);
        var prepared = chart.Prepare(); var svg = prepared.ToSvg(); var png = prepared.ToPng(); var json = prepared.ToInterchangeEnvelope().ToJson();
        Capture(name + "-direct", svg, png, json);
        var restored = VisualArtifactInterchangeEnvelope.FromJson(json).ToTopologyChart().Prepare();
        var restoredSvg = restored.ToSvg(); var restoredPng = restored.ToPng(); Capture(name + "-restored", restoredSvg, restoredPng, restored.ToInterchangeEnvelope().ToJson());
        Assert.DoesNotContain("preparedActorBasis", json, StringComparison.Ordinal);
        Assert.DoesNotContain("preparedNodeBasis", json, StringComparison.Ordinal);
        Assert.Equal(Paint(svg), Paint(restoredSvg)); Assert.Equal(png, restoredPng);
    }

    private static TopologyChart Chart(TopologyNodeDisplayMode mode, TopologyIconShape glyph, bool shrink) {
        var chart = TopologyChart.Create().WithId("external-native-units").WithTitle("").WithLegend(null)
            .WithViewport(shrink ? 320 : 640, shrink ? 240 : 360, 0).WithLayout(TopologyLayoutMode.Layered);
        chart.Nodes.Add(new TopologyNode { Id = "node", Label = "Native participant", Subtitle = "A native caption", X = 40, Y = 30,
            Width = shrink ? 920 : 220, Height = shrink ? 720 : 140, Shape = TopologyNodeShape.Rounded, IconShape = glyph,
            Kind = TopologyNodeKind.Generic, DisplayMode = mode, PreserveDisplayModeSize = true, ShowStatusBadge = false, Color = "#2563EB",
            TextStyle = new TextStyleOverride { FontFamily = BrowserProofFonts.Family },
            SubtitleTextStyle = new TextStyleOverride { FontFamily = BrowserProofFonts.Family, FontSize = 13, FontWeight = "500" }
        });
        chart.WithRenderOptions(new TopologyRenderOptions { IncludeLegend = false, FitContentToViewport = true, WrapNodeLabels = true, MaxNodeLabelLines = 4, MaxNodeSubtitleLines = 4 });
        return chart;
    }
    private static string Paint(string svg) {
        var node = XDocument.Parse(svg).Descendants().Single(element => (string?)element.Attribute("data-cfx-role") == "topology-node"
            && (string?)element.Attribute("data-node-id") == "node");
        return string.Join("\n", node.Descendants().Where(element => element.Name.LocalName is "rect" or "ellipse" or "path" or "line" or "text" or "use")
            .Select(element => element.Name.LocalName + " " + string.Join(" ", element.Attributes().Where(attribute =>
                attribute.Name.LocalName is "x" or "y" or "width" or "height" or "cx" or "cy" or "rx" or "ry" or "d" or "fill" or "stroke" or "stroke-width"
                    or "stroke-linejoin" or "stroke-linecap" or "transform" or "font-size" or "font-family" or "font-weight")
                .Select(attribute => attribute.Name.LocalName + "=" + attribute.Value)) + " " + (element.Name.LocalName == "text" ? element.Value : "")));
    }
    private static void CaptureInput(TopologyChart chart, string name) => Write(name + ".input.json", JsonSerializer.Serialize(new {
        chart.LayoutMode, chart.Viewport.Width, chart.Viewport.Height, Nodes = chart.Nodes.Select(node => new { node.Id, node.Label, node.Subtitle, node.X, node.Y, node.Width, node.Height,
            node.Shape, node.IconShape, node.DisplayMode, node.PreserveDisplayModeSize, node.ShowStatusBadge, node.Color })
    }));
    private static void Capture(string name, string svg, byte[] png, string json) {
        Write(name + ".svg", svg); Write(name + ".json", json); var output = Output(); if (output != null) File.WriteAllBytes(Path.Combine(output, name + ".native.png"), png);
    }
    private static void Write(string name, string text) { var output = Output(); if (output != null) File.WriteAllText(Path.Combine(output, name), text); }
    private static string? Output() { var output = Environment.GetEnvironmentVariable("CFX_EXTERNAL_UNIT_CAPTURE_DIRECTORY"); if (string.IsNullOrWhiteSpace(output)) return null; Directory.CreateDirectory(output); return output; }
}
