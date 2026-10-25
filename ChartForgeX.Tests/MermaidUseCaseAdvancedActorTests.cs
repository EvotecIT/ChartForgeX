using System.Xml.Linq;
using ChartForgeX.Markup.Mermaid;
using ChartForgeX.Mermaid;
using ChartForgeX.Rendering;
using ChartForgeX.Topology;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

[Collection(nameof(FontRegistryCollection))]
public sealed class MermaidUseCaseAdvancedActorTests {
    [Theory]
    [InlineData(390, false)] [InlineData(390, true)] [InlineData(1100, false)] [InlineData(1100, true)]
    public void WholeFamilyDirectFenceAndPortableCapturesRetainDistinctNativeAnatomyAndSourceIdentity(int width, bool dark) {
        var source = UseCaseAdvancedActorFixture.Read(width == 390); var options = UseCaseAdvancedActorFixture.Options(width, dark);
        var direct = MermaidRenderer.Render(source, options); Assert.Empty(direct.Diagnostics);
        var fence = new MermaidVisualMarkupParser(options).Parse("```mermaid\n" + source + "\n```"); Assert.Empty(fence.Diagnostics);
        var artifact = Assert.IsType<VisualArtifact>(direct.Artifact); var other = Assert.Single(fence.Artifacts);
        Assert.Equal(artifact.ToSvg(), other.ToSvg()); Assert.Equal(artifact.ToPng(), other.ToPng());
        var chart = Assert.IsType<TopologyChart>(artifact.Model); var prepared = chart.Prepare();
        Assert.Equal(new[] { TopologyNodeShape.Actor, TopologyNodeShape.ActorHollow, TopologyNodeShape.ActorBusiness,
            TopologyNodeShape.ActorBusinessHollow, TopologyNodeShape.ActorSilhouette, TopologyNodeShape.ActorIcon, TopologyNodeShape.BusinessEllipse }, chart.Nodes.Select(node => node.Shape!.Value));
        var envelope = prepared.ToInterchangeEnvelope(); Assert.Equal(2, envelope.Version);
        Assert.Equal(new[] { "Plain", "Hollow", "Business", "BusinessHollow", "Silhouette", "Icon", "Submit" }, envelope.Nodes.Select(node => node.Id));
        Assert.Equal(new[] { "plain", "hollow", "business", "partner", "operator", "system" }, envelope.Edges.Select(edge => edge.Id));
        Assert.Equal("network:router", chart.Nodes[5].IconId); Assert.Equal(TopologyIconShape.Router, chart.Nodes[5].IconShape);
        Assert.Equal(source, VisualArtifactInterchangeEnvelope.FromJson(artifact.ToInterchangeJson()).Extensions["mermaid.source"]);
        AssertPaint(prepared.ToSvg());
        var visible = string.Join(" ", prepared.Visual.Scene.Nodes.OfType<VisualSceneText>().SelectMany(text => text.Text.Lines.Select(line => line.Text)));
        foreach (var word in new[] { "Plain", "Hollow", "Business", "Silhouette", "Icon", "participant", "external", "operator", "system", "Submit request", "business action" }) Assert.Contains(word, visible);
        var restored = VisualArtifactInterchangeEnvelope.FromJson(envelope.ToJson()).ToTopologyChart();
        Assert.Equal(envelope.Nodes.Select(node => (node.Id, node.X, node.Y, node.Width, node.Height, node.Topology!.Shape, node.Topology.IconShape)),
            restored.Prepare().ToInterchangeEnvelope().Nodes.Select(node => (node.Id, node.X, node.Y, node.Width, node.Height, node.Topology!.Shape, node.Topology.IconShape)));
        AssertPaint(restored.ToSvg()); Assert.NotEmpty(restored.ToPng());
        Capture(artifact, "usecase-advanced-actors-" + width + (dark ? "-dark" : "-light"));
        var svg = prepared.ToSvg(); var png = prepared.ToPng(); chart.Nodes[4].Shape = TopologyNodeShape.Actor; chart.Nodes[5].IconShape = TopologyIconShape.Badge;
        chart.Nodes[0].Subtitle = "edited"; chart.Edges.Clear(); Assert.Equal(svg, prepared.ToSvg()); Assert.Equal(png, prepared.ToPng());
    }

