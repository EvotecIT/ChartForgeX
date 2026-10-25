using System.Xml.Linq;
using ChartForgeX.Mermaid;
using ChartForgeX.Markup.Mermaid;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;
using ChartForgeX.Topology;
using ChartForgeX.Typography;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

[Collection(nameof(FontRegistryCollection))]
public sealed class TopologyAdvancedActorPortableTests {
    [Fact]
    public void CustomGenericRouterIsCapturedAsFiniteNativeFactsAndRestoresWithoutItsProducerCatalog() {
        var definition = new TopologyIconDefinition("tools", "gateway", "A custom router", TopologyNodeKind.Generic, TopologyIconShape.Router) { Symbol = "R", Color = "#2563EB" };
        var pack = new TopologyIconPack("tools", "Finite test icons").AddIcon(definition);
        var catalog = new TopologyIconCatalog().AddPack(pack);
        var family = new MermaidFlowchartRenderOptions { Width = 640, Height = 360, FitContent = false, IconCatalog = catalog,
            Presentation = new MermaidPresentationOptions { FontFamily = BrowserProofFonts.Family, TopologySizing = MermaidTopologySizing.FixedViewport } };
        var copied = (MermaidFlowchartRenderOptions)family.Clone();
        var fenceParser = new MermaidVisualMarkupParser(new MermaidRenderOptions { UseCase = family, WarningPolicy = MermaidWarningPolicy.Reject });
        Assert.NotSame(catalog, copied.IconCatalog); Assert.NotSame(definition, copied.IconCatalog!.Resolve("tools:gateway"));
        const string source = "usecase-beta\nactor Gateway @{icon:'tools:gateway'} <<external system>>";
        var document = new MermaidParser().ParseFlowchart(source).Document!;
        var chart = document.ToTopologyChart(family); var captured = chart.Prepare(); var before = captured.ToSvg(); var png = captured.ToPng();
        var original = captured.ToInterchangeEnvelope(); var json = original.ToJson(); Assert.Equal(2, original.Version);
        var sourceNode = Assert.Single(original.Nodes); Assert.Equal("tools:gateway", sourceNode.IconId);
        Assert.Equal(TopologyNodeKind.Generic, sourceNode.Topology!.Kind); Assert.Equal(TopologyIconShape.Router, sourceNode.Topology.IconShape);
        Assert.Equal(TopologyNodeShape.ActorIcon, sourceNode.Topology.Shape); Assert.Equal("tools:gateway", sourceNode.Extensions["mermaid.property.icon"]); Assert.Equal("'tools:gateway'", sourceNode.Extensions["mermaid.property.icon.raw"]);
        definition.Shape = TopologyIconShape.Badge; definition.Symbol = "EDIT"; definition.Metadata["changed"] = "producer"; catalog.RemovePack("tools");
        document.Nodes[0].Properties["icon"] = "changed:missing"; family.IconCatalog = null;
        Assert.Equal(TopologyIconShape.Router, copied.IconCatalog.Resolve("tools:gateway")!.Shape);
        var fenced = fenceParser.Parse("```mermaid\n" + source + "\n```"); Assert.Empty(fenced.Diagnostics);
        Assert.Equal(TopologyIconShape.Router, Assert.IsType<TopologyChart>(Assert.Single(fenced.Artifacts).Model).Nodes[0].IconShape);
        Assert.Equal(before, chart.ToSvg()); Assert.Equal(png, chart.ToPng()); Assert.Equal(before, captured.ToSvg()); Assert.Equal(png, captured.ToPng());
        var envelope = VisualArtifactInterchangeEnvelope.FromJson(json); var restored = envelope.ToTopologyChart();
        Assert.Null(restored.DefaultRenderOptions?.IconCatalog); Assert.Equal(TopologyIconShape.Router, restored.Nodes[0].IconShape);
        var after = restored.Prepare(); var retained = Assert.Single(after.ToInterchangeEnvelope().Nodes);
        Capture(before, png, json, "actor-custom-router-direct"); Capture(after.ToSvg(), after.ToPng(), after.ToInterchangeEnvelope().ToJson(), "actor-custom-router-restored");
        Assert.Equal((sourceNode.Id, sourceNode.X, sourceNode.Y, sourceNode.Width, sourceNode.Height), (retained.Id, retained.X, retained.Y, retained.Width, retained.Height));
        Assert.Equal(Glyph(before, "Gateway"), Glyph(after.ToSvg(), "Gateway")); Assert.Equal(png, after.ToPng());
        restored.Nodes[0].IconShape = TopologyIconShape.Badge;
        Assert.NotEqual(Glyph(before, "Gateway"), Glyph(restored.ToSvg(), "Gateway")); Assert.NotEqual(png, restored.ToPng());
        Assert.Equal(before, captured.ToSvg()); Assert.Equal(TopologyIconShape.Router, envelope.Nodes[0].Topology!.IconShape);
    }

