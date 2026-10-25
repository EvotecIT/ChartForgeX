using System.Xml.Linq;
using ChartForgeX.Markup.Mermaid;
using ChartForgeX.Mermaid;
using ChartForgeX.Themes;
using ChartForgeX.Topology;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class MermaidMindMapNativeNotationTests {
    [Fact]
    public void AuthoredShapesModifiersAndIdentityUseTheExistingHierarchyWithoutInventedGlyphs() {
        var result = MermaidRenderer.Render(Source, Options(false, 1100)); Assert.False(result.HasErrors);
        Assert.Single(result.Diagnostics);
        Assert.All(result.Diagnostics, item => Assert.Equal(MermaidDiagnosticCodes.ApproximateMapping, item.Code));
        var document = Assert.IsType<MermaidMindMapDocument>(result.Document);
        Assert.Equal(Source, document.SourceText); Assert.Empty(document.RawStatements);
        Assert.Equal(new string?[] { "root", "plan", "store", "review", "ideas", "alert", null }, document.Nodes.Select(node => node.SourceId));
        Assert.Equal(new[] { 0, 1, 2, 1, 1, 1, 1 }, document.Nodes.Select(node => node.Level));
        Assert.Equal(Enumerable.Range(0, 7).Select(index => "mindmap-node-" + index), document.Nodes.Select(node => node.Id));
        Assert.Equal("`**Roadmap**<br>service #38; data`", document.Nodes[0].Text);
        Assert.Equal("cloud", document.Nodes[4].Shape); Assert.Equal("bang", document.Nodes[5].Shape);
        Assert.Equal("highlight", Assert.Single(document.Nodes[1].Classes));
        Assert.Equal("common:application", Assert.Single(document.Nodes[1].Icons));
        var chart = Assert.IsType<TopologyChart>(result.Artifact!.Model);
        Assert.Equal(new[] { TopologyNodeShape.Circle, TopologyNodeShape.Rounded, TopologyNodeShape.Rectangle, TopologyNodeShape.Hexagon, TopologyNodeShape.Cloud, TopologyNodeShape.Burst, TopologyNodeShape.Rounded }, chart.Nodes.Select(node => node.Shape!.Value));
        Assert.Equal("Roadmap\nservice & data", chart.Nodes[0].Label);
        Assert.Contains(chart.Nodes[0].LabelContent.Runs, run => run.Text == "Roadmap" && run.Emphasis == TextInlineEmphasis.Bold);
        Assert.Equal("Plan\nnext step", chart.Nodes[1].Label);
        Assert.All(chart.Nodes, node => { Assert.Equal(TopologyNodeKind.Generic, node.Kind); Assert.Null(node.Symbol); Assert.True(node.FitLabelToContent); Assert.True(node.PreserveDisplayModeSize); });
        Assert.Equal(new string?[] { null, "common:application", "common:database", null, null, null, null }, chart.Nodes.Select(node => node.IconId));
        Assert.Equal(new[] { chart.Nodes[1].Id }, chart.DefaultRenderOptions!.SelectedNodeIds);
        Assert.Equal(document.Nodes.Where(node => node.ParentId != null).Select(node => (node.ParentId, node.Id)), chart.Edges.Select(edge => ((string?)edge.SourceNodeId, edge.TargetNodeId)));
        Assert.All(chart.Edges, edge => { Assert.Equal(TopologyEdgeRouting.Curved, edge.Routing); Assert.Equal(VisualLinkDirection.Forward, edge.Direction); });
        foreach (var node in document.Nodes) {
            var native = chart.Nodes.Single(item => item.Id == node.Id);
            Assert.Equal(node.Text, native.Metadata["mermaid.source.label"]);
            Assert.Equal(node.Span.Line.ToString(System.Globalization.CultureInfo.InvariantCulture), native.Metadata["mermaid.source.line"]);
        }
        var markerLine = Source.Split('\n').Select((line, index) => (line, index)).Single(item => item.line.Contains(":::highlight", StringComparison.Ordinal));
        var diagnostic = result.Diagnostics.Single(item => item.Message.Contains("selected-node", StringComparison.Ordinal));
        Assert.Equal((markerLine.index + 1, markerLine.line.IndexOf("highlight", StringComparison.Ordinal) + 1, 9), (diagnostic.Span.Line, diagnostic.Span.Column, diagnostic.Span.Length));
    }

    [Fact]
    public void MultilineQuotedMarkdownKeepsLiteralModifiersCommentsAndPhysicalSourceSpan() {
        const string statement = "  root[\"`**Line**\r\nliteral :::tag ::icon(common:database) %% keep`\"]";
        var source = "mindmap\r\n" + statement + "\r\n    leaf[Leaf]";
        var result = MermaidRenderer.Render(source); Assert.False(result.HasErrors);
        var document = Assert.IsType<MermaidMindMapDocument>(result.Document); Assert.Equal(2, document.Nodes.Count);
        var root = document.Nodes[0]; Assert.Equal("root", root.SourceId); Assert.Empty(root.Classes); Assert.Empty(root.Icons);
        Assert.Equal((2, 3, statement.Length - 2), (root.Span.Line, root.Span.Column, root.Span.Length));
        Assert.Equal(statement.Substring(2), source.Substring(source.IndexOf("root", StringComparison.Ordinal), root.Span.Length));
        Assert.Equal("`**Line**\nliteral :::tag ::icon(common:database) %% keep`", root.Text);
        Assert.Equal("Line\nliteral :::tag ::icon(common:database) %% keep", Assert.IsType<TopologyChart>(result.Artifact!.Model).Nodes[0].Label);
        Assert.Empty(result.Diagnostics);
        Assert.Contains(Assert.IsType<TopologyChart>(result.Artifact.Model).Nodes[0].LabelContent.Runs,
            run => run.Text == "Line" && run.Emphasis == TextInlineEmphasis.Bold);
        Assert.NotEmpty(result.Artifact.ToSvg()); Assert.NotEmpty(result.Artifact.ToPng());
    }

    [Fact]
    public void ExternalClassesAndIconsRemainLocatedInertFactsAndQualifiedIconsNeverGuessFromNames() {
        const string source = "mindmap\n  database[Database]:::external\n    ::icon(fa fa-database)\n    server[Server]::icon(database)\n    router[Router]::icon(unknown:router)\n    explicit[Explicit]::icon(common:database)::icon(network:router)";
        var result = MermaidRenderer.Render(source); Assert.False(result.HasErrors);
        Assert.Equal(5, result.Diagnostics.Count);
        Assert.Equal(4, result.Diagnostics.Count(item => item.Code == MermaidDiagnosticCodes.UnsupportedStatement));
        Assert.Contains(result.Diagnostics, item => item.Code == MermaidDiagnosticCodes.ApproximateMapping && item.Span.Line == 6);
        Assert.All(result.Diagnostics, item => { Assert.True(item.Span.Line > 1); Assert.True(item.Span.Column > 1); Assert.True(item.Span.Length > 0); });
        var chart = Assert.IsType<TopologyChart>(result.Artifact!.Model);
        Assert.All(chart.Nodes.Take(3), node => { Assert.Null(node.IconId); Assert.Null(node.Symbol); Assert.Equal(TopologyNodeKind.Generic, node.Kind); });
        Assert.Equal("common:database", chart.Nodes[3].IconId);
        Assert.Equal("fa fa-database", chart.Nodes[0].Metadata["mermaid.icons"]);
        var xml = XDocument.Parse(result.Artifact.ToSvg());
        Assert.DoesNotContain(xml.Descendants(), item => item.Name.LocalName is "script" or "foreignObject" or "image" or "style");
        Assert.DoesNotContain(xml.Descendants().Attributes("class"), item => item.Value.Split(' ').Contains("external"));
        Assert.NotEmpty(result.Artifact.ToPng());
        var strict = MermaidRenderer.Render(source, new MermaidRenderOptions { WarningPolicy = MermaidWarningPolicy.Reject }); Assert.Null(strict.Artifact);
        Assert.Contains(strict.Diagnostics, item => item.Code == MermaidDiagnosticCodes.RejectedWarning);
    }

    [Theory]
    [InlineData("::icon(common:database)")]
    [InlineData("root[unclosed")]
    [InlineData("root[Label] trailing")]
    [InlineData("root(-Cloud)")]
    [InlineData("root[]")]
    [InlineData("root[\"``\"]")]
    [InlineData("root[\"unclosed]")]
    [InlineData("root[Label]::: ")]
    [InlineData("root[Label]::icon(")]
    [InlineData("root[Label]::icon()")]
    public void MalformedNotationIsLocatedAndProducesNoPartialDirectOrFenceArtifact(string statement) {
        var source = "mindmap\n  " + statement;
        var rendered = MermaidRenderer.Render(source); Assert.True(rendered.HasErrors); Assert.Null(rendered.Artifact);
        Assert.Contains(rendered.Diagnostics, item => item.Code == MermaidDiagnosticCodes.InvalidStatement && item.Span.Line == 2 && item.Span.Column == 3);
        var document = Assert.IsType<MermaidMindMapDocument>(rendered.Document); Assert.NotEmpty(document.RawStatements);
        var fence = new MermaidVisualMarkupParser().Parse("Text\n\n```mermaid\n" + source + "\n```");
        Assert.True(fence.HasErrors); Assert.Empty(fence.Artifacts); Assert.Contains(fence.Diagnostics, item => item.Line == 5);
    }

    [Fact]
    public void CapturedBudgetsIncludeModifierFactsDepthDeferredExportsAndCancellation() {
        const string source = "mindmap\n Root\n  Child:::highlight\n    ::icon(common:database)";
        var limits = new MermaidResourceLimits { MaximumElements = 3 };
        var options = new MermaidRenderOptions { ParseOptions = new MermaidParseOptions { ResourceLimits = limits } };
        var rejected = MermaidRenderer.Render(source, options); Assert.Null(rejected.Artifact);
        Assert.Contains(rejected.Diagnostics, item => item.Code == MermaidDiagnosticCodes.ResourceLimitExceeded && item.Span.Line == 4);
        limits.MaximumElements = 4; var accepted = MermaidRenderer.Render(source, options); Assert.False(accepted.HasErrors); Assert.NotNull(accepted.Artifact);
        limits.MaximumNestingDepth = 1;
        var deep = MermaidRenderer.Render(source, options); Assert.Null(deep.Artifact);
        Assert.Contains(deep.Diagnostics, item => item.Code == MermaidDiagnosticCodes.ResourceLimitExceeded && item.Span.Line == 3);
        limits.MaximumNestingDepth = 4; limits.MaximumOutputBytes = 32; limits.MaximumRasterPixels = 1;
        var bounded = MermaidRenderer.Render(source, options); Assert.False(bounded.HasErrors);
        limits.MaximumOutputBytes = 10_000_000; limits.MaximumRasterPixels = 10_000_000;
        Assert.Equal(nameof(limits.MaximumOutputBytes), Assert.Throws<MermaidResourceLimitException>(() => bounded.Artifact!.ToSvg()).LimitName);
        Assert.Equal(nameof(limits.MaximumRasterPixels), Assert.Throws<MermaidResourceLimitException>(() => bounded.Artifact!.ToPng()).LimitName);
        using var cancel = new CancellationTokenSource(); cancel.Cancel();
        Assert.Throws<OperationCanceledException>(() => new MermaidParser().Parse(source, null, cancel.Token));
        var fence = new MermaidVisualMarkupParser(options).Parse("```mermaid\n" + source + "\n```"); Assert.False(fence.HasErrors); Assert.Single(fence.Artifacts);
    }

    [Fact]
    public void NativeAndSourceEditsReexportWithoutMutatingCapturedPortableFacts() {
        var rendered = MermaidRenderer.Render(Source, Options(false, 1100)); var artifact = rendered.Artifact!;
        var originalSvg = artifact.ToSvg(); var originalPng = artifact.ToPng();
        var saved = VisualArtifactInterchangeEnvelope.FromJson(artifact.ToInterchangeJson()); var json = saved.ToJson();
        var chart = Assert.IsType<TopologyChart>(artifact.Model); var node = chart.Nodes[5]; node.Label = "Edited"; node.Shape = TopologyNodeShape.Hexagon; node.IconId = "common:service";
        Assert.NotEqual(originalSvg, artifact.ToSvg()); Assert.False(originalPng.SequenceEqual(artifact.ToPng()));
        var updated = VisualArtifactInterchangeEnvelope.FromJson(artifact.ToInterchangeJson()).Nodes.Single(item => item.Id == node.Id);
        Assert.Equal("Edited", updated.Label); Assert.Equal(TopologyNodeShape.Hexagon, updated.Topology!.Shape); Assert.Equal("common:service", updated.IconId);
        Assert.Equal("Attention", updated.Extensions["mermaid.source.label"]); Assert.Equal("alert", updated.Extensions["mermaid.source.id"]);
        Assert.Equal(json, saved.ToJson()); Assert.Equal(TopologyNodeShape.Burst, saved.Nodes.Single(item => item.Id == node.Id).Topology!.Shape);
        var document = Assert.IsType<MermaidMindMapDocument>(rendered.Document); document.Nodes[5].Shape = "cloud"; document.Nodes[5].Icons.Add("network:router");
        var remapped = document.ToTopologyChart(); Assert.Equal(TopologyNodeShape.Cloud, remapped.Nodes[5].Shape); Assert.Equal("network:router", remapped.Nodes[5].IconId);
        Assert.Equal(TopologyNodeShape.Hexagon, node.Shape);
        var legacy = new MermaidMindMapNode("host", "Host", 0, new MermaidSourceSpan(1, 1, 4)); Assert.Null(legacy.SourceId);
        var authored = new MermaidMindMapNode("host", "Host", 0, new MermaidSourceSpan(1, 1, 4), "source"); Assert.Equal("source", authored.SourceId);
        Assert.Throws<ArgumentException>(() => new MermaidMindMapNode("host", "Host", 0, authored.Span, " "));
    }

    [Fact]
    public void QuotedContinuationWordsCannotBecomeSourceConfigurationAccessibilityOrModifiers() {
        const string source = "mindmap\naccTitle: Outside\nroot[\"`Literal\naccTitle: Inside\naccDescr: Inside description\n%%{init: {'theme':'dark'}}%%\n::icon(common:database)\n:::highlight\n`\"]\n  child[Child]";
        var result = MermaidRenderer.Render(source); Assert.False(result.HasErrors);
        var document = Assert.IsType<MermaidMindMapDocument>(result.Document); Assert.Equal(2, document.Nodes.Count);
        Assert.Equal("Outside", document.Accessibility.Name); Assert.Null(document.Accessibility.Description); Assert.Null(document.Configuration.Theme);
        Assert.Empty(document.Directives); Assert.Empty(document.Nodes[0].Classes); Assert.Empty(document.Nodes[0].Icons);
        Assert.Contains("accTitle: Inside", document.Nodes[0].Text, StringComparison.Ordinal);
        var chart = Assert.IsType<TopologyChart>(result.Artifact!.Model); Assert.Contains("%%{init:", chart.Nodes[0].Label, StringComparison.Ordinal);
        Assert.Null(chart.Nodes[0].IconId); Assert.Empty(chart.DefaultRenderOptions!.SelectedNodeIds);
        var fence = new MermaidVisualMarkupParser().Parse("```mermaid\n" + source + "\n```"); Assert.False(fence.HasErrors);
        Assert.Equal(result.Artifact.ToSvg(), Assert.Single(fence.Artifacts).ToSvg()); Assert.NotEmpty(result.Artifact.ToPng());
    }

    internal static string Source => File.ReadAllText(Path.Combine(TestRepository.Root, "ChartForgeX.Examples", "Assets", "mindmap-native-notation.mmd"));
    internal static MermaidRenderOptions Options(bool dark, int width) => new() {
        Presentation = new MermaidPresentationOptions { FontFamily = BrowserProofFonts.Family, FontSize = 12, ThemeMode = dark ? VisualThemeMode.Dark : VisualThemeMode.Light, TopologySizing = MermaidTopologySizing.Natural },
        MindMap = new MermaidTopologyRenderOptions { Width = width, Height = 560 }
    };
}
