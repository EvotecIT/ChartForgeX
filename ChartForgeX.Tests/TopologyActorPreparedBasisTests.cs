using System.Text.Json.Nodes;
using ChartForgeX.Topology;
using ChartForgeX.VisualArtifacts;
using Xunit;
using static ChartForgeX.Tests.TopologyActorPreparedBasisCausalTests;

namespace ChartForgeX.Tests;

[Collection(nameof(FontRegistryCollection))]
public sealed class TopologyActorPreparedBasisTests {
    [Theory]
    [InlineData(TopologyNodeShape.Actor, TopologyNodeDisplayMode.Card)]
    [InlineData(TopologyNodeShape.Actor, TopologyNodeDisplayMode.CompactCard)]
    [InlineData(TopologyNodeShape.ActorHollow, TopologyNodeDisplayMode.Card)]
    [InlineData(TopologyNodeShape.ActorHollow, TopologyNodeDisplayMode.CompactCard)]
    [InlineData(TopologyNodeShape.ActorBusiness, TopologyNodeDisplayMode.Card)]
    [InlineData(TopologyNodeShape.ActorBusiness, TopologyNodeDisplayMode.CompactCard)]
    [InlineData(TopologyNodeShape.ActorBusinessHollow, TopologyNodeDisplayMode.Card)]
    [InlineData(TopologyNodeShape.ActorBusinessHollow, TopologyNodeDisplayMode.CompactCard)]
    [InlineData(TopologyNodeShape.ActorSilhouette, TopologyNodeDisplayMode.Card)]
    [InlineData(TopologyNodeShape.ActorSilhouette, TopologyNodeDisplayMode.CompactCard)]
    [InlineData(TopologyNodeShape.ActorIcon, TopologyNodeDisplayMode.Card)]
    [InlineData(TopologyNodeShape.ActorIcon, TopologyNodeDisplayMode.CompactCard)]
    public void EveryNativeActorFigureAndMeasuredCaptionRetainsItsPreparedUnits(TopologyNodeShape shape, TopologyNodeDisplayMode mode) {
        var chart = Chart(TopologyIconShape.Router, false, shape, mode); var direct = chart.Prepare();
        var svg = direct.ToSvg(); var png = direct.ToPng(); var json = direct.ToInterchangeEnvelope().ToJson();
        Capture(svg, png, json, "actor-basis-" + shape + "-" + mode + "-direct");
        var restored = VisualArtifactInterchangeEnvelope.FromJson(json).ToTopologyChart().Prepare();
        var afterSvg = restored.ToSvg(); var afterPng = restored.ToPng(); var afterJson = restored.ToInterchangeEnvelope().ToJson();
        Capture(afterSvg, afterPng, afterJson, "actor-basis-" + shape + "-" + mode + "-restored");
        Assert.Contains("preparedNodeBasis", json, StringComparison.Ordinal);
        Assert.Equal(Paint(svg), Paint(afterSvg)); Assert.Equal(png, afterPng);
        var recaptured = VisualArtifactInterchangeEnvelope.FromJson(afterJson).ToTopologyChart().Prepare();
        Assert.Equal(Paint(afterSvg), Paint(recaptured.ToSvg())); Assert.Equal(afterPng, recaptured.ToPng());
    }