    [Fact]
    public void CatalogArtworkIsCopiedIntoNativeAndPortableActorOwnersBeforeProducerMutation() {
        var artwork = new TopologyIconArtwork { SvgViewBox = "0 0 20 20", SvgBody = "<path d='M 2 10 L 10 2 L 18 10 L 10 18 Z' fill='#2563EB'/>" };
        var definition = new TopologyIconDefinition("tools", "mark", "Finite artwork", TopologyNodeKind.Generic, TopologyIconShape.Badge) { Artwork = artwork };
        var catalog = new TopologyIconCatalog().AddPack(new TopologyIconPack("tools", "Finite artwork").AddIcon(definition));
        var chart = new MermaidParser().ParseFlowchart("usecase-beta\nactor Mark @{icon:'tools:mark'}").Document!.ToTopologyChart(
            new MermaidFlowchartRenderOptions { Width = 500, Height = 300, FitContent = false, IconCatalog = catalog });
        Assert.NotSame(artwork, chart.Nodes[0].Artwork); var prepared = chart.Prepare(); var svg = prepared.ToSvg(); var png = prepared.ToPng();
        var envelope = VisualArtifactInterchangeEnvelope.FromJson(prepared.ToInterchangeEnvelope().ToJson()); var restored = envelope.ToTopologyChart();
        Assert.NotSame(chart.Nodes[0].Artwork, restored.Nodes[0].Artwork); Assert.Equal(artwork.SvgBody, restored.Nodes[0].Artwork!.SvgBody);
        artwork.SvgBody = "<circle cx='10' cy='10' r='2'/>"; definition.Artwork = null; catalog.RemovePack("tools");
        Assert.Equal(svg, chart.ToSvg()); Assert.Equal(png, chart.ToPng());
        chart.Nodes[0].Artwork!.SvgBody = "<rect width='20' height='20'/>";
        Assert.Equal(svg, prepared.ToSvg()); Assert.Equal(png, prepared.ToPng());
        Assert.Contains("M 2 10", restored.Nodes[0].Artwork!.SvgBody); Assert.Contains("M 2 10", envelope.Nodes[0].Topology!.Artwork!.SvgBody);
        Assert.NotEmpty(restored.ToPng());
    }