    [Theory]
    [InlineData("actor A @{type:hollow}", TopologyNodeShape.ActorHollow)]
    [InlineData("actor A @{business:true}", TopologyNodeShape.ActorBusiness)]
    [InlineData("actor A @{type:hollow,business:true}", TopologyNodeShape.ActorBusinessHollow)]
    [InlineData("actor A @{type:awesome}", TopologyNodeShape.ActorSilhouette)]
    [InlineData("actor A @{icon:'network:router'}", TopologyNodeShape.ActorIcon)]
    [InlineData("actor A @{icon:router}", TopologyNodeShape.ActorIcon)]
    [InlineData("actor A @{icon:''}", TopologyNodeShape.Actor)]
    [InlineData("A(Approve) @{business:true}", TopologyNodeShape.BusinessEllipse)]
    public void SupportedPinnedMetadataLowersWithoutApproximationWarnings(string body, TopologyNodeShape shape) {
        var result = MermaidRenderer.Render("usecase-beta\n" + body, new MermaidRenderOptions { WarningPolicy = MermaidWarningPolicy.Reject });
        Assert.Empty(result.Diagnostics); var chart = Assert.IsType<TopologyChart>(result.Artifact!.Model);
        Assert.Equal(shape, Assert.Single(chart.Nodes).Shape);
    }

