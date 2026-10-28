using System.Text.Json;
using ChartForgeX.Markup.Mermaid;
using ChartForgeX.Mermaid;
using ChartForgeX.Topology;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class MermaidFlowchartLabelDepthTests {
    [Fact]
    public void MultilineAndMarkdownLabelsKeepContentLocationsAndNativeNodeRuns() {
        var source = File.ReadAllText(Fixture("flowchart-multiline-markdown.mmd"));
        using var expected = JsonDocument.Parse(File.ReadAllText(Fixture("flowchart-multiline-markdown.expected.json")));
        var direct = MermaidRenderer.Render(source, BrowserProofFonts.MermaidOptions());
        Assert.False(direct.HasErrors);
        var document = Assert.IsType<MermaidFlowchartDocument>(direct.Document);
        Assert.Equal(new[] { 2, 3, 4, 5 }, document.Nodes.Select(node => node.Span.Line));
        Assert.Empty(direct.Diagnostics);
        Assert.True(direct.Artifact != null);
        var chart = Assert.IsType<TopologyChart>(direct.Artifact!.Model);
        foreach (var node in chart.Nodes) {
            Assert.Equal(expected.RootElement.GetProperty("displayLabels").GetProperty(node.Id).GetString(), node.Label);
            Assert.Equal(document.Nodes.Single(item => item.Id == node.Id).Text, node.Metadata["mermaid.label"]);
            Assert.Equal(expected.RootElement.GetProperty("nodeLabels").GetProperty(node.Id).GetString(),
                document.Nodes.Single(item => item.Id == node.Id).Text!.Trim('`'));
        }
        Assert.Equal(expected.RootElement.GetProperty("displayEdgeLabels").EnumerateArray().Select(value => value.GetString()), chart.Edges.Select(edge => edge.Label ?? ""));
        Assert.Equal("`**Review** the\n*request*`", document.Nodes[0].Text);
        Assert.Contains(chart.Nodes[0].LabelContent.Runs, run => run.Text == "Review" && run.Emphasis == TextInlineEmphasis.Bold);
        Assert.Contains(chart.Nodes[0].LabelContent.Runs, run => run.Text == "request" && run.Emphasis == TextInlineEmphasis.Italic);
        var fenced = new MermaidVisualMarkupParser(BrowserProofFonts.MermaidOptions()).Parse("Intro\n\n```mermaid\n" + source + "\n```");
        Assert.False(fenced.HasErrors);
        Assert.Equal(direct.Diagnostics.Select(d => d.Span.Line + 3), fenced.Diagnostics.Select(d => d.Line));
        var artifact = Assert.Single(fenced.Artifacts);
        Assert.Equal(direct.Artifact.ToSvg(), artifact.ToSvg());
        Assert.Equal(direct.Artifact.ToPng(), artifact.ToPng());
        var envelope = VisualArtifactInterchangeEnvelope.FromJson(artifact.ToInterchangeJson());
        Assert.Equal(chart.Nodes.Select(node => node.Label), envelope.Nodes.Select(node => node.Label));
    }

    [Theory]
    [InlineData("Ready #amp; done #35;", "Ready & done #", false)]
    [InlineData("&lt;br&gt; #lt;script#gt;", "<br> <script>", false)]
    [InlineData("<b>Plain</b>", "<b>Plain</b>", true)]
    [InlineData("$$x^2$$", "$$x^2$$", true)]
    public void EntityEscapesAreInertTextAndUnsupportedMarkupIsDiagnosed(string label, string displayed, bool warning) {
        var result = MermaidRenderer.Render("flowchart LR\nA[\"" + label + "\"] --> B");
        Assert.False(result.HasErrors);
        Assert.Equal(displayed, Assert.IsType<TopologyChart>(result.Artifact!.Model).Nodes[0].Label);
        Assert.Equal(warning ? 1 : 0, result.Diagnostics.Count);
        if (warning) Assert.Equal(MermaidDiagnosticCodes.UnsupportedStatement, result.Diagnostics[0].Code);
        Assert.DoesNotContain("<script>", result.Artifact.ToSvg(), StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("`Read **this** and *that*`", "Read this and that")]
    [InlineData("`version_one_two and \\*literal\\*`", "version_one_two and *literal*")]
    [InlineData("`**unfinished`", "**unfinished")]
    public void NativeMarkdownKeepsEscapesAndUnmatchedDelimiters(string label, string displayed) {
        var result = MermaidRenderer.Render("flowchart LR\nA[\"" + label + "\"]");
        Assert.Equal(displayed, Assert.IsType<TopologyChart>(result.Artifact!.Model).Nodes[0].Label);
        Assert.Empty(result.Diagnostics);
    }

    [Theory]
    [InlineData("  A[\"`[Review](x)`\"]", "`[Review](x)`", MermaidDiagnosticCodes.UnsupportedStatement, 3, 1)]
    [InlineData("  A[\"<b>Review</b>\"]", "<b>Review</b>", MermaidDiagnosticCodes.UnsupportedStatement, 3, 1)]
    [InlineData("  A[\"$$x^2$$\"]", "$$x^2$$", MermaidDiagnosticCodes.UnsupportedStatement, 3, 1)]
    [InlineData("  A@{ label: \"`[Review](x)`\" }", "`[Review](x)`", MermaidDiagnosticCodes.UnsupportedStatement, 14, 15)]
    [InlineData("  A@{ label: \"<b>Review</b>\" }", "<b>Review</b>", MermaidDiagnosticCodes.UnsupportedStatement, 14, 15)]
    [InlineData("  A@{ label: \"$$x^2$$\" }", "$$x^2$$", MermaidDiagnosticCodes.UnsupportedStatement, 14, 9)]
    public void ReplacedLabelDiagnosticsLocateTheDeclarationWithoutMovingTheOriginalNodeReference(
        string declaration, string label, string code, int column, int length) {
        var source = "flowchart LR\n  A --> B\n" + declaration + "\n  A --> C";
        var parsed = new MermaidParser().Parse(source);
        var rendered = MermaidRenderer.Render(source, BrowserProofFonts.MermaidOptions());
        foreach (var result in new[] { (parsed.Document, parsed.Diagnostics, parsed.HasErrors), (rendered.Document, rendered.Diagnostics, rendered.HasErrors) }) {
            Assert.False(result.HasErrors);
            var document = Assert.IsType<MermaidFlowchartDocument>(result.Document);
            var node = document.Nodes.Single(item => item.Id == "A");
            Assert.Equal(label, node.Text);
            Assert.Equal((2, 3, 1), (node.Span.Line, node.Span.Column, node.Span.Length));
            var diagnostic = Assert.Single(result.Diagnostics);
            Assert.Equal(code, diagnostic.Code);
            Assert.Equal((3, column, length), (diagnostic.Span.Line, diagnostic.Span.Column, diagnostic.Span.Length));
        }

        var fenced = new MermaidVisualMarkupParser(BrowserProofFonts.MermaidOptions()).Parse("Intro\n\n```mermaid\n" + source + "\n```");
        Assert.False(fenced.HasErrors);
        Assert.Single(fenced.Artifacts);
        var mapped = Assert.Single(fenced.Diagnostics);
        Assert.Equal(code, mapped.Code);
        Assert.Equal(6, mapped.Line);
    }

    [Fact]
    public void RepeatedMultilineMetadataLabelsUseTheFinalScalarLocationInDirectAndFencedDiagnostics() {
        const string source = "flowchart LR\nA[\"`**First**`\"] --> B\nA@{ label: \"`**Second**`\" }\nA[\"Old\ncaption\"]@{ label: \"`**Final**\ncaption`\" }\nA --> C";
        var direct = MermaidRenderer.Render(source, BrowserProofFonts.MermaidOptions());
        Assert.False(direct.HasErrors);
        var node = Assert.IsType<MermaidFlowchartDocument>(direct.Document).Nodes.Single(item => item.Id == "A");
        Assert.Equal("`**Final**\ncaption`", node.Text);
        Assert.Equal(2, node.Span.Line);
        Assert.Empty(direct.Diagnostics);
        Assert.Equal("Final\ncaption", Assert.IsType<TopologyChart>(direct.Artifact!.Model).Nodes.Single(item => item.Id == "A").LabelContent.PlainText);
        var fenced = new MermaidVisualMarkupParser(BrowserProofFonts.MermaidOptions()).Parse("Intro\n```mermaid\n" + source + "\n```");
        Assert.False(fenced.HasErrors);
        Assert.Empty(fenced.Diagnostics);
        Assert.Equal(direct.Artifact!.ToSvg(), Assert.Single(fenced.Artifacts).ToSvg());
        // Check the final scalar location through public diagnostics, without exposing parser internals.
        var locatedSource = source.Replace("**Final**", "<b>Final</b>", StringComparison.Ordinal);
        var located = MermaidRenderer.Render(locatedSource);
        var diagnostic = Assert.Single(located.Diagnostics);
        Assert.Equal(MermaidDiagnosticCodes.UnsupportedStatement, diagnostic.Code);
        Assert.Equal((5, 20, 24), (diagnostic.Span.Line, diagnostic.Span.Column, diagnostic.Span.Length));
        var locatedFence = new MermaidVisualMarkupParser(BrowserProofFonts.MermaidOptions()).Parse("Intro\n```mermaid\n" + locatedSource + "\n```");
        var mapped = Assert.Single(locatedFence.Diagnostics);
        Assert.Equal(diagnostic.Code, mapped.Code); Assert.Equal((7, 20), (mapped.Line, mapped.Column));
    }

    [Fact]
    public void TrailingErrorsAfterMultilineCaptionsKeepTheirPhysicalLineAndColumn() {
        const string source = "---\nconfig:\n  theme: dark\n---\nflowchart LR\nA[\"First\nsecond\"] --> B:::";
        var direct = MermaidRenderer.Render(source, BrowserProofFonts.MermaidOptions());
        Assert.Null(direct.Artifact);
        var error = Assert.Single(direct.Diagnostics);
        Assert.Equal((7, 15, 3), (error.Span.Line, error.Span.Column, error.Span.Length));
        var fenced = new MermaidVisualMarkupParser(BrowserProofFonts.MermaidOptions()).Parse("Intro\n```mermaid\n" + source + "\n```");
        Assert.Empty(fenced.Artifacts);
        Assert.Equal(9, Assert.Single(fenced.Diagnostics).Line);
    }

    [Fact]
    public void LaterLinkStyleDeclarationsReplacePropertiesRatherThanMergeThem() {
        var result = MermaidRenderer.Render(File.ReadAllText(Fixture("flowchart-link-style-replacement.mmd")));
        Assert.False(result.HasErrors);
        var document = Assert.IsType<MermaidFlowchartDocument>(result.Document);
        Assert.All(document.Edges, edge => Assert.False(edge.Styles.ContainsKey("stroke")));
        Assert.Equal(new[] { "0", "5 5" }, document.Edges.Select(edge => edge.Styles["stroke-dasharray"]));
        var chart = Assert.IsType<TopologyChart>(result.Artifact!.Model);
        Assert.Equal(new[] { TopologyEdgeLineStyle.Solid, TopologyEdgeLineStyle.Dashed }, chart.Edges.Select(edge => edge.LineStyle));
    }

    internal static string Fixture(string name) => Path.Combine(TestRepository.Root, "tests", "mermaid-conformance", "fixtures", name);
}