    [Theory]
    [InlineData(TopologyIconShape.Router)]
    [InlineData(TopologyIconShape.Database)]
    public void PositionOnlyEditTranslatesTheCapturedGlyphAndPolicyEditsResumeNativeGeometry(TopologyIconShape glyph) {
        var direct = Chart(glyph, false).Prepare(); var svg = direct.ToSvg(); var png = direct.ToPng(); var envelope = direct.ToInterchangeEnvelope();
        var json = envelope.ToJson(); var restored = VisualArtifactInterchangeEnvelope.FromJson(json).ToTopologyChart();
        var first = GlyphSurface(svg); restored.WithViewport(1200, 900, 0); restored.Nodes[0].X += 37; restored.Nodes[0].Y += 29;
        var moved = restored.Prepare(); var movedSvg = moved.ToSvg(); var movedJson = moved.ToInterchangeEnvelope().ToJson();
        Capture(movedSvg, moved.ToPng(), movedJson, "actor-basis-" + glyph + "-translated");
        var translated = GlyphSurface(movedSvg);
        if (first.Name.LocalName == "rect") {
            Assert.Equal(Number(first, "x") + 37, Number(translated, "x"), 3); Assert.Equal(Number(first, "y") + 29, Number(translated, "y"), 3);
            Assert.Equal(Number(first, "width"), Number(translated, "width"));
        } else {
            Assert.Equal(Number(first, "cx") + 37, Number(translated, "cx"), 3); Assert.Equal(Number(first, "cy") + 29, Number(translated, "cy"), 3);
            Assert.Equal(Number(first, "rx"), Number(translated, "rx"));
        }
        var originalBasis = JsonNode.Parse(json)!["nodes"]![0]!["topology"]!["preparedNodeBasis"]!;
        var movedBasis = JsonNode.Parse(movedJson)!["nodes"]![0]!["topology"]!["preparedNodeBasis"]!;
        Assert.Equal(originalBasis["glyphAnchorX"]!.ToJsonString(), movedBasis["glyphAnchorX"]!.ToJsonString());
        Assert.Equal(originalBasis["glyphAnchorY"]!.ToJsonString(), movedBasis["glyphAnchorY"]!.ToJsonString());
        restored.Nodes[0].Width += 10;
        Assert.DoesNotContain("preparedNodeBasis", restored.Prepare().ToInterchangeEnvelope().ToJson(), StringComparison.Ordinal);
        var changed = VisualArtifactInterchangeEnvelope.FromJson(json).ToTopologyChart(); changed.Nodes[0].IconShape = TopologyIconShape.Badge;
        Assert.DoesNotContain("preparedNodeBasis", changed.Prepare().ToInterchangeEnvelope().ToJson(), StringComparison.Ordinal);
        var automatic = VisualArtifactInterchangeEnvelope.FromJson(json).ToTopologyChart(); automatic.LayoutMode = TopologyLayoutMode.Layered;
        automatic.WithViewport(1600, 1200, 0); automatic.WithRenderOptions(new TopologyRenderOptions { FitContentToViewport = false, IncludeLegend = false });
        Assert.DoesNotContain("preparedNodeBasis", automatic.Prepare().ToInterchangeEnvelope().ToJson(), StringComparison.Ordinal);
        Assert.Equal(svg, direct.ToSvg()); Assert.Equal(png, direct.ToPng()); Assert.Equal(json, envelope.ToJson());
    }

    [Fact]
    public void CapturedActorNamedContourKeepsTheExistingConnectorGapInTheCapturedUnits() {
        var json = Chart(TopologyIconShape.Router, false).Prepare().ToInterchangeEnvelope().ToJson();
        var restored = VisualArtifactInterchangeEnvelope.FromJson(json).ToTopologyChart(); restored.WithViewport(1600, 1200, 0);
        restored.AddNode("target", "Target", 1100, 50, width: 80, height: 80)
            .AddEdge("connector", "actor", "target", routing: TopologyEdgeRouting.Straight);
        restored.Nodes[1].Kind = TopologyNodeKind.Generic; restored.Nodes[1].ShowStatusBadge = false;
        restored.Edges[0].SourcePort = TopologyEdgePort.Right;
        var prepared = restored.Prepare(); var svg = prepared.ToSvg(); var envelope = prepared.ToInterchangeEnvelope();
        Capture(svg, prepared.ToPng(), envelope.ToJson(), "actor-basis-captured-contour-gap");
        var factor = JsonNode.Parse(json)!["nodes"]![0]!["topology"]!["preparedNodeBasis"]!["factor"]!.GetValue<double>();
        var path = Node(svg).Descendants().Single(item => (string?)item.Attribute("data-cfx-role") == "topology-node-surface");
        var numbers = System.Text.RegularExpressions.Regex.Matches(path.Attribute("d")!.Value, @"[-+]?(?:[0-9]*\.)?[0-9]+(?:[eE][-+]?[0-9]+)?")
            .Cast<System.Text.RegularExpressions.Match>().Select(match => double.Parse(match.Value, System.Globalization.CultureInfo.InvariantCulture)).ToArray();
        var right = numbers.Where((_, index) => index % 2 == 0).Max();
        Assert.InRange(Math.Abs(Assert.Single(envelope.Edges).ResolvedRoute[0].X - right - 7 * factor), 0, .0011);
    }

