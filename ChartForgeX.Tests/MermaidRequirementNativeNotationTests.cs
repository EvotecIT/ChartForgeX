using System.Xml.Linq;
using ChartForgeX.Markup.Mermaid;
using ChartForgeX.Mermaid;
using ChartForgeX.Raster;
using ChartForgeX.Topology;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class MermaidRequirementNativeNotationTests {
    private const string Source = """
        requirementDiagram
        direction LR
        classDef default fill:#fef9c3,stroke:#164e63,color:#172554
        classDef urgent fill:#fed7aa
        functionalRequirement authentication:::urgent { id: "AUTH-1" text: "User identity; risk: never a field" risk: High verifymethod: Test }
        element service { type: "Application"; docref: "Auth contract" }
        service - satisfies -> authentication
        style service fill:#dbeafe
        """;

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void CompactFieldsAndSharedPaintRetainVisibleSysmlFactsInPairedDirectAndFenceOutput(bool dark) {
        var source = (dark ? "%%{init: {'theme':'dark'}}%%\n" : "") + Source;
        var output = MermaidRenderer.Render(source);
        Assert.Empty(output.Diagnostics);
        var artifact = Assert.IsType<ChartForgeX.VisualArtifacts.VisualArtifact>(output.Artifact);
        var topology = Assert.IsType<TopologyChart>(artifact.Model);
        var requirement = Assert.Single(topology.Nodes, node => node.Id == "authentication");
        Assert.Equal("#fed7aa", requirement.BackgroundColor);
        Assert.Equal("#164e63", requirement.Color);
        Assert.Equal("#172554", requirement.LabelColor);
        Assert.Equal(new[] { "id", "text", "risk", "verifymethod" }, requirement.Details.Select(detail => detail.Label));
        Assert.Equal(new[] { "AUTH-1", "User identity; risk: never a field", "High", "Test" }, requirement.Details.Select(detail => detail.Value));
        Assert.Equal("functionalRequirement", requirement.Metadata["mermaid.requirementType"]);
        Assert.Equal("satisfies", Assert.Single(topology.Edges).Label);
        var fence = new MermaidVisualMarkupParser().Parse("```mermaid\n" + source + "\n```");
        Assert.False(fence.HasErrors);
        var fenced = Assert.Single(fence.Artifacts);
        var svg = artifact.ToSvg(); var png = artifact.ToPng();
        Assert.Equal(svg, fenced.ToSvg()); Assert.Equal(png, fenced.ToPng());
        var xml = XDocument.Parse(svg);
        Assert.Contains(xml.Descendants(), element => string.Equals((string?)element.Attribute("fill"), "#FED7AA", StringComparison.OrdinalIgnoreCase));
        var text = string.Join(" ", xml.Descendants().Where(element => element.Name.LocalName == "text").Select(element => element.Value));
        Assert.Contains("AUTH-1", text); Assert.Contains("High", text); Assert.Contains("Test", text);
        var image = RasterImageDecoder.Decode(png);
        Assert.True(image.Width > 0 && image.Height > 0);
        var captures = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (!string.IsNullOrWhiteSpace(captures)) {
            Directory.CreateDirectory(captures);
            var stem = Path.Combine(captures, "requirement-native-" + (dark ? "dark" : "light"));
            File.WriteAllText(stem + ".svg", svg); File.WriteAllBytes(stem + ".png", png);
        }
    }

    [Fact]
    public void ForwardRelationshipsQuotedIdentitiesAndDeferredStyleAssignmentsUseConcreteDeclaredNodes() {
        const string source = """
            requirementDiagram
            "test service" - verifies -> "user access"
            class "user access" urgent
            requirement "user access" { id: 7 risk: low verifymethod: inspection }
            element "test service" {}
            classDef urgent fill:#fed7aa
            """;
        var parsed = new MermaidParser().ParseRequirement(source);
        Assert.Empty(parsed.Diagnostics);
        Assert.Equal(new[] { "user access" }, parsed.Document!.Requirements.Select(item => item.Name));
        Assert.Equal(new[] { "test service" }, parsed.Document.Elements.Select(item => item.Name));
        var nodes = parsed.Document.ToTopologyChart().Nodes;
        Assert.Equal("#fed7aa", Assert.Single(nodes, node => node.Id == "user access").BackgroundColor);
        Assert.Equal(("test service", "user access"), (parsed.Document.Relationships[0].SourceName, parsed.Document.Relationships[0].TargetName));
    }

    [Theory]
    [InlineData("requirement req { risk: impossible }", "risk")]
    [InlineData("requirement req { verifymethod: magical }", "verifymethod")]
    [InlineData("requirement req { id: 1", "requirement")]
    [InlineData("}", "}")]
    [InlineData("direction diagonal\nrequirement req {}", "direction")]
    public void MalformedCompactNotationHasLocatedErrorsAndNoDirectOrFencedArtifact(string body, string located) {
        var source = "requirementDiagram\n  " + body;
        var direct = MermaidRenderer.Render(source);
        Assert.True(direct.HasErrors); Assert.Null(direct.Artifact);
        var error = Assert.Single(direct.Diagnostics, item => item.Severity == MermaidDiagnosticSeverity.Error && item.Span.Line == 2);
        Assert.Equal(MermaidDiagnosticCodes.InvalidStatement, error.Code);
        Assert.Equal(3 + body.IndexOf(located, StringComparison.Ordinal), error.Span.Column);
        var fence = new MermaidVisualMarkupParser().Parse("```mermaid\n" + source + "\n```");
        Assert.True(fence.HasErrors); Assert.Empty(fence.Artifacts);
    }
}
