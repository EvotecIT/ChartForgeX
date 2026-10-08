using System.Xml.Linq;
using ChartForgeX.Mermaid;
using ChartForgeX.Topology;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class MermaidSemanticRenderingTests {
    [Fact]
    public void ModernNodeMetadataAndFanOutPreserveIdentitiesAndAllConnections() {
        var result = new MermaidParser().ParseFlowchart("flowchart LR\nA@{ shape: cloud, label: \"API, cloud\" } & B --> C & D --> E");
        Assert.Empty(result.Diagnostics);
        var document = result.Document!;
        Assert.Equal(new[] { "A", "B", "C", "D", "E" }, document.Nodes.Select(node => node.Id));
        Assert.Equal("API, cloud", document.Nodes[0].Text);
        Assert.Equal(new[] { ("A", "C"), ("A", "D"), ("B", "C"), ("B", "D"), ("C", "E"), ("D", "E") }, document.Edges.Select(edge => (edge.SourceId, edge.TargetId)));
        var topology = document.ToTopologyChart();
        Assert.Equal(TopologyNodeShape.Cloud, topology.Nodes[0].Shape);
        Assert.Contains("data-node-shape=\"Cloud\"", document.ToSvg());
        Assert.NotEmpty(document.ToPng());
    }

    [Fact]
    public void AccessibilityAndSourceThemeReachTheArtifactAndItsPaint() {
        const string source = "---\nconfig:\n  theme: dark\n---\nflowchart LR\naccTitle: Accessible API\naccDescr {\n  Service to database\n}\nA --> B";
        var parsed = new MermaidParser().ParseFlowchart(source);
        Assert.Empty(parsed.Diagnostics);
        var document = parsed.Document!;
        Assert.Equal(new[] { "A", "B" }, document.Nodes.Select(node => node.Id));
        Assert.Equal(10, document.Nodes[0].Span.Line);
        var artifact = document.ToVisualArtifact();
        Assert.Equal("Accessible API", artifact.Accessibility.Name);
        var svg = XDocument.Parse(artifact.ToSvg());
        Assert.Contains(svg.Descendants(), element => element.Name.LocalName == "title" && element.Value == "Accessible API");
        Assert.Contains("#0B1120", svg.ToString());
        Assert.Equal("#0B1120", Assert.IsType<TopologyChart>(artifact.Model).Theme!.Background);
    }

    [Fact]
    public void ClassMembersAndInheritanceRenderAtTheDeclaredEnd() {
        var parsed = new MermaidParser().Parse("classDiagram\nnamespace Domain {\nclass User {\n+string SentinelName\n+SentinelLogin() bool\n}\n}\n<<interface>> User\nUser <|-- Admin");
        Assert.Empty(parsed.Diagnostics);
        var document = Assert.IsType<MermaidClassDocument>(parsed.Document);
        var chart = document.ToTopologyChart();
        Assert.Equal(TopologyMarkerKind.OpenTriangle, chart.Edges[0].SourceMarker);
        Assert.Equal(TopologyMarkerKind.None, chart.Edges[0].TargetMarker);
        Assert.Equal(VisualLinkDirection.Backward, chart.Edges[0].Direction);
        Assert.Equal("Domain", chart.Nodes[0].GroupId);
        var svg = document.ToSvg();
        Assert.Contains("+string SentinelName", svg);
        Assert.Contains("+SentinelLogin() bool", svg);
        Assert.Contains("interface", svg);
        var relationship = Assert.Single(XDocument.Parse(svg).Descendants(), element => (string?)element.Attribute("data-cfx-role") == "topology-edge");
        Assert.Equal("OpenTriangle", (string?)relationship.Attribute("data-source-marker"));
        Assert.Contains(relationship.Descendants(), element => (string?)element.Attribute("data-cfx-role") == "topology-marker");
        Assert.NotEmpty(document.ToPng());
    }

    [Fact]
    public void EntityAttributesKeysAndCardinalityReachBothRenderers() {
        var parsed = new MermaidParser().Parse("erDiagram\nCUSTOMER ||--o{ ORDER : places\nCUSTOMER {\nstring SentinelKey PK\nstring SentinelEmail\n}");
        Assert.Empty(parsed.Diagnostics);
        var document = Assert.IsType<MermaidEntityRelationshipDocument>(parsed.Document);
        var chart = document.ToTopologyChart();
        Assert.Equal(TopologyMarkerKind.ExactlyOne, chart.Edges[0].SourceMarker);
        Assert.Equal(TopologyMarkerKind.ZeroOrMany, chart.Edges[0].TargetMarker);
        Assert.Contains("string SentinelKey  PK", document.ToSvg());
        Assert.Contains("string SentinelEmail", document.ToSvg());
        Assert.NotEmpty(document.ToPng());
    }

    [Theory]
    [InlineData("xychart")]
    [InlineData("xychart-beta")]
    public void XYSeriesNamesSurviveBothHeaderForms(string header) {
        var parsed = new MermaidParser().Parse(header + "\nx-axis [Jan, Feb]\nline \"Revenue\" [1, 2]");
        Assert.Empty(parsed.Diagnostics);
        var document = Assert.IsType<MermaidXYChartDocument>(parsed.Document);
        Assert.Equal("Revenue", document.Series[0].Name);
        Assert.Contains("Revenue", document.ToSvg());
    }

    [Fact]
    public void GanttExclusionsMoveDurationAndDependentTasksWithoutInventingProgress() {
        var parsed = new MermaidParser().Parse("gantt\ndateFormat YYYY-MM-DD\nexcludes weekends\nTask :active, a, 2026-01-02, 2d\nFollowup :b, after a, 1d\nExplicit :c, 2026-01-02, 2026-01-04");
        Assert.Empty(parsed.Diagnostics);
        var document = Assert.IsType<MermaidGanttDocument>(parsed.Document);
        Assert.Equal(new DateTime(2026, 1, 6), document.Tasks[0].End);
        Assert.Equal(document.Tasks[0].End, document.Tasks[1].Start);
        Assert.Equal(new DateTime(2026, 1, 7), document.Tasks[1].End);
        Assert.Equal(new DateTime(2026, 1, 4), document.Tasks[2].End);
        Assert.Equal(0, document.Tasks[0].Progress);
    }

    [Theory]
    [InlineData("A@{ shape: cloud, label: \"Cloud\"")]
    [InlineData("A & --> B")]
    [InlineData("A --> B &")]
    public void IncompleteMetadataAndNodeGroupsFailWithLocatedDiagnostics(string statement) {
        var result = new MermaidParser().ParseFlowchart("flowchart LR\n" + statement);
        Assert.True(result.HasErrors);
        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Span.Line == 2);
    }
}
