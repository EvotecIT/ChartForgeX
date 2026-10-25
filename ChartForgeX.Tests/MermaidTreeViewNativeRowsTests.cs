using System.Xml.Linq;
using ChartForgeX.Markup.Mermaid;
using ChartForgeX.Mermaid;
using ChartForgeX.Themes;
using ChartForgeX.VisualArtifacts;
using ChartForgeX.VisualBlocks;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class MermaidTreeViewNativeRowsTests {
    internal const string Source = "---\nconfig:\n  treeView:\n    showIcons: true\n---\ntreeView-beta\npackage/\n├── src/\n│   ├── Engine.cs :::highlight ## Native rendering\n│   └── \"quoted \\\"%% marker\\\".cs\" icon(none)\n└── README.md icon(file) ## Integration guide";

    [Fact]
    public void BoxAndIndentSourcesHaveTheSameNativeHierarchyAndCompleteAnnotations() {
        const string indented = "treeView\npackage/\n    src/\n        Engine.cs :::highlight ## Native rendering\n        \"quoted \\\"%% marker\\\".cs\" icon(none)\n    README.md icon(file) ## Integration guide";
        var standard = new MermaidParser().ParseTreeView(Source); var plain = new MermaidParser().ParseTreeView(indented);
        var heavy = new MermaidParser().ParseTreeView(Source.Replace('├', '┣').Replace('└', '┗').Replace('│', '┃').Replace('─', '━'));
        Assert.Empty(standard.Diagnostics); Assert.Empty(plain.Diagnostics); Assert.Empty(heavy.Diagnostics);
        var nodes = standard.Document!.Nodes;
        Assert.Equal(plain.Document!.Nodes.Select(node => (node.Label, node.Level)), nodes.Select(node => (node.Label, node.Level)));
        Assert.Equal(nodes.Select(node => (node.Label, node.Level)), heavy.Document!.Nodes.Select(node => (node.Label, node.Level)));
        Assert.Equal("quoted \"%% marker\".cs", nodes[3].Label); Assert.Equal("Native rendering", nodes[2].Description);
        Assert.Equal(new[] { "highlight" }, nodes[2].Classes); Assert.True(nodes[0].IsDirectory);
        Assert.Equal(9, nodes[2].Span.Line); Assert.Equal(9, nodes[2].Span.Column);
        var block = standard.Document!.ToTreeViewBlock(); Assert.Equal(5, block.Items.Count); Assert.True(block.ShowIcons);
        Assert.True(block.Items[2].Highlighted); Assert.Equal(TreeViewIcon.None, block.Items[3].Icon);
    }

    [Fact]
    public void UnknownClassesAndIconsAreLocatedInertMetadataWithAVisibleLocalFallback() {
        const string source = "treeView\nroot/\n  file.txt icon(pack:custom) :::external ## Description";
        var result = new MermaidParser().ParseTreeView(source);
        Assert.False(result.HasErrors); Assert.Equal(2, result.Diagnostics.Count);
        Assert.All(result.Diagnostics, item => { Assert.Equal(MermaidDiagnosticCodes.UnsupportedStatement, item.Code); Assert.Equal(3, item.Span.Line); });
        Assert.Equal(12, result.Diagnostics[0].Span.Column); Assert.Equal(30, result.Diagnostics[1].Span.Column);
        var node = result.Document!.Nodes[1]; Assert.Equal("pack:custom", node.IconReference); Assert.Equal(new[] { "external" }, node.Classes);
        var artifact = result.Document!.ToVisualArtifact(); var xml = XDocument.Parse(artifact.ToSvg());
        var row = Assert.Single(xml.Descendants(), item => (string?)item.Attribute("data-icon-reference") == "pack:custom");
        Assert.Equal("external", (string?)row.Attribute("data-source-classes"));
        Assert.DoesNotContain(xml.Descendants(), item => item.Name.LocalName == "script" || item.Name.LocalName == "image" || item.Name.LocalName == "foreignObject");
        Assert.Single(xml.Descendants(), item => (string?)item.Attribute("data-cfx-role") == "treeview-unknown-icon");
        var strict = MermaidRenderer.Render(source, new MermaidRenderOptions { WarningPolicy = MermaidWarningPolicy.Reject });
        Assert.Null(strict.Artifact);
    }

    [Theory]
    [InlineData("  ├─ broken", 3)]
    [InlineData("  │ broken", 3)]
    [InlineData("  child icon(unclosed", 9)]
    [InlineData("  child :::", 9)]
    [InlineData("  \"unclosed", 3)]
    public void MalformedPrefixesAndAnnotationsKeepOriginalLocationsAndProduceNoArtifact(string row, int column) {
        var result = MermaidRenderer.Render("treeView\nroot/\n" + row);
        Assert.Null(result.Artifact); var error = Assert.Single(result.Diagnostics, item => item.Severity == MermaidDiagnosticSeverity.Error);
        Assert.Equal(MermaidDiagnosticCodes.InvalidStatement, error.Code); Assert.Equal(3, error.Span.Line); Assert.Equal(column, error.Span.Column);
        var fenced = new MermaidVisualMarkupParser().Parse("# Source\n\n```mermaid\ntreeView\nroot/\n" + row + "\n```");
        Assert.Empty(fenced.Artifacts); Assert.Contains(fenced.Diagnostics, item => item.Line == 6);
    }

    [Theory]
    [InlineData(false, 390)]
    [InlineData(true, 390)]
    [InlineData(false, 1100)]
    [InlineData(true, 1100)]
    public void DirectAndFencedNativeArtifactsUseTheSameCapturedOptionsAndDeterministicExports(bool dark, int width) {
        var options = Options(dark, width); var clone = options.Clone();
        var direct = MermaidRenderer.Render(Source, clone); Assert.Empty(direct.Diagnostics);
        var directArtifact = direct.Artifact; Assert.NotNull(directArtifact);
        var parser = new MermaidVisualMarkupParser(options); options.TreeView!.Width = 100; options.Presentation!.FontSize = 40;
        var fence = parser.Parse("```mermaid\n" + Source + "\n```"); Assert.Empty(fence.Diagnostics);
        var artifact = Assert.Single(fence.Artifacts); var block = Assert.IsType<TreeViewBlock>(artifact.Model);
        Assert.Equal(width, block.Options.Size.Width); Assert.Equal(5, block.Items.Count);
        var svg = artifact.ToSvg(); var png = artifact.ToPng();
        Assert.Equal(svg, directArtifact.ToSvg()); Assert.Equal(png, directArtifact.ToPng());
        Assert.Equal(svg, artifact.ToSvg()); Assert.Equal(png, artifact.ToPng());
        Assert.Equal(nameof(TreeViewBlock), artifact.Metadata["render.model"]); Assert.Equal(VisualArtifactHtmlSizing.PreserveSize, artifact.HtmlSizing);
        var envelope = VisualArtifactInterchangeEnvelope.FromJson(artifact.ToInterchangeJson());
        Assert.Equal(VisualArtifactInterchangeFamily.TreeView, envelope.Family); Assert.Empty(envelope.Nodes);
        Assert.Equal(block.Items.Count, envelope.TreeView!.Items.Count);
        Assert.Equal(artifact.Title, envelope.Title);
        Save("treeview-mermaid", dark, width, svg, png);
    }

    [Fact]
    public void SourceAndHostIconPoliciesAreIndependentOfExplicitNodeIconsAndInvalidSourceSettingsReject() {
        var result = MermaidRenderer.Render(Source, new MermaidRenderOptions { TreeView = new MermaidTreeViewRenderOptions { ShowIcons = false } });
        Assert.Empty(result.Diagnostics); var xml = XDocument.Parse(result.Artifact!.ToSvg());
        Assert.Single(xml.Descendants(), item => (string?)item.Attribute("data-cfx-role") == "treeview-icon");
        var invalid = MermaidRenderer.Render("---\nconfig:\n  treeView:\n    showIcons: 'true'\n---\ntreeView\nroot");
        Assert.Null(invalid.Artifact); Assert.Contains(invalid.Diagnostics, item => item.Severity == MermaidDiagnosticSeverity.Error && item.Span.Line == 4);
        var tabs = new MermaidParser().ParseTreeView("treeView\nroot\n\tchild\n\t\tleaf"); Assert.Empty(tabs.Diagnostics);
        Assert.Equal(new[] { 0, 1, 2 }, tabs.Document!.Nodes.Select(node => node.Level));
        var apostrophe = new MermaidParser().ParseTreeView("treeView\nroot/\n  O'Brien.cs %% author comment");
        Assert.Empty(apostrophe.Diagnostics); Assert.Equal("O'Brien.cs", apostrophe.Document!.Nodes[1].Label);
    }

    [Fact]
    public void CapturedExportLimitsAndCancellationApplyToNewNaturalRows() {
        var limits = new MermaidResourceLimits { MaximumOutputBytes = 100, MaximumRasterPixels = 100 };
        var options = new MermaidRenderOptions { ParseOptions = new MermaidParseOptions { ResourceLimits = limits } };
        var result = MermaidRenderer.Render("treeView\nroot/\n  file", options); Assert.Empty(result.Diagnostics);
        limits.MaximumOutputBytes = 1000000; limits.MaximumRasterPixels = 1000000;
        Assert.Throws<MermaidResourceLimitException>(() => result.Artifact!.ToSvg());
        Assert.Throws<MermaidResourceLimitException>(() => result.Artifact!.ToPng());
        using var cancelled = new CancellationTokenSource();
        var deferred = MermaidRenderer.Render("treeView\nroot/\n  file", null, cancelled.Token); Assert.Empty(deferred.Diagnostics);
        cancelled.Cancel(); Assert.Throws<OperationCanceledException>(() => deferred.Artifact!.ToSvg());
        Assert.Throws<OperationCanceledException>(() => deferred.Artifact!.ToPng());
    }

    internal static MermaidRenderOptions Options(bool dark, int width) => new() {
        TreeView = new MermaidTreeViewRenderOptions { Width = width, Height = 320 },
        Presentation = new MermaidPresentationOptions { ThemeMode = dark ? VisualThemeMode.Dark : VisualThemeMode.Light, FontFamily = BrowserProofFonts.Family }
    };
    private static void Save(string name, bool dark, int width, string svg, byte[] png) {
        var output = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY"); if (string.IsNullOrWhiteSpace(output)) return;
        Directory.CreateDirectory(output); var path = Path.Combine(output, name + "-" + width + "-" + (dark ? "dark" : "light"));
        File.WriteAllText(path + ".svg", svg); File.WriteAllBytes(path + ".native.png", png);
    }
}
