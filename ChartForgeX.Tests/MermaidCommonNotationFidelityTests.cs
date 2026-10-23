using System.Xml.Linq;
using ChartForgeX.Markup.Mermaid;
using ChartForgeX.Mermaid;
using ChartForgeX.Topology;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

[Trait("BrowserSuite", "Compatibility")]
public sealed partial class MermaidCommonNotationFidelityTests {
    [Fact]
    public void NestedClassNamespacesAndCompositeStatesUseRealNativeGroupIdentity() {
        var classes = MermaidRenderer.Render("classDiagram\nnamespace Outer {\nnamespace Inner {\nclass A\n}\n}\nA --> B");
        Assert.Empty(classes.Diagnostics);
        var classChart = Assert.IsType<TopologyChart>(classes.Artifact!.Model);
        Assert.Equal("Outer", classChart.Groups.Single(group => group.Id == "Outer.Inner").ParentGroupId);
        Assert.Equal("Outer.Inner", classChart.Nodes.Single(node => node.Id == "A").GroupId);
        Assert.NotEmpty(classes.Artifact.ToPng());

        var states = MermaidRenderer.Render("stateDiagram-v2\ndirection LR\nOutside --> Outer\nstate Outer {\ndirection TB\nstate Inner {\nstate F <<fork>>\nF --> A\nA --> B\n}\n}\nnote right of Outer : Composite note");
        Assert.Empty(states.Diagnostics);
        var stateChart = Assert.IsType<TopologyChart>(states.Artifact!.Model);
        Assert.DoesNotContain(stateChart.Nodes, node => node.Id == "Outer" || node.Id == "Inner");
        Assert.Equal("Outer", stateChart.Groups.Single(group => group.Id == "Inner").ParentGroupId);
        Assert.Equal(96, stateChart.Nodes.Single(node => node.Id == "F").Width);
        Assert.Equal(8, stateChart.Nodes.Single(node => node.Id == "F").Height);
        Assert.Equal("Outer", Assert.Single(stateChart.Edges, edge => edge.SourceNodeId == "Outside").TargetNodeId);
        Assert.NotEmpty(states.Artifact.ToSvg());
        Assert.NotEmpty(states.Artifact.ToPng());
        var envelope = VisualArtifactInterchangeEnvelope.FromJson(states.Artifact.ToInterchangeJson());
        Assert.Equal("Outer", Assert.Single(Assert.Single(envelope.Annotations).TargetIds));
    }

    [Theory]
    [InlineData("classDiagram\nclass A\nnote for A \"First line\\nSecond: A --> Phantom\"\nnote \"Floating note\"", "First line\nSecond: A --> Phantom", 2)]
    [InlineData("stateDiagram-v2\nA --> B\nnote left of A\nFirst line\nSecond: A --> Phantom\nend note\nnote right of B : Inline note", "First line\nSecond: A --> Phantom", 2)]
    public void NotesRenderThroughNativeSceneAndFenceWithoutInventingGraphFacts(string source, string text, int count) {
        var result = MermaidRenderer.Render(source);
        Assert.Empty(result.Diagnostics);
        var chart = Assert.IsType<TopologyChart>(result.Artifact!.Model);
        Assert.Equal(count, chart.Annotations.Count);
        Assert.Equal(text, chart.Annotations[0].Text);
        Assert.DoesNotContain(chart.Nodes, node => node.Id == "Phantom");
        var svg = XDocument.Parse(result.Artifact.ToSvg());
        Assert.Equal(count, svg.Descendants().Count(element => (string?)element.Attribute("data-cfx-role") == "topology-annotation"));
        Assert.Contains(svg.Descendants(), element => element.Name.LocalName == "text" && element.Value == "First line");
        Assert.Contains(svg.Descendants(), element => element.Name.LocalName == "text" && element.Value == "Second: A --> Phantom");
        Assert.NotEmpty(result.Artifact.ToPng());
        var envelope = VisualArtifactInterchangeEnvelope.FromJson(result.Artifact.ToInterchangeJson());
        Assert.Equal(chart.Nodes.Select(node => node.Id), envelope.Nodes.Select(node => node.Id));
        Assert.Equal(chart.Annotations.Select(note => note.Text), envelope.Annotations.Select(note => note.Text));
        Assert.Equal(chart.Annotations[0].TargetId, Assert.Single(envelope.Annotations[0].TargetIds));
        var fence = new MermaidVisualMarkupParser().Parse("```mermaid\n" + source + "\n```");
        Assert.False(fence.HasErrors);
        var fencedArtifact = Assert.Single(fence.Artifacts);
        Assert.Contains("topology-annotation-text", fencedArtifact.ToSvg());
    }

