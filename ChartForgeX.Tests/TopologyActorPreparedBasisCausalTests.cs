using System.Globalization;
using System.Text.Json;
using System.Xml.Linq;
using ChartForgeX.Rendering;
using ChartForgeX.Topology;
using ChartForgeX.Typography;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

[Collection(nameof(FontRegistryCollection))]
public sealed class TopologyActorPreparedBasisCausalTests {
    [Theory]
    [InlineData(TopologyIconShape.Router, false)]
    [InlineData(TopologyIconShape.Database, false)]
    [InlineData(TopologyIconShape.Router, true)]
    [InlineData(TopologyIconShape.Database, true)]
    public void FittedActorGlyphAndCaptionRestoreThroughCurrentPublicNativeApis(TopologyIconShape glyph, bool shrink) {
        var chart = Chart(glyph, shrink);
        var name = "actor-basis-" + glyph + (shrink ? "-shrink" : "-enlarge");
        CaptureRequest(chart, name);
        var direct = chart.Prepare(); var svg = direct.ToSvg(); var png = direct.ToPng();
        var envelope = direct.ToInterchangeEnvelope(); var json = envelope.ToJson();
        Capture(svg, png, json, name + "-direct");
        var restored = VisualArtifactInterchangeEnvelope.FromJson(json).ToTopologyChart();
        var retained = restored.Prepare(); var afterSvg = retained.ToSvg(); var afterPng = retained.ToPng();
        Capture(afterSvg, afterPng, retained.ToInterchangeEnvelope().ToJson(), name + "-restored");
        Assert.Equal(Paint(svg), Paint(afterSvg));
        Assert.Equal(png, afterPng);
        Assert.Equal(svg, direct.ToSvg()); Assert.Equal(png, direct.ToPng());
    }

    [Theory]
    [InlineData(TopologyIconShape.Router)]
    [InlineData(TopologyIconShape.Database)]
    public void OrdinaryManualScaleOneRetainsExistingNativeGeometryAndPortableBytes(TopologyIconShape glyph) {
        var chart = Chart(glyph, false); chart.LayoutMode = TopologyLayoutMode.Manual;
        chart.WithRenderOptions(new TopologyRenderOptions { IncludeLegend = false, FitContentToViewport = false });
        var name = "actor-basis-" + glyph + "-unit-control"; CaptureRequest(chart, name);
        var direct = chart.Prepare(); var svg = direct.ToSvg(); var png = direct.ToPng(); var json = direct.ToInterchangeEnvelope().ToJson();
        Capture(svg, png, json, name + "-direct");
        var restored = VisualArtifactInterchangeEnvelope.FromJson(json).ToTopologyChart().Prepare();
        var afterSvg = restored.ToSvg(); var afterPng = restored.ToPng(); Capture(afterSvg, afterPng, restored.ToInterchangeEnvelope().ToJson(), name + "-restored");
        Assert.DoesNotContain("preparedNodeBasis", json, StringComparison.Ordinal);
        Assert.Equal(Paint(svg), Paint(afterSvg)); Assert.Equal(png, afterPng);
    }

    internal static TopologyChart Chart(TopologyIconShape glyph, bool shrink,
        TopologyNodeShape shape = TopologyNodeShape.ActorIcon, TopologyNodeDisplayMode mode = TopologyNodeDisplayMode.Card) {
        var chart = TopologyChart.Create().WithId("actor-prepared-basis").WithViewport(shrink ? 320 : 640, shrink ? 240 : 360, 0)
            .WithLayout(TopologyLayoutMode.Layered).AddNode("actor", "Native participant", 40, 30, width: shrink ? 920 : 220, height: shrink ? 720 : 140);
        var node = chart.Nodes[0]; node.Shape = shape; node.IconShape = shape == TopologyNodeShape.ActorIcon ? glyph : null;
        node.Kind = TopologyNodeKind.Generic; node.DisplayMode = mode; node.PreserveDisplayModeSize = true; node.ShowStatusBadge = false;
        node.Color = "#2563EB"; node.Subtitle = "A native caption";
        node.TextStyle = new TextStyleOverride { FontFamily = BrowserProofFonts.Family };
        node.SubtitleTextStyle = new TextStyleOverride { FontFamily = BrowserProofFonts.Family, FontSize = 13, FontWeight = "500" };
        chart.WithRenderOptions(new TopologyRenderOptions { IncludeLegend = false, FitContentToViewport = true, WrapNodeLabels = true, MaxNodeLabelLines = 4, MaxNodeSubtitleLines = 4 });
        return chart;
    }

    internal static string Paint(string svg) {
        var group = Node(svg);
        return string.Join("\n", group.Descendants().Where(item => (string?)item.Attribute("data-cfx-role")
            is "topology-node-surface" or "topology-node-icon" or "topology-icon-surface")
            .Select(item => item.Name.LocalName + " " + string.Join(" ", item.Attributes().Where(a =>
                a.Name.LocalName is "x" or "y" or "width" or "height" or "cx" or "cy" or "rx" or "ry" or "d" or "fill" or "stroke" or "stroke-width" or "stroke-linejoin" or "stroke-linecap")
                .Select(a => a.Name.LocalName + "=" + a.Value))));
    }
    internal static XElement Node(string svg) => XDocument.Parse(svg).Descendants().Single(item =>
        (string?)item.Attribute("data-cfx-role") == "topology-node" && (string?)item.Attribute("data-node-id") == "actor");
    internal static XElement GlyphSurface(string svg) => Node(svg).Descendants().First(item => (string?)item.Attribute("data-cfx-role") == "topology-icon-surface");
    internal static double Number(XElement element, string name) => double.Parse(element.Attribute(name)!.Value, CultureInfo.InvariantCulture);

    internal static void CaptureRequest(TopologyChart chart, string name) {
        var output = Output(); if (output == null) return;
        File.WriteAllText(Path.Combine(output, name + ".request.json"), JsonSerializer.Serialize(new { chart.LayoutMode,
            chart.Viewport.Width, chart.Viewport.Height, Nodes = chart.Nodes.Select(n => new { n.Id, n.X, n.Y, n.Width, n.Height, n.Shape, n.IconShape, n.DisplayMode }) }));
    }
    internal static void Capture(string svg, byte[] png, string json, string name) {
        var output = Output(); if (output == null) return;
        var stem = Path.Combine(output, name); File.WriteAllText(stem + ".svg", svg); File.WriteAllBytes(stem + ".native.png", png); File.WriteAllText(stem + ".json", json);
    }
    private static string? Output() {
        var output = Environment.GetEnvironmentVariable("CFX_NATIVE_CAPTURE_DIRECTORY");
        if (string.IsNullOrWhiteSpace(output)) return null;
        Directory.CreateDirectory(output); return output;
    }
}