    [Theory]
    [InlineData("actor A @{business:true,type:awesome}")]
    [InlineData("actor A @{business:true,icon:'network:router'}")]
    [InlineData("actor A @{type:hollow,icon:'network:router'}")]
    [InlineData("actor A @{icon:true}")]
    [InlineData("actor A @{icon:network:router}")]
    [InlineData("A[Approve] @{business:true}")]
    public void InvalidActorCombinationsAreLocatedErrorsBeforeNativeConversion(string body) {
        var result = MermaidRenderer.Render("usecase-beta\n" + body); Assert.True(result.HasErrors); Assert.Null(result.Artifact);
        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Severity == MermaidDiagnosticSeverity.Error && diagnostic.Span.Line == 2);
    }

    [Theory]
    [InlineData("fa:user")] [InlineData(" ")]
    public void UnknownExactIconRetainsLocatedSourceFactsAndFrameWithoutGuessedGlyphAndCannotBypassRejectPolicy(string iconId) {
        var source = "usecase-beta\nactor A @{icon:'" + iconId + "'} <<system>>";
        var result = MermaidRenderer.Render(source); Assert.False(result.HasErrors);
        Assert.Contains(result.Diagnostics, item => item.Code == MermaidDiagnosticCodes.UnsupportedStatement && item.Span.Line == 2 && item.Span.Column > 1);
        var chart = Assert.IsType<TopologyChart>(result.Artifact!.Model); Assert.Equal(iconId, chart.Nodes[0].IconId); Assert.Null(chart.Nodes[0].IconShape);
        Assert.Equal(TopologyNodeShape.ActorIcon, chart.Nodes[0].Shape);
        Assert.Contains(chart.Prepare().Visual.Diagnostics, item => item.Code == "topology.actor-icon-unresolved");
        var svg = XDocument.Parse(chart.ToSvg()); var group = Node(svg, "A"); Assert.DoesNotContain(group.Descendants(), item => (string?)item.Attribute("data-cfx-role") == "topology-node-icon");
        Assert.Equal(iconId, VisualArtifactInterchangeEnvelope.FromJson(result.Artifact!.ToInterchangeJson()).Nodes[0].Extensions["mermaid.property.icon"]);
        var strict = MermaidRenderer.Render(source, new MermaidRenderOptions { WarningPolicy = MermaidWarningPolicy.Reject }); Assert.True(strict.HasErrors); Assert.Null(strict.Artifact);
        var fenced = new MermaidVisualMarkupParser(new MermaidRenderOptions { WarningPolicy = MermaidWarningPolicy.Reject }).Parse("```mermaid\n" + source + "\n```");
        Assert.True(fenced.HasErrors); Assert.Empty(fenced.Artifacts);
    }

    [Fact]
    public void TypedMetadataKeepsRawTokensSpansOrderAndOrdinaryNullPolicyOutput() {
        var source = UseCaseAdvancedActorFixture.Read(); var parsed = new MermaidParser().ParseFlowchart(source); Assert.Empty(parsed.Diagnostics);
        var document = Assert.IsType<MermaidUseCaseDocument>(parsed.Document); var lines = source.Split('\n');
        var authored = new (string Id, string Key, string Raw, int Line)[] {
            ("Hollow", "type", "hollow", 4), ("Business", "business", "true", 5),
            ("BusinessHollow", "type", "hollow", 6), ("BusinessHollow", "business", "true", 6),
            ("Silhouette", "type", "awesome", 7), ("Icon", "icon", "\"network:router\"", 8),
            ("Submit", "business", "true", 9)
        };
        Assert.Equal(authored.Length, document.Nodes.Sum(node => node.Properties.Keys.Count(key => key is "type" or "business" or "icon")));
        var converted = document.ToTopologyChart();
        foreach (var property in authored) {
            var node = Assert.Single(document.Nodes, item => item.Id == property.Id);
            var native = Assert.Single(converted.Nodes, item => item.Id == property.Id); var prefix = "mermaid.property." + property.Key;
            var spanLine = int.Parse(native.Metadata[prefix + ".line"], System.Globalization.CultureInfo.InvariantCulture);
            var spanColumn = int.Parse(native.Metadata[prefix + ".column"], System.Globalization.CultureInfo.InvariantCulture);
            var spanLength = int.Parse(native.Metadata[prefix + ".length"], System.Globalization.CultureInfo.InvariantCulture);
            var propertySource = property.Key + ":" + property.Raw;
            Assert.Equal(property.Line, spanLine);
            Assert.Equal(lines[property.Line - 1].IndexOf(propertySource, StringComparison.Ordinal) + 1, spanColumn);
            Assert.Equal(propertySource.Length, spanLength);
            Assert.Contains(property.Raw, lines[spanLine - 1].Substring(spanColumn - 1, spanLength));
            Assert.Equal(property.Raw, native.Metadata[prefix + ".raw"]);
            Assert.Equal(property.Raw.Trim('"'), node.Properties[property.Key]);
        }
        var plain = MermaidRenderer.Render("usecase-beta\nactor A\nA --> Submit(Submit request)").Artifact!;
        var chart = Assert.IsType<TopologyChart>(plain.Model); var before = chart.Prepare(); chart.Nodes[0].IconShape = null;
        Assert.Equal(before.ToSvg(), chart.ToSvg()); Assert.Equal(before.ToPng(), chart.ToPng());
        TopologyViewportPaddingFixtureContract.RequireCapturedInsets(before.ToInterchangeEnvelope(), chart.Viewport.Padding);
    }

    internal static XElement Node(XDocument document, string id) => document.Descendants().Single(item => (string?)item.Attribute("data-cfx-role") == "topology-node" && (string?)item.Attribute("data-node-id") == id);
    internal static void AssertPaint(string svg) {
        var xml = XDocument.Parse(svg);
        XElement[] Surfaces(string id) => Node(xml, id).Descendants().Where(item => (string?)item.Attribute("data-cfx-role") == "topology-node-surface").ToArray();
        Assert.Equal(4, Surfaces("Plain").Length); Assert.Equal(2, Surfaces("Hollow").Length); Assert.Equal(5, Surfaces("Business").Length);
        Assert.Equal(3, Surfaces("BusinessHollow").Length); Assert.Equal(2, Surfaces("Silhouette").Length); Assert.Equal(2, Surfaces("Submit").Length);
        Assert.All(Surfaces("Hollow"), path => Assert.Equal("none", (string?)path.Attribute("fill")));
        Assert.All(Surfaces("Silhouette"), path => { Assert.NotEqual("none", (string?)path.Attribute("fill")); Assert.NotNull(path.Attribute("fill")); });
        Assert.Contains(Node(xml, "Icon").Descendants(), item => (string?)item.Attribute("data-cfx-role") == "topology-node-icon");
        Assert.DoesNotContain(xml.Descendants(), item => item.Name.LocalName is "script" or "image" or "foreignObject");
    }
    internal static void Capture(VisualArtifact artifact, string name) {
        var output = Environment.GetEnvironmentVariable("CFX_NATIVE_CAPTURE_DIRECTORY"); if (string.IsNullOrWhiteSpace(output)) return;
        Directory.CreateDirectory(output); var stem = Path.Combine(output, name);
        File.WriteAllText(stem + ".svg", artifact.ToSvg()); File.WriteAllBytes(stem + ".native.png", artifact.ToPng()); File.WriteAllText(stem + ".json", artifact.ToInterchangeJson());
    }
}