    [Theory]
    [InlineData("TB", 96, 8)]
    [InlineData("BT", 96, 8)]
    [InlineData("LR", 8, 96)]
    [InlineData("RL", 8, 96)]
    public void StateControlsUseCaptionlessNativeShapesAndPreserveTheirSemanticIds(string direction, double width, double height) {
        var result = MermaidRenderer.Render("stateDiagram-v2\ndirection " + direction + "\nstate F <<fork>>\nstate J <<join>>\nstate C <<choice>>\n[*] --> F\nF --> A\nF --> B\nA --> J\nB --> J\nJ --> C\nC --> Done : yes");
        Assert.Empty(result.Diagnostics);
        var chart = Assert.IsType<TopologyChart>(result.Artifact!.Model);
        foreach (var id in new[] { "F", "J" }) {
            var bar = chart.Nodes.Single(node => node.Id == id);
            Assert.Equal(TopologyNodeShape.SynchronizationBar, bar.Shape);
            Assert.Equal(width, bar.Width);
            Assert.Equal(height, bar.Height);
            Assert.False(bar.ShowLabel);
        }
        var choice = chart.Nodes.Single(node => node.Id == "C");
        Assert.Equal(TopologyNodeShape.Diamond, choice.Shape);
        Assert.Equal(30, choice.Width);
        Assert.False(choice.ShowLabel);
        var svg = XDocument.Parse(result.Artifact.ToSvg());
        foreach (var id in new[] { "F", "J", "C" }) {
            var group = Assert.Single(svg.Descendants(), element => (string?)element.Attribute("data-node-id") == id && (string?)element.Attribute("data-cfx-role") == "topology-node");
            Assert.DoesNotContain(group.Descendants(), element => element.Name.LocalName == "text");
        }
        Assert.NotEmpty(result.Artifact.ToPng());
        var envelope = VisualArtifactInterchangeEnvelope.FromJson(result.Artifact.ToInterchangeJson());
        Assert.False(envelope.Nodes.Single(node => node.Id == "C").Topology!.ShowLabel);
    }

    [Theory]
    [InlineData("<|--|>", TopologyMarkerKind.OpenTriangle, TopologyMarkerKind.OpenTriangle)]
    [InlineData("o--*", TopologyMarkerKind.OpenDiamond, TopologyMarkerKind.Diamond)]
    [InlineData("<..>", TopologyMarkerKind.Arrow, TopologyMarkerKind.Arrow)]
    public void TwoWayClassRelationsKeepBothMarkersAndStableEndpoints(string connector, TopologyMarkerKind sourceMarker, TopologyMarkerKind targetMarker) {
        var result = MermaidRenderer.Render("classDiagram\nA \"1\" " + connector + " \"many\" B : connects");
        Assert.Empty(result.Diagnostics);
        var chart = Assert.IsType<TopologyChart>(result.Artifact!.Model);
        Assert.Equal(new[] { "A", "B" }, chart.Nodes.Select(node => node.Id));
        var edge = Assert.Single(chart.Edges);
        Assert.Equal(sourceMarker, edge.SourceMarker);
        Assert.Equal(targetMarker, edge.TargetMarker);
        Assert.Equal(VisualLinkDirection.Bidirectional, edge.Direction);
        Assert.Equal(connector.Contains("..", StringComparison.Ordinal) ? TopologyEdgeLineStyle.Dashed : TopologyEdgeLineStyle.Solid, edge.LineStyle);
        Assert.Equal("1", edge.SourceLabel);
        Assert.Equal("many", edge.TargetLabel);
        Assert.NotEmpty(result.Artifact.ToPng());
    }

