using System.Xml.Linq;
using ChartForgeX.Markup.Mermaid;
using ChartForgeX.Mermaid;
using ChartForgeX.Themes;
using ChartForgeX.Topology;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class MermaidSafeInlineCaptionTests {
    public static IEnumerable<object[]> Poses() {
        foreach (var family in new[] { "flowchart", "requirement", "mindmap" }) foreach (var width in new[] { 390, 1100 })
            foreach (var dark in new[] { false, true }) yield return new object[] { family, width, dark };
    }
    [Theory]
    [MemberData(nameof(Poses))]
    public void NativeMixedCaptionDirectFencePortableAndPixelsShareContent(string family, int width, bool dark) {
        var source = Source(family, "Normal **bold** and *italic*<br>next"); var options = Options(width, dark);
        var direct = MermaidRenderer.Render(source, options); Assert.False(direct.HasErrors); Assert.Empty(direct.Diagnostics);
        var chart = Assert.IsType<TopologyChart>(direct.Artifact!.Model); var content = Content(chart, family);
        Assert.Equal("Normal bold and italic\nnext", content.PlainText);
        Assert.Contains(content.Runs, run => run.Emphasis == TextInlineEmphasis.Bold && run.Text == "bold");
        Assert.Contains(content.Runs, run => run.Emphasis == TextInlineEmphasis.Italic && run.Text == "italic");
        var fence = new MermaidVisualMarkupParser(options).Parse("```mermaid\n" + source + "\n```"); Assert.False(fence.HasErrors);
        var artifact = Assert.Single(fence.Artifacts); Assert.Equal(direct.Artifact.ToSvg(), artifact.ToSvg()); Assert.Equal(direct.Artifact.ToPng(), artifact.ToPng());
        NativeInlineContentTests.AssertResources(artifact.ToSvg()); Assert.NotEmpty(artifact.ToPng());
        var restored = VisualArtifactInterchangeEnvelope.FromJson(artifact.ToInterchangeJson()).ToTopologyChart();
        Assert.Equal(content.PlainText, Content(restored, family).PlainText); Assert.Equal(content.Runs.Select(run => run.Emphasis), Content(restored, family).Runs.Select(run => run.Emphasis));
    }
    [Theory]
    [InlineData("***both***", "both", TextInlineEmphasis.Bold | TextInlineEmphasis.Italic)]
    [InlineData("__strong__", "strong", TextInlineEmphasis.Bold)]
    [InlineData("**outer *inner* end**", "outer inner end", TextInlineEmphasis.Bold)]
    [InlineData("**line\nbreak**", "line\nbreak", TextInlineEmphasis.Bold)]
    public void SafePinnedDelimitersAndNestedEmphasisKeepLiteralSemanticText(string markdown, string plain, TextInlineEmphasis emphasis) {
        var result = MermaidRenderer.Render(Source("flowchart", markdown)); Assert.False(result.HasErrors); Assert.Empty(result.Diagnostics);
        var content = Content(Assert.IsType<TopologyChart>(result.Artifact!.Model), "flowchart"); Assert.Equal(plain, content.PlainText);
        Assert.Contains(content.Runs, run => (run.Emphasis & emphasis) == emphasis);
        if (markdown.Contains("inner", StringComparison.Ordinal)) Assert.Contains(content.Runs, run => run.Text == "inner" && run.Emphasis == (TextInlineEmphasis.Bold | TextInlineEmphasis.Italic));
    }
    [Theory]
    [InlineData("version_one_two and \\*literal\\*", "version_one_two and *literal*")]
    [InlineData("**unfinished", "**unfinished")]
    [InlineData("#42;#42;encoded#42;#42;", "**encoded**")]
    public void EscapedEncodedAndUnmatchedDelimitersRemainInert(string markdown, string plain) {
        var result = MermaidRenderer.Render(Source("flowchart", markdown)); Assert.False(result.HasErrors); Assert.Empty(result.Diagnostics);
        var content = Content(Assert.IsType<TopologyChart>(result.Artifact!.Model), "flowchart"); Assert.Equal(plain, content.PlainText);
        Assert.DoesNotContain(content.Runs, run => run.Emphasis != TextInlineEmphasis.Normal);
    }
    [Theory]
    [InlineData("flowchart", "<b>literal</b>")]
    [InlineData("requirement", "$$x^2$$")]
    [InlineData("mindmap", "![image](https://example.invalid/a.png)")]
    [InlineData("flowchart", "[link](https://example.invalid/)")]
    [InlineData("mindmap", "inner `code`")]
    public void HtmlMathCodeLinksAndImagesRemainLocatedAndStrictlyRejectable(string family, string caption) {
        var source = Source(family, caption); var result = MermaidRenderer.Render(source); Assert.False(result.HasErrors);
        var diagnostic = Assert.Single(result.Diagnostics); Assert.Equal(MermaidDiagnosticCodes.UnsupportedStatement, diagnostic.Code);
        Assert.True(diagnostic.Span.Line > 1); Assert.True(diagnostic.Span.Column > 0); Assert.True(diagnostic.Span.Length > 0);
        var xml = XDocument.Parse(result.Artifact!.ToSvg()); Assert.DoesNotContain(xml.Descendants(), node => node.Name.LocalName is "script" or "foreignObject" or "image" or "a");
        var strict = MermaidRenderer.Render(source, new MermaidRenderOptions { WarningPolicy = MermaidWarningPolicy.Reject }); Assert.Null(strict.Artifact);
        Assert.Contains(strict.Diagnostics, item => item.Code == MermaidDiagnosticCodes.RejectedWarning);
    }
    [Fact]
    public void FlowchartEdgeAndGroupMarkdownUseTheSameNativeRunsAsNodes() {
        const string source = "flowchart LR\nsubgraph cluster[\"`**Group**`\"]\nA[\"`**Node**`\"] -->|\"`*Edge*`\"| B[Plain]\nend";
        var result = MermaidRenderer.Render(source); Assert.False(result.HasErrors);
        Assert.Empty(result.Diagnostics);
        var chart = Assert.IsType<TopologyChart>(result.Artifact!.Model);
        Assert.True(Assert.Single(chart.Groups).LabelContent.IsStructured);
        Assert.True(Assert.Single(chart.Edges).LabelContent!.IsStructured);
        Assert.True(Assert.IsType<TopologyChart>(result.Artifact!.Model).Nodes.Single(node => node.Id == "A").LabelContent.IsStructured);
    }
    internal static string Source(string family, string caption) => family switch {
        "flowchart" => "flowchart LR\nA[\"`" + caption + "`\"]",
        "mindmap" => "mindmap\n  root[\"`" + caption + "`\"]",
        "requirement" => "requirementDiagram\nrequirement read {\n id: R1\n text: \"" + caption + "\"\n risk: low\n verifymethod: test\n}",
        _ => throw new ArgumentOutOfRangeException(nameof(family))
    };
    internal static TextContent Content(TopologyChart chart, string family) => family == "requirement"
        ? chart.Nodes[0].Details.Single(detail => detail.Label == "text").ValueContent : chart.Nodes[0].LabelContent;
    private static MermaidRenderOptions Options(int width, bool dark) => new() {
        Presentation = new MermaidPresentationOptions { FontFamily = BrowserProofFonts.Family, ThemeMode = dark ? VisualThemeMode.Dark : VisualThemeMode.Light },
        Flowchart = new MermaidFlowchartRenderOptions { Width = width, Height = 420 }, MindMap = new MermaidTopologyRenderOptions { Width = width, Height = 420 }, Requirement = new MermaidTopologyRenderOptions { Width = width, Height = 420 }
    };
}