    [Fact]
    public void ExplicitGlyphWithoutIconIdOverridesKindAndCatalogShapeWhileNullRetainsInference() {
        var chart = TopologyChart.Create().WithId("native-glyph-policy").WithViewport(480, 320, 0).WithLayout(TopologyLayoutMode.Manual)
            .AddNode("router", "Native router", 100, 60, width: 170, height: 140);
        var node = chart.Nodes[0]; node.Kind = TopologyNodeKind.Database; node.IconShape = TopologyIconShape.Router;
        node.ShowStatusBadge = false; node.DisplayMode = TopologyNodeDisplayMode.Icon; node.PreserveDisplayModeSize = true; node.Color = "#2563EB";
        chart.WithRenderOptions(new TopologyRenderOptions { IncludeLegend = false });
        var direct = chart.Prepare(); var svg = direct.ToSvg(); Assert.NotEmpty(Glyph(svg, "router"));
        Assert.Equal(TopologyIconShape.Router, Assert.Single(direct.ToInterchangeEnvelope().Nodes).Topology!.IconShape);
        node.Kind = TopologyNodeKind.Generic; Assert.Equal(Glyph(svg, "router"), Glyph(chart.ToSvg(), "router"));
        node.IconShape = null; Assert.NotEqual(Glyph(svg, "router"), Glyph(chart.ToSvg(), "router"));
        node.IconShape = TopologyIconShape.Router; node.IconId = "network:firewall";
        Assert.Equal(Glyph(svg, "router"), Glyph(chart.ToSvg(), "router"));
        var restored = VisualArtifactInterchangeEnvelope.FromJson(direct.ToInterchangeEnvelope().ToJson()).ToTopologyChart();
        Assert.Null(restored.Nodes[0].IconId); Assert.Equal(TopologyIconShape.Router, restored.Nodes[0].IconShape);
        Assert.Equal(Glyph(svg, "router"), Glyph(restored.ToSvg(), "router"));
    }

    [Theory]
    [InlineData(TopologyNodeShape.ActorHollow)] [InlineData(TopologyNodeShape.ActorBusiness)]
    [InlineData(TopologyNodeShape.ActorBusinessHollow)] [InlineData(TopologyNodeShape.ActorSilhouette)] [InlineData(TopologyNodeShape.ActorIcon)]
    public void ActorVariantsUseMeasuredExternalRolesAndAttachEdgesToTheFigureRatherThanItsWideCaptionFrame(TopologyNodeShape shape) {
        var chart = TopologyChart.Create().WithId("actor-ports").WithViewport(640, 400, 0).WithLayout(TopologyLayoutMode.Manual)
            .AddNode("actor", "External participant", 50, 60, width: 220, height: 140)
            .AddNode("action", "Submit request", 390, 60, width: 180, height: 90)
            .AddEdge("submit", "actor", "action", routing: TopologyEdgeRouting.Straight);
        var actor = chart.Nodes[0]; actor.LabelContent = TextContent.FromRuns(new[] { new TextContentRun("External "), new TextContentRun("participant", TextInlineEmphasis.Bold) }); actor.Kind = TopologyNodeKind.Generic; actor.Shape = shape; actor.Subtitle = "Business participant";
        actor.SubtitleTextStyle = new TextStyleOverride { FontSize = 13, FontWeight = "500", FontFamily = BrowserProofFonts.Family };
        if (shape == TopologyNodeShape.ActorIcon) actor.IconShape = TopologyIconShape.Router;
        chart.Nodes[1].Kind = TopologyNodeKind.Generic; chart.Nodes[1].Shape = TopologyNodeShape.Ellipse;
        foreach (var node in chart.Nodes) { node.ShowStatusBadge = false; node.PreserveDisplayModeSize = true; node.DisplayMode = TopologyNodeDisplayMode.Card; }
        chart.Edges[0].SourcePort = TopologyEdgePort.Right;
        chart.WithRenderOptions(new TopologyRenderOptions { IncludeLegend = false, WrapNodeLabels = true, MaxNodeSubtitleLines = 4 });
        var prepared = chart.Prepare(); var envelope = prepared.ToInterchangeEnvelope(); var captured = envelope.Nodes[0]; var first = Assert.Single(envelope.Edges).ResolvedRoute[0];
        Capture(prepared.ToSvg(), prepared.ToPng(), envelope.ToJson(), "actor-ports-" + shape);
        if (shape == TopologyNodeShape.ActorIcon) {
            // This fixture has ActorScale=1: the native48-unit frame ends24 units right of its centre,
            // then the established7-unit endpoint gap begins the route outside the real figure.
            Assert.Equal(captured.X!.Value + captured.Width!.Value / 2 + 24 + 7, first.X, 9);
        } else Assert.InRange(first.X, captured.X!.Value + captured.Width!.Value / 2 + 10, captured.X.Value + captured.Width.Value / 2 + 30);
        Assert.True(first.X < captured.X.Value + captured.Width.Value);
        var xml = XDocument.Parse(prepared.ToSvg()); var group = MermaidUseCaseAdvancedActorTests.Node(xml, "actor");
        var subtitle = group.Descendants().Single(item => (string?)item.Attribute("data-cfx-role") == "topology-node-subtitle");
        Assert.Equal("Business participant", (string?)subtitle.Attribute("data-cfx-caption"));
        Assert.Contains(subtitle.Descendants(), item => (string?)item.Attribute("data-cfx-native-text-ink") == "true");
        NativeInlineContentTests.AssertResources(prepared.ToSvg());
        Assert.Equal(13, captured.Topology!.SubtitleTextStyle!.FontSize);
        Assert.Equal("500", captured.Topology.SubtitleTextStyle.FontWeight);
        Assert.Equal(BrowserProofFonts.Family, captured.Topology.SubtitleTextStyle.FontFamily);
        Assert.Equal(TextInlineEmphasis.Bold, captured.Topology.LabelContent!.Runs[1].Emphasis);
        var loaded = VisualArtifactInterchangeEnvelope.FromJson(envelope.ToJson()).ToTopologyChart();
        Assert.Equal(shape, loaded.Nodes[0].Shape); Assert.Equal(TopologyEdgePort.Right, loaded.Edges[0].SourcePort);
        Assert.Equal(BrowserProofFonts.Family, loaded.Nodes[0].SubtitleTextStyle!.FontFamily);
        Assert.NotEmpty(loaded.ToPng());
        var fontProbe = VisualArtifactInterchangeEnvelope.FromJson(envelope.ToJson()).ToTopologyChart();
        var oldInk = fontProbe.ToPng(); fontProbe.Nodes[0].SubtitleTextStyle!.FontSize = 19;
        Assert.NotEqual(oldInk, fontProbe.ToPng()); Assert.Equal(13, captured.Topology.SubtitleTextStyle.FontSize);
    }