    [Fact]
    public void GenericClassDeclarationsKeepOneStableIdentityWithinTheirNamespace() {
        var result = MermaidRenderer.Render("classDiagram\nnamespace Domain {\nclass Store~Item~ {\n+List~List~int~~ values\n+get() List~Item~\n}\n}\nStore --> Other\nnote for Store \"Generic owner\"");
        Assert.Empty(result.Diagnostics);
        var document = Assert.IsType<MermaidClassDocument>(result.Document);
        var store = document.Classes.Single(node => node.Id == "Store");
        Assert.Equal("Store<Item>", store.Label);
        Assert.Equal("Domain", store.Namespace);
        var chart = Assert.IsType<TopologyChart>(result.Artifact!.Model);
        Assert.Equal("+List<List<int>> values", chart.Nodes[0].Details[0].Text);
        Assert.Equal("Store", Assert.Single(chart.Edges).SourceNodeId);
        Assert.Equal("Store", Assert.Single(chart.Annotations).TargetId);
    }

    [Fact]
    public void ErAliasesReplaceVisibleNamesWithoutChangingRelationshipsAndAttributes() {
        var result = MermaidRenderer.Render("erDiagram\nC ||--o{ O : \"places order\"\nC[\"Customer account\"] {\nstring id PK, FK \"External customer key\"\nstring? display_name\n}\nO[Orders]");
        Assert.Empty(result.Diagnostics);
        var document = Assert.IsType<MermaidEntityRelationshipDocument>(result.Document);
        Assert.Equal(new[] { "C", "O" }, document.Entities.Select(node => node.Id));
        Assert.Equal(new[] { "Customer account", "Orders" }, document.Entities.Select(node => node.Label));
        var chart = Assert.IsType<TopologyChart>(result.Artifact!.Model);
        Assert.Equal("Customer account", chart.Nodes[0].Label);
        Assert.Equal("string id  PK, FK  External customer key", chart.Nodes[0].Details[0].Text);
        Assert.Equal("string? display_name", chart.Nodes[0].Details[1].Text);
        Assert.Equal("C", Assert.Single(chart.Edges).SourceNodeId);
        Assert.Equal("places order", chart.Edges[0].Label);
        Assert.Contains("Customer account", result.Artifact.ToSvg());
        Assert.NotEmpty(result.Artifact.ToPng());
    }

    [Theory]
    [InlineData("C[Customer account]")]
    [InlineData("C[\"Customer account\"] ||--o{ O : places")]
    public void ErAliasesRequireTheDocumentedDeclarationForm(string statement) {
        var result = MermaidRenderer.Render("erDiagram\n" + statement);
        Assert.True(result.HasErrors);
        Assert.Null(result.Artifact);
        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == MermaidDiagnosticCodes.InvalidStatement && diagnostic.Span.Line == 2);
    }

    [Theory]
    [InlineData("classDiagram\nclass A\nnote for Missing \"Attached note\"")]
    [InlineData("stateDiagram-v2\nA --> B\nnote left of Missing : Attached note")]
    public void MissingNoteAttachmentsProduceLocatedErrors(string source) {
        var result = MermaidRenderer.Render(source);
        Assert.True(result.HasErrors);
        Assert.Null(result.Artifact);
        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == MermaidDiagnosticCodes.InvalidStatement && diagnostic.Span.Line == 3);
    }

    [Theory]
    [InlineData("Interface ()-- A")]
    [InlineData("A --() Interface")]
    public void LollipopsUseDistinctNativeInterfacesWithoutCreatingCounterfeitClasses(string relationship) {
        var result = MermaidRenderer.Render("classDiagram\nclass A\n" + relationship);
        Assert.False(result.HasErrors);
        Assert.Empty(result.Diagnostics);
        Assert.Equal("A", Assert.Single(Assert.IsType<MermaidClassDocument>(result.Document).Classes).Id);
        var chart = Assert.IsType<TopologyChart>(result.Artifact!.Model);
        var endpoint = Assert.Single(chart.Nodes, node => node.Metadata.TryGetValue("mermaid.kind", out var kind) && kind == "interface");
        Assert.Equal(TopologyNodeShape.Circle, endpoint.Shape);
        Assert.False(endpoint.ShowLabel);
        Assert.Contains(endpoint.Id, new[] { chart.Edges[0].SourceNodeId, chart.Edges[0].TargetNodeId });
        Assert.Empty(result.Document!.RawStatements);
        Assert.NotEmpty(result.Artifact.ToPng());
    }
}
