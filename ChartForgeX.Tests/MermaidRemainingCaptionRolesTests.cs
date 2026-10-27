using System.Xml.Linq;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Markup.Mermaid;
using ChartForgeX.Mermaid;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using ChartForgeX.Topology;
using ChartForgeX.Typography;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class MermaidRemainingCaptionRolesTests {
    public static IEnumerable<object[]> Poses() {
        foreach (var family in new[] { "class", "er", "flowchart", "requirement" })
            foreach (var width in new[] { 390, 1100 }) foreach (var dark in new[] { false, true }) yield return new object[] { family, width, dark };
    }
    [Theory]
    [MemberData(nameof(Poses))]
    public void SourceFenceNativeInkAndPortableSnapshotRetainEveryNamedCaptionRole(string family, int width, bool dark) {
        var source = MermaidRemainingCaptionRolesExample.Source(family); var options = Options(width, dark);
        var result = MermaidRenderer.Render(source, options); Assert.False(result.HasErrors); Assert.Empty(result.Diagnostics);
        var chart = Assert.IsType<TopologyChart>(result.Artifact!.Model);
        var expected = Contents(chart).Where(content => content.IsStructured).ToArray(); Assert.NotEmpty(expected);
        Assert.Contains(expected.SelectMany(content => content.Runs), run => run.Emphasis == TextInlineEmphasis.Bold);
        if (family != "flowchart") Assert.Contains(expected.SelectMany(content => content.Runs), run => run.Emphasis == TextInlineEmphasis.Italic);
        if (family is "class" or "er" or "flowchart") {
            Assert.True(Assert.Single(chart.Groups).LabelContent.IsStructured);
            Assert.True(Assert.Single(chart.Edges).LabelContent!.IsStructured);
        } else {
            Assert.Equal("satisfies", Assert.Single(chart.Edges).Label);
            Assert.False(Assert.Single(chart.Edges).LabelContent?.IsStructured ?? false);
            Assert.Equal("Review **carefully**", chart.Nodes[0].Id);
        }
        var fence = new MermaidVisualMarkupParser(options).Parse("```mermaid\n" + source + "\n```"); Assert.False(fence.HasErrors);
        var fenced = Assert.Single(fence.Artifacts); var svg = result.Artifact.ToSvg(); var png = result.Artifact.ToPng();
        Assert.Equal(svg, fenced.ToSvg()); Assert.Equal(png, fenced.ToPng()); NativeInlineContentTests.AssertResources(svg);
        var xml = XDocument.Parse(svg);
        Assert.All(expected, content => Assert.Contains(xml.Descendants(), element => (string?)element.Attribute("data-cfx-caption") == content.PlainText));
        var envelope = VisualArtifactInterchangeEnvelope.FromJson(result.Artifact.ToInterchangeJson());
        // Source-family padding28/32 is an exact required version-two presentation fact.
        TopologyViewportPaddingFixtureContract.RequireCapturedInsets(envelope, chart.Viewport.Padding);
        var restored = envelope.ToTopologyChart(); Assert.Equal(Signatures(chart), Signatures(restored));
        var prepared = chart.Prepare(); var capturedSvg = prepared.ToSvg(); var capturedPng = prepared.ToPng(); var snapshot = prepared.ToInterchangeEnvelope().ToJson();
        chart.Nodes[0].Label = "Changed caller";
        if (chart.Groups.Count > 0) chart.Groups[0].Label = "Changed group";
        chart.Edges[0].Label = "Changed relationship";
        Assert.Equal(capturedSvg, prepared.ToSvg()); Assert.Equal(capturedPng, prepared.ToPng()); Assert.Equal(snapshot, prepared.ToInterchangeEnvelope().ToJson());
        Assert.NotEqual(capturedSvg, chart.Prepare().ToSvg());
        var emitted = ExampleRecipeSource.EmitRemainingCaptionRoles(family, width == 390 ? "compact" : "wide", dark ? VisualThemeMode.Dark : VisualThemeMode.Light);
        // The ordinary-program generator hoists imports and makes the shared factory public.
        // Complete programs are compiled and executed by the maintained consumer qualification.
        Assert.DoesNotContain("{{", emitted, StringComparison.Ordinal);
        if (Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY") is { Length: > 0 } output) {
            Directory.CreateDirectory(output); var stem = Path.Combine(output, "remaining-caption-roles-" + family + "-" + width + "-" + dark);
            File.WriteAllText(stem + ".svg", svg); File.WriteAllBytes(stem + ".png", png); File.WriteAllText(stem + ".json", envelope.ToJson());
            File.WriteAllText(stem + ".mmd", source); File.WriteAllText(stem + ".program.cs.txt", emitted);
        }
    }
    [Fact]
    public void PublicGroupAndNullableEdgeContentShareDetachedRunsAndPlainResetContracts() {
        var runs = new List<TextContentRun> { new("Review "), new("work", TextInlineEmphasis.Bold) };
        var content = TextContent.FromRuns(runs); runs.Clear();
        var group = new TopologyGroup { LabelContent = content }; var edge = new TopologyEdge { LabelContent = content };
        Assert.Equal(content.PlainText, group.Label); Assert.Equal(content.PlainText, edge.Label);
        group.Label = "Plain"; edge.Label = "Plain"; Assert.False(group.LabelContent.IsStructured); Assert.False(edge.LabelContent!.IsStructured);
        edge.Label = null; Assert.Null(edge.LabelContent); Assert.Null(edge.Label);
        Assert.Throws<ArgumentNullException>(() => group.LabelContent = null!);
    }
    [Theory]
    [InlineData("group")]
    [InlineData("edge")]
    public void PortableV2RunsRejectContradictoryPlainTextAndKeepPreparedReadersIndependent(string role) {
        var chart = Model(); var content = TextContent.FromRuns(new[] { new TextContentRun("Review "), new TextContentRun("work", TextInlineEmphasis.Italic) });
        if (role == "group") chart.Groups[0].LabelContent = content; else chart.Edges[0].LabelContent = content;
        var prepared = chart.Prepare(); var envelope = prepared.ToInterchangeEnvelope(); Assert.Equal(2, envelope.Version);
        var original = envelope.ToJson();
        Assert.Throws<NotSupportedException>(() => VisualArtifactInterchangeEnvelope.FromJson(original.Replace("\"version\":2", "\"version\":1", StringComparison.Ordinal)));
        if (role == "group") envelope.Groups[0].Label = "Contradiction"; else envelope.Edges[0].Label = "Contradiction";
        Assert.Throws<ArgumentException>(() => envelope.ToJson()); Assert.Equal(original, prepared.ToInterchangeEnvelope().ToJson());
        var restored = VisualArtifactInterchangeEnvelope.FromJson(original).ToTopologyChart();
        Assert.Equal(content.Runs.Select(run => run.Emphasis), (role == "group" ? restored.Groups[0].LabelContent : restored.Edges[0].LabelContent)!.Runs.Select(run => run.Emphasis));
    }
    [Fact]
    public void MetricSelectedEdgeTextDoesNotInheritAuthoredRunsAndPlainGraphiteOutputKeepsV1() {
        var theme = VisualTheme.Graphite();
        var chart = Model().WithTheme(theme); chart.Groups[0].Label = "Scope";
        var ordinaryVersion = chart.Prepare().ToInterchangeEnvelope().Version;
        chart.Edges[0].LabelContent = TextContent.FromRuns(new[] { new TextContentRun("Original", TextInlineEmphasis.Bold) });
        chart.Edges[0].Metrics["reading"] = "Measured";
        var envelope = chart.Prepare(new TopologyRenderOptions { EdgeLabelMetricKey = "reading" }).ToInterchangeEnvelope();
        Assert.Equal("Measured", Assert.Single(envelope.Edges).Label); Assert.Null(Assert.Single(envelope.Edges).Topology!.LabelContent); Assert.Equal(ordinaryVersion, envelope.Version);
        var a = Model().WithTheme(theme); var b = Model().WithTheme(theme); b.Groups[0].LabelContent = TextContent.Plain(a.Groups[0].Label); b.Edges[0].LabelContent = TextContent.Plain(a.Edges[0].Label!);
        Assert.Equal(a.Prepare().ToSvg(), b.Prepare().ToSvg()); Assert.Equal(a.Prepare().ToPng(), b.Prepare().ToPng()); Assert.Equal(a.Prepare().ToInterchangeEnvelope().ToJson(), b.Prepare().ToInterchangeEnvelope().ToJson());
    }
    [Fact]
    public void PresentationCallbackCannotRewriteTheCapturedHeaderRelationshipOrPortableSource() {
        var chart = Model();
        chart.Groups[0].LabelContent = TextContent.FromRuns(new[] { new TextContentRun("Original "), new TextContentRun("header", TextInlineEmphasis.Bold) });
        chart.Edges[0].LabelContent = TextContent.FromRuns(new[] { new TextContentRun("Original "), new TextContentRun("relationship", TextInlineEmphasis.Italic) });
        PreparedTopology? captured = null;
        var html = new HtmlInteractiveTopologyRenderer().RenderPresentationFragment(chart, prepared => {
            captured = prepared;
            chart.Groups[0].Label = "Changed header"; chart.Edges[0].Label = "Changed relationship";
            return prepared.ToSvg();
        });
        Assert.NotNull(captured);
        var envelope = captured!.ToInterchangeEnvelope();
        Assert.Equal("Original header", Assert.Single(envelope.Groups).Label);
        Assert.Equal("Original relationship", Assert.Single(envelope.Edges).Label);
        Assert.True(Assert.Single(envelope.Groups).Topology!.LabelContent!.IsStructured);
        Assert.True(Assert.Single(envelope.Edges).Topology!.LabelContent!.IsStructured);
        Assert.Contains("Original header", html, StringComparison.Ordinal); Assert.DoesNotContain("Changed header", html, StringComparison.Ordinal);
        envelope.Groups[0].Topology!.LabelContent = TextContent.FromRuns(new[] { new TextContentRun("Mutated reader") });
        Assert.Equal("Original header", captured.ToInterchangeEnvelope().Groups[0].Topology!.LabelContent!.PlainText);
    }
    [Theory]
    [InlineData("class")]
    [InlineData("er")]
    [InlineData("flowchart")]
    [InlineData("requirement")]
    public void UnsupportedActiveCaptionSyntaxStaysLiteralLocatedAndStrictlyRejectable(string family) {
        var source = MermaidRemainingCaptionRolesExample.Source(family).Replace("**scope**", "<b>scope</b>", StringComparison.Ordinal)
            .Replace("**carefully**", "$$carefully$$", StringComparison.Ordinal);
        var result = MermaidRenderer.Render(source); Assert.False(result.HasErrors);
        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == MermaidDiagnosticCodes.UnsupportedStatement && diagnostic.Span.Line > 1 && diagnostic.Span.Column > 0);
        var xml = XDocument.Parse(result.Artifact!.ToSvg()); Assert.DoesNotContain(xml.Descendants(), element => element.Name.LocalName is "script" or "foreignObject" or "image");
        var strict = MermaidRenderer.Render(source, new MermaidRenderOptions { WarningPolicy = MermaidWarningPolicy.Reject }); Assert.Null(strict.Artifact);
    }
    internal static MermaidRenderOptions Options(int width, bool dark) {
        var topology = new MermaidTopologyRenderOptions { Width = width, Height = 620 };
        return new MermaidRenderOptions {
            Presentation = new MermaidPresentationOptions { FontFamily = BrowserProofFonts.Family, ThemeMode = dark ? VisualThemeMode.Dark : VisualThemeMode.Light, TopologySizing = MermaidTopologySizing.Natural },
            Class = topology, EntityRelationship = topology, Requirement = topology, Flowchart = new MermaidFlowchartRenderOptions { Width = width, Height = 620 }
        };
    }
    private static IEnumerable<TextContent> Contents(TopologyChart chart) => chart.Groups.Select(group => group.LabelContent)
        .Concat(chart.Nodes.Select(node => node.LabelContent)).Concat(chart.Nodes.SelectMany(node => node.Details)
            .Select(detail => detail.Content ?? TextContent.Join(TextContent.Plain(detail.Label + " "), detail.ValueContent)))
        .Concat(chart.Edges.Where(edge => edge.LabelContent != null).Select(edge => edge.LabelContent!));
    private static string[] Signatures(TopologyChart chart) => Contents(chart).Where(content => content.IsStructured)
        .Select(content => content.PlainText + ":" + string.Join(";", content.Runs.Select(run => run.Text + "=" + run.Emphasis))).OrderBy(value => value, StringComparer.Ordinal).ToArray();
    internal static TopologyChart Model() => TopologyChart.Create().WithViewport(800, 500).WithTheme(VisualTheme.Report().WithFontFamily(BrowserProofFonts.Family))
        .AddAutoGroup("scope", "Scope").AddAutoNode("a", "First", groupId: "scope").AddAutoNode("b", "Second", groupId: "scope").AddEdge("link", "a", "b", "Relationship");
}