    [Fact]
    public void InvalidExplicitGlyphPolicyRejectsNativeAndTypedPortableDataAndVersionOneCannotSilentlyLoseIt() {
        var chart = TopologyChart.Create().WithViewport(300, 200).AddNode("node", "Node", 30, 30);
        chart.Nodes[0].IconShape = (TopologyIconShape)999; var validation = new TopologyChartValidator().Validate(chart);
        Assert.Contains(validation.Errors, error => error.Code == "node-icon-shape");
        chart.Nodes[0].IconShape = TopologyIconShape.Router; var envelope = chart.Prepare().ToInterchangeEnvelope();
        Assert.Equal(2, envelope.Version); var json = envelope.ToJson(); Assert.Throws<NotSupportedException>(() => VisualArtifactInterchangeEnvelope.FromJson(json.Replace("\"version\":2", "\"version\":1", StringComparison.Ordinal)));
        envelope.Nodes[0].Topology!.IconShape = (TopologyIconShape)999; Assert.Throws<ArgumentOutOfRangeException>(() => envelope.Validate());
    }

    private static string Glyph(string svg, string id) => string.Join("\n", MermaidUseCaseAdvancedActorTests.Node(XDocument.Parse(svg), id).Descendants()
        .Where(item => (string?)item.Attribute("data-cfx-role") is "topology-node-icon" or "topology-icon-surface")
        .Select(item => item.ToString(SaveOptions.DisableFormatting)));
    private static void Capture(string svg, byte[] png, string json, string name) {
        var output = Environment.GetEnvironmentVariable("CFX_NATIVE_CAPTURE_DIRECTORY"); if (string.IsNullOrWhiteSpace(output)) return;
        Directory.CreateDirectory(output); var stem = Path.Combine(output, name); File.WriteAllText(stem + ".svg", svg); File.WriteAllBytes(stem + ".native.png", png); File.WriteAllText(stem + ".json", json);
    }
}