    [Theory]
    [InlineData("height")] [InlineData("shape")] [InlineData("kind")]
    [InlineData("display")] [InlineData("padding")] [InlineData("wrapping")]
    public void NativeGeometryAndShapePolicyEditsDiscardThePrivateCapturedBasis(string policy) {
        var json = Chart(TopologyIconShape.Router, false).Prepare().ToInterchangeEnvelope().ToJson();
        var restored = VisualArtifactInterchangeEnvelope.FromJson(json).ToTopologyChart(); restored.WithViewport(1600, 1200, 0);
        var node = restored.Nodes[0];
        switch (policy) {
            case "height": node.Height += 10; break;
            case "shape": node.Shape = TopologyNodeShape.ActorBusiness; break;
            case "kind": node.Kind = TopologyNodeKind.Server; break;
            case "display": node.DisplayMode = TopologyNodeDisplayMode.CompactCard; break;
            case "padding": node.ContentPadding = 9; break;
            case "wrapping": node.MaximumTextWidth = 160; break;
        }
        var prepared = restored.Prepare(); var svg = prepared.ToSvg(); var png = prepared.ToPng(); var after = prepared.ToInterchangeEnvelope().ToJson();
        Capture(svg, png, after, "actor-basis-edited-" + policy);
        Assert.DoesNotContain("preparedNodeBasis", after, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("factor", 0d)] [InlineData("factor", -1d)] [InlineData("factor", 100000000d)]
    [InlineData("glyphAnchorX", 1000000000d)] [InlineData("glyphAnchorY", -1000000000d)]
    [InlineData("width", 0d)] [InlineData("height", -1d)]
    public void HostileOrContradictoryCapturedUnitsRejectBeforeNativeGeometry(string field, double value) {
        var direct = Chart(TopologyIconShape.Router, false).Prepare(); var json = direct.ToInterchangeEnvelope().ToJson();
        Capture(direct.ToSvg(), direct.ToPng(), json, "node-basis-hostile-" + field + "-" + value.ToString(System.Globalization.CultureInfo.InvariantCulture));
        var root = JsonNode.Parse(json)!; root["nodes"]![0]!["topology"]!["preparedNodeBasis"]![field] = value;
        Assert.ThrowsAny<ArgumentException>(() => VisualArtifactInterchangeEnvelope.FromJson(root.ToJsonString()));
    }

    [Fact]
    public void NonfiniteAndMismatchedWireFactsCannotCreateACapturedGeometryEscape() {
        var json = Chart(TopologyIconShape.Router, false).Prepare().ToInterchangeEnvelope().ToJson();
        var root = JsonNode.Parse(json)!; root["nodes"]![0]!["width"] = root["nodes"]![0]!["width"]!.GetValue<double>() + 1;
        Assert.Throws<ArgumentException>(() => VisualArtifactInterchangeEnvelope.FromJson(root.ToJsonString()));
        root = JsonNode.Parse(json)!; root["nodes"]![0]!["topology"]!["preparedNodeBasis"]!["factor"] = "NaN";
        Assert.ThrowsAny<ArgumentException>(() => VisualArtifactInterchangeEnvelope.FromJson(root.ToJsonString()));
        var text = System.Text.RegularExpressions.Regex.Replace(json, "\"factor\":[-+0-9.eE]+", "\"factor\":1e309");
        Assert.NotEqual(json, text);
        Assert.ThrowsAny<ArgumentException>(() => VisualArtifactInterchangeEnvelope.FromJson(text));
    }

    [Theory]
    [InlineData(TopologyNodeShape.Actor, TopologyNodeDisplayMode.Icon)]
    [InlineData(TopologyNodeShape.Actor, TopologyNodeDisplayMode.Tile)]
    [InlineData(TopologyNodeShape.ActorHollow, TopologyNodeDisplayMode.Icon)]
    [InlineData(TopologyNodeShape.ActorHollow, TopologyNodeDisplayMode.Tile)]
    [InlineData(TopologyNodeShape.ActorBusiness, TopologyNodeDisplayMode.Icon)]
    [InlineData(TopologyNodeShape.ActorBusiness, TopologyNodeDisplayMode.Tile)]
    [InlineData(TopologyNodeShape.ActorBusinessHollow, TopologyNodeDisplayMode.Icon)]
    [InlineData(TopologyNodeShape.ActorBusinessHollow, TopologyNodeDisplayMode.Tile)]
    [InlineData(TopologyNodeShape.ActorSilhouette, TopologyNodeDisplayMode.Icon)]
    [InlineData(TopologyNodeShape.ActorSilhouette, TopologyNodeDisplayMode.Tile)]
    [InlineData(TopologyNodeShape.ActorIcon, TopologyNodeDisplayMode.Icon)]
    [InlineData(TopologyNodeShape.ActorIcon, TopologyNodeDisplayMode.Tile)]
    public void MateriallyScaledLegacyActorIconAndTileModesRestoreThroughTheExistingOriginalNodeOwner(TopologyNodeShape shape, TopologyNodeDisplayMode mode) {
        var chart = Chart(TopologyIconShape.Router, false, shape, mode);
        var prepared = chart.Prepare(); var svg = prepared.ToSvg(); var png = prepared.ToPng(); var json = prepared.ToInterchangeEnvelope().ToJson();
        var name = "actor-basis-legacy-" + shape + "-" + mode;
        Capture(svg, png, json, name + "-prepared");
        var artifact = chart.ToVisualArtifact(); var artifactSvg = artifact.ToSvg(); var artifactPng = artifact.ToPng(); var artifactJson = artifact.ToInterchangeJson();
        Capture(artifactSvg, artifactPng, artifactJson, name + "-artifact");
        var envelope = VisualArtifactInterchangeEnvelope.FromJson(json); var artifactEnvelope = VisualArtifactInterchangeEnvelope.FromJson(artifactJson);
        var restored = envelope.ToTopologyChart().Prepare(); var wrapped = artifactEnvelope.ToTopologyChart().Prepare();
        Capture(restored.ToSvg(), restored.ToPng(), restored.ToInterchangeEnvelope().ToJson(), name + "-restored");
        Capture(wrapped.ToSvg(), wrapped.ToPng(), wrapped.ToInterchangeEnvelope().ToJson(), name + "-wrapped-restored");
        Assert.NotEmpty(svg); Assert.NotEmpty(png); Assert.NotEmpty(artifactSvg); Assert.NotEmpty(artifactPng);
        Assert.Contains("preparedNodeBasis", json, StringComparison.Ordinal); Assert.Equal(json, envelope.ToJson()); Assert.NotEmpty(artifactEnvelope.ToJson());
        Assert.Equal(Paint(svg), Paint(restored.ToSvg())); Assert.Equal(png, restored.ToPng());
        Assert.Equal(Paint(artifactSvg), Paint(wrapped.ToSvg())); Assert.Equal(artifactPng, wrapped.ToPng());
        Assert.Equal(mode, envelope.Nodes[0].Topology!.DisplayMode); Assert.Equal(shape, envelope.Nodes[0].Topology!.Shape);
    }
}
