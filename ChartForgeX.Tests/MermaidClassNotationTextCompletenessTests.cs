using System.Xml.Linq;
using ChartForgeX.Markup.Mermaid;
using ChartForgeX.Mermaid;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Topology;
using ChartForgeX.Typography;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class MermaidClassNotationTextCompletenessTests {
    [Theory]
    [InlineData("class", false, 390)] [InlineData("class", true, 390)]
    [InlineData("class", false, 1100)] [InlineData("class", true, 1100)]
    [InlineData("er", false, 390)] [InlineData("er", true, 390)]
    [InlineData("er", false, 1100)] [InlineData("er", true, 1100)]
    public void EveryAuthoredRelationshipAndDetailRowSurvivesDirectFenceSvgPngAndPortableFacts(string family, bool dark, int width) {
        var options = MermaidClassErCaptionTests.Options(dark, width);
        var result = MermaidRenderer.Render(Source(family), options); Assert.Empty(result.Diagnostics);
        var artifact = result.Artifact!; var svg = artifact.ToSvg(); var png = artifact.ToPng();
        Capture("notation-complete-" + family + "-" + width + (dark ? "-dark" : "-light"), svg, png);
        var fence = new MermaidVisualMarkupParser(options).Parse("```mermaid\n" + Source(family) + "\n```");
        Assert.Empty(fence.Diagnostics); var fenced = Assert.Single(fence.Artifacts);
        Assert.Equal(svg, fenced.ToSvg()); Assert.Equal(png, fenced.ToPng());
        var xml = XDocument.Parse(svg);
        foreach (var expected in ExpectedRows(family)) Assert.Contains(xml.Descendants(), element => element.Name.LocalName == "text" && element.Value == expected);
        var edge = Assert.Single(xml.Descendants(), element => Role(element) == "topology-edge-label");
        Assert.Equal("2", (string?)edge.Attribute("data-label-line-count"));
        Assert.Equal(family == "class" ? new[] { "owns", "many" } : new[] { "holds", "rows" }, Text(edge));
        var envelope = VisualArtifactInterchangeEnvelope.FromJson(artifact.ToInterchangeJson());
        Assert.Contains(envelope.Nodes.SelectMany(node => node.Details), detail => detail.Text!.Contains('\n'));
        Assert.Equal(family == "class" ? "owns\nmany" : "holds\nrows", Assert.Single(envelope.Edges).Label);
        Assert.DoesNotContain(xml.Descendants(), element => element.Name.LocalName is "script" or "foreignObject" or "image");
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void PrimarySecondaryAndTertiaryLabelsReserveAndPaintAllSixRows(bool monitoring) {
        var chart = Chart(); chart.AddEdge("rows", "a", "b", "Primary\ncontinued");
        chart.Edges[0].SecondaryLabel = "Secondary\ncontinued two"; chart.Edges[0].TertiaryLabel = "Tertiary\ncontinued three";
        var options = new TopologyRenderOptions { IncludeLegend = false, IncludeTitle = false };
        if (monitoring) options.WithMonitoringDashboardStyle();
        var prepared = chart.Prepare(Context(), options); var svg = prepared.ToSvg();
        var edge = Assert.Single(XDocument.Parse(svg).Descendants(), element => Role(element) == "topology-edge-label");
        Assert.Equal("6", (string?)edge.Attribute("data-label-line-count"));
        Assert.Equal(new[] { "Primary", "continued", "Secondary", "continued two", "Tertiary", "continued three" }, Text(edge));
        Assert.DoesNotContain(prepared.Diagnostics, item => item.Code is "topology.label-lines" or "topology.label-height" or "topology.label-truncated");
        Capture("topology-six-authored-edge-rows-" + monitoring, svg, prepared.ToPng());
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void StyledMultilineDetailsReserveCompleteRowsInOrdinaryAndEnclosedCards(bool enclosed) {
        var chart = TopologyChart.Create().WithViewport(780, 480, 20).WithLegend(null);
        chart.Nodes.Add(new TopologyNode { Id = "a", Label = "Service", X = 40, Y = 40, Width = 220, Height = 70,
            Shape = enclosed ? TopologyNodeShape.Diamond : null, FitLabelToContent = enclosed, ShowStatusBadge = false });
        chart.Nodes[0].Details.Add(new TopologyNodeDetail { Text = "first\nsecond", TextStyle = new TextStyleOverride { Italic = true, Underline = true } });
        chart.Nodes[0].Details.Add(new TopologyNodeDetail { Text = "third\nfourth" });
        var prepared = chart.Prepare(Context(), new TopologyRenderOptions { IncludeLegend = false, IncludeTitle = false });
        var svg = prepared.ToSvg(); var xml = XDocument.Parse(svg);
        Assert.Equal(new[] { "first", "second", "third", "fourth" }, xml.Descendants().Where(element => Role(element) == "topology-node-detail").SelectMany(Text));
        Assert.DoesNotContain(prepared.Diagnostics, item => item.Code.StartsWith("topology.label-", StringComparison.Ordinal) || item.Code == "topology.shape-content-overflow");
        Assert.Equal(70, chart.Nodes[0].Height); Capture("topology-four-authored-detail-rows-" + enclosed, svg, prepared.ToPng());
    }

    [Fact]
    public void MultilineDisabledJoinsRelationshipAndDetailRowsWithoutChangingSourceFacts() {
        var chart = Chart(); chart.Nodes[0].Height = 150; chart.Nodes[0].Width = 240;
        chart.Nodes[0].Details.Add(new TopologyNodeDetail { Text = "first\r\nsecond" }); chart.AddEdge("e", "a", "b", "owns\rmany");
        var prepared = chart.Prepare(Context(), new TopologyRenderOptions { IncludeLegend = false, IncludeTitle = false, AllowMultilineNodeLabels = false });
        var xml = XDocument.Parse(prepared.ToSvg());
        Assert.Contains(xml.Descendants(), element => element.Name.LocalName == "text" && element.Value == "first second");
        Assert.Contains(xml.Descendants(), element => element.Name.LocalName == "text" && element.Value == "owns many");
        Assert.Equal("owns\rmany", chart.Edges[0].Label); Assert.Equal("first\r\nsecond", chart.Nodes[0].Details[0].Text);
    }

    [Theory]
    [InlineData("Pair~K,V~ ()-- Other", true)] [InlineData("Other --() Pair~K,V~", false)]
    public void GenericLollipopInterfacesRetainTheirAuthoredCaptionAndPortableTypeFacts(string relationship, bool source) {
        var result = MermaidRenderer.Render("classDiagram\n" + relationship); Assert.False(result.HasErrors);
        Assert.Contains(result.Diagnostics, item => item.Code == MermaidDiagnosticCodes.ApproximateMapping);
        var chart = Assert.IsType<TopologyChart>(result.Artifact!.Model);
        var node = Assert.Single(chart.Nodes, item => item.Metadata.TryGetValue("mermaid.kind", out var kind) && kind == "interface");
        Assert.Equal("Pair<K,V>", node.Label); Assert.Equal("Pair~K,V~", node.Metadata["mermaid.generic.source"]);
        Assert.Equal("Pair<K,V>", source ? chart.Edges[0].SourceLabel : chart.Edges[0].TargetLabel);
        Assert.Equal("Pair~K,V~", VisualArtifactInterchangeEnvelope.FromJson(result.Artifact.ToInterchangeJson()).Nodes.Single(item => item.Id == node.Id).Extensions["mermaid.generic.source"]);
    }

    internal static string Source(string family) => family == "class" ? """
        classDiagram
        class Box~T~["Service"] {
          +run()<br>ready
          +id<br>value
        }
        class Other
        Box~T~ --> Other : owns<br>many
        """ : """
        erDiagram
        A["Account"] {
          string id PK "external<br>key"
        }
        B["Order"] {
          int id PK "record"
        }
        A ||--o{ B : holds<br>rows
        """;
    internal static string[] ExpectedRows(string family) => family == "class" ? new[] { "+run()", "ready", "+id", "value", "owns", "many" }
        : new[] { "string id  PK  external", "key", "int id  PK  record", "holds", "rows" };
    private static string? Role(XElement element) => (string?)element.Attribute("data-cfx-role");
    private static string[] Text(XElement element) => element.Descendants().Where(item => item.Name.LocalName == "text").Select(item => item.Value).ToArray();
    private static TopologyChart Chart() {
        var chart = TopologyChart.Create().WithViewport(780, 480, 20).WithLegend(null);
        chart.Nodes.Add(new TopologyNode { Id = "a", Label = "A", X = 40, Y = 50, Width = 100, Height = 70, ShowStatusBadge = false });
        chart.Nodes.Add(new TopologyNode { Id = "b", Label = "B", X = 570, Y = 330, Width = 100, Height = 70, ShowStatusBadge = false }); return chart;
    }
    private static VisualRenderContext Context() => new(new VisualLayoutOptions(new VisualSize(780, 480), 20), frame: new VisualFrame(showLegend: false),
        font: new FontSpec { Family = "Carlito", FilePath = Path.Combine(AppContext.BaseDirectory, "Fixtures", "Fonts", "Carlito", "Carlito-Regular.ttf") });
    private static void Capture(string name, string svg, byte[] png) {
        var root = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY"); if (string.IsNullOrWhiteSpace(root)) return;
        Directory.CreateDirectory(root); File.WriteAllText(Path.Combine(root, name + ".svg"), svg); File.WriteAllBytes(Path.Combine(root, name + ".png"), png);
    }
}
