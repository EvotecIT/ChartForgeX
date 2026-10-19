using System.Text.Json;
using System.Xml.Linq;
using ChartForgeX.Markup.Mermaid;
using ChartForgeX.Mermaid;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;
using ChartForgeX.Topology;
using ChartForgeX.VisualArtifacts;
using Microsoft.Playwright;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class MermaidFlowchartStyleTests {
    [Theory]
    [InlineData("color", "-->")]
    [InlineData("box", "--x")]
    [InlineData("hello", "--o")]
    [InlineData("external-box", "-->")]
    [InlineData("read-only", "-->")]
    [InlineData("external-xml", "--x")]
    [InlineData("mode-one", "-.->")]
    [InlineData("prefix-xray", "==>")]
    public void InlineClassesRetainTheirIdentifierBeforeACompactLink(string className, string link) {
        var result = MermaidRenderer.Render("flowchart LR\nclassDef " + className + " fill:#fed7aa\nA:::" + className + link + "B");
        Assert.Empty(result.Diagnostics);
        var document = Assert.IsType<MermaidFlowchartDocument>(result.Document);
        Assert.Equal(className, Assert.Single(document.Nodes[0].Classes));
        var edge = Assert.Single(document.Edges);
        Assert.Equal(("A", "B", link), (edge.SourceId, edge.TargetId, edge.Operator));
        Assert.Equal("#fed7aa", Assert.IsType<TopologyChart>(result.Artifact!.Model).Nodes[0].BackgroundColor);
    }

    [Fact]
    public void VersionedStylesPreserveDefaultClassRedefinitionAndDirectPrecedence() {
        var source = File.ReadAllText(Fixture("flowchart-class-styles.mmd"));
        using var expected = JsonDocument.Parse(File.ReadAllText(Fixture("flowchart-class-styles.expected.json")));
        var result = new MermaidParser().ParseFlowchart(source);
        Assert.Empty(result.Diagnostics);
        var styles = expected.RootElement.GetProperty("flowchartStyles");
        foreach (var node in result.Document!.Nodes) {
            Assert.Equal(styles.GetProperty("nodeClasses").GetProperty(node.Id).EnumerateArray().Select(item => item.GetString()), node.Classes);
            AssertStyles(styles.GetProperty("nodeStyles").GetProperty(node.Id), node.Styles);
        }
        for (var index = 0; index < result.Document.Edges.Count; index++)
            AssertStyles(styles.GetProperty("edgeStyles")[index], result.Document.Edges[index].Styles);
    }

    [Fact]
    public void InlineCommaIsOneClassWhileClassStatementsAssignSeveralClasses() {
        var result = new MermaidParser().ParseFlowchart("flowchart LR\nA:::color,box --> B\nclass B color,box");
        Assert.Empty(result.Diagnostics);
        Assert.Equal(new[] { "color,box" }, result.Document!.Nodes[0].Classes);
        Assert.Equal(new[] { "color", "box" }, result.Document.Nodes[1].Classes);
    }

    [Theory]
    [InlineData("class A color")]
    [InlineData("A:::color")]
    public void RepeatedClassesPreserveTheLatestAssignmentPrecedence(string lastAssignment) {
        var result = MermaidRenderer.Render("flowchart LR\nclassDef color fill:#ff0000\nclassDef box fill:#00ff00\nA:::color --> B\nclass A box\n" + lastAssignment);
        Assert.Empty(result.Diagnostics);
        var document = Assert.IsType<MermaidFlowchartDocument>(result.Document);
        Assert.Equal(new[] { "color", "box", "color" }, document.Nodes[0].Classes);
        var artifact = Assert.IsType<VisualArtifact>(result.Artifact);
        Assert.Equal("#ff0000", Assert.IsType<TopologyChart>(artifact.Model).Nodes[0].BackgroundColor);
    }

    [Theory]
    [InlineData("style A font-weight:bold", "font-weight")]
    [InlineData("style A fill:hsl(120,100%,50%)", "fill")]
    [InlineData("classDef color stroke-width:4px\nclass A color", "stroke-width")]
    [InlineData("linkStyle 0 color:#123456", "color")]
    public void UnsupportedStylingIsLocatedAndRetainedWithoutBreakingExports(string statement, string property) {
        var result = MermaidRenderer.Render("flowchart LR\nA --> B\n" + statement);
        Assert.False(result.HasErrors);
        var warning = Assert.Single(result.Diagnostics);
        Assert.Equal(MermaidDiagnosticCodes.UnsupportedStatement, warning.Code);
        Assert.Equal(MermaidDiagnosticSeverity.Warning, warning.Severity);
        Assert.Equal(3, warning.Span.Line);
        Assert.Contains(property, warning.Message);
        var document = Assert.IsType<MermaidFlowchartDocument>(result.Document);
        var retained = statement.StartsWith("linkStyle", StringComparison.Ordinal) ? document.Edges[0].Styles : document.Nodes[0].Styles;
        Assert.True(retained.ContainsKey(property));
        var artifact = Assert.IsType<VisualArtifact>(result.Artifact);
        Assert.NotEmpty(artifact.ToSvg());
        Assert.NotEmpty(artifact.ToPng());
    }

    [Theory]
    [InlineData("none", false)]
    [InlineData("0", false)]
    [InlineData("5 5", true)]
    public void DashPatternsUseTheNativeLineStyleAndExposeApproximation(string value, bool dashed) {
        var result = MermaidRenderer.Render("flowchart LR\nA -.-> B\nlinkStyle 0 stroke-dasharray:" + value);
        Assert.False(result.HasErrors);
        Assert.Equal(dashed ? TopologyEdgeLineStyle.Dashed : TopologyEdgeLineStyle.Solid,
            Assert.Single(Assert.IsType<TopologyChart>(result.Artifact!.Model).Edges).LineStyle);
        Assert.Equal(dashed ? 1 : 0, result.Diagnostics.Count);
        if (dashed) Assert.Equal(MermaidDiagnosticCodes.UnsupportedStatement, result.Diagnostics[0].Code);
    }

    [Theory]
    [InlineData("flowchart LR\nA::: --> B")]
    [InlineData("flowchart LR\nA:::color:::box --> B")]
    [InlineData("flowchart LR\nA --> B\nlinkStyle 2 stroke:#ff0000")]
    public void InvalidClassOrStyleSelectorsDoNotProduceDirectOrFenceArtifacts(string source) {
        var direct = MermaidRenderer.Render(source);
        Assert.True(direct.HasErrors);
        Assert.Null(direct.Artifact);
        Assert.Contains(direct.Diagnostics, item => item.Code == MermaidDiagnosticCodes.InvalidStatement);
        var fenced = new MermaidVisualMarkupParser().Parse("```mermaid\n" + source + "\n```");
        Assert.True(fenced.HasErrors);
        Assert.Empty(fenced.Artifacts);
    }

    [Theory]
    [InlineData("A --> B\nlinkStyle 1 stroke:#f00\nB --> C", 3)]
    [InlineData("A --> B\nlinkStyle 0,1 stroke:#f00\nB --> C", 3)]
    [InlineData("A --> B\nlinkStyle -1 stroke:#f00", 3)]
    [InlineData("A --> B\nlinkStyle 0,-1 stroke:#f00", 3)]
    [InlineData("A --> B\nlinkStyle 0,,0 stroke:#f00", 3)]
    public void LinkStyleIndicesMustExistWhenTheirStatementIsParsed(string body, int line) {
        var source = "flowchart LR\n" + body;
        var direct = MermaidRenderer.Render(source);
        Assert.True(direct.HasErrors);
        Assert.Null(direct.Artifact);
        var error = Assert.Single(direct.Diagnostics);
        Assert.Equal(MermaidDiagnosticCodes.InvalidStatement, error.Code);
        Assert.Equal(line, error.Span.Line);
        Assert.Equal(1, error.Span.Column);

        var fenced = new MermaidVisualMarkupParser().Parse("Intro\n\n```mermaid\n" + source + "\n```");
        Assert.True(fenced.HasErrors);
        Assert.Empty(fenced.Artifacts);
        var fenceError = Assert.Single(fenced.Diagnostics);
        Assert.Equal(error.Code, fenceError.Code);
        Assert.Equal(line + 3, fenceError.Line);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("0,0")]
    [InlineData("0,1")]
    public void DefaultLinkStylesReachFutureEdgesWhileExistingIndicesKeepTheirOverride(string selectors) {
        var source = "flowchart LR\nlinkStyle default stroke:#f00\nA --> B\nB --> C\nlinkStyle " + selectors + " stroke:#0f0\nC --> D";
        var direct = MermaidRenderer.Render(source);
        Assert.Empty(direct.Diagnostics);
        var colors = new[] { "#0f0", selectors.Contains('1') ? "#0f0" : "#f00", "#f00" };
        var chart = Assert.IsType<TopologyChart>(Assert.IsType<VisualArtifact>(direct.Artifact).Model);
        Assert.Equal(colors, chart.Edges.Select(edge => edge.Color));
        var fenced = new MermaidVisualMarkupParser().Parse("```mermaid\n" + source + "\n```");
        Assert.False(fenced.HasErrors);
        Assert.Equal(colors, Assert.IsType<TopologyChart>(Assert.Single(fenced.Artifacts).Model).Edges.Select(edge => edge.Color));
    }

    [Trait("Category", "Browser")]
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ResolvedClassPaintReachesDirectAndFenceSvgAndNativePng(bool dark) {
        var source = (dark ? "%%{init: {'theme':'dark'}}%%\n" : "") + File.ReadAllText(Fixture("flowchart-class-styles.mmd"));
        var direct = MermaidRenderer.Render(source);
        var fenced = new MermaidVisualMarkupParser().Parse("```mermaid\n" + source + "\n```");
        Assert.Empty(direct.Diagnostics);
        Assert.False(fenced.HasErrors);
        var directArtifact = Assert.IsType<VisualArtifact>(direct.Artifact);
        foreach (var artifact in new[] { directArtifact, Assert.Single(fenced.Artifacts) }) {
            var svg = XDocument.Parse(artifact.ToSvg());
            var image = RasterImageDecoder.Decode(artifact.ToPng());
            foreach (var fill in new[] { "#DBEAFE", "#FEF9C3", "#FED7AA" }) {
                Assert.Contains(svg.Descendants(), element => (string?)element.Attribute("fill") == fill);
                var color = ChartColor.Parse(fill);
                var matches = 0;
                for (var index = 0; index < image.Pixels.Length; index += 4)
                    if (image.Pixels[index] == color.R && image.Pixels[index + 1] == color.G && image.Pixels[index + 2] == color.B) matches++;
                Assert.True(matches > 20, fill + " must paint visible native node interiors.");
            }
        }
        if (!InteractiveChartBrowser.Enabled) return;
        await using var session = await InteractiveChartBrowser.OpenAsync("<!doctype html><html><body style='margin:0'>" + directArtifact.ToSvg() + "</body></html>");
        InteractiveChartBrowser.AssertNoConsoleErrors(session);
        var captures = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (string.IsNullOrWhiteSpace(captures)) return;
        Directory.CreateDirectory(captures);
        var name = Path.Combine(captures, dark ? "flowchart-classes-dark" : "flowchart-classes-light");
        File.WriteAllText(name + ".svg", directArtifact.ToSvg());
        File.WriteAllBytes(name + ".native.png", directArtifact.ToPng());
        await session.Page.ScreenshotAsync(new PageScreenshotOptions { Path = name + ".browser.png", FullPage = true });
    }

    private static void AssertStyles(JsonElement expected, Dictionary<string, string> actual) {
        Assert.Equal(expected.EnumerateObject().Count(), actual.Count);
        foreach (var property in expected.EnumerateObject()) Assert.Equal(property.Value.GetString(), actual[property.Name]);
    }

    private static string Fixture(string name) => Path.Combine(TestRepository.Root, "tests", "mermaid-conformance", "fixtures", name);
}
