using System.Text.Json;
using ChartForgeX.Mermaid;
using ChartForgeX.Raster;
using ChartForgeX.Topology;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class MermaidFlowchartAssetCausalTests {
    [Fact]
    public void ExistingNativeCatalogCanRepresentAnAuthoredExternalIconCaption() {
        const string source = "flowchart LR\nserver@{icon: 'common:server', label: 'Server', pos: t}";
        CaptureInput(source, "source-native-icon-caption-top");
        var rendered = MermaidRenderer.Render(source, new MermaidRenderOptions {
            Presentation = new MermaidPresentationOptions { FontFamily = BrowserProofFonts.Family },
            Flowchart = new MermaidFlowchartRenderOptions { IconCatalog = TopologyIconCatalog.Default() }
        });
        var exportFailure = Capture(rendered, "source-native-icon-caption-top");
        Assert.Null(exportFailure); Assert.Empty(rendered.Diagnostics);
        var node = Assert.Single(Assert.IsType<TopologyChart>(rendered.Artifact!.Model).Nodes);
        Assert.Equal("common:server", node.IconId); Assert.Equal(TopologyNodeDisplayMode.Icon, node.DisplayMode); Assert.Equal("Server", node.Label);
    }

    [Fact]
    public void IconMetadataWithoutARealLabelDoesNotInventAnIdentityCaption() {
        const string source = "flowchart LR\nserver@{icon: 'common:server'}";
        CaptureInput(source, "source-icon-without-label");
        var rendered = MermaidRenderer.Render(source, BrowserProofFonts.MermaidOptions()); var exportFailure = Capture(rendered, "source-icon-without-label");
        Assert.Null(exportFailure); Assert.Empty(rendered.Diagnostics);
        var node = Assert.Single(Assert.IsType<TopologyChart>(rendered.Artifact!.Model).Nodes);
        Assert.False(node.ShowLabel); Assert.Equal("server", node.Label); Assert.Equal(source, rendered.Document!.SourceText);
    }

    [Fact]
    public void UnboundImageReferencesRemainInertLocatedAndRejectable() {
        const string source = "flowchart LR\npicture@{img: 'https://unbound.invalid/picture.png'}";
        CaptureInput(source, "unbound-image-reference-control");
        var rendered = MermaidRenderer.Render(source, BrowserProofFonts.MermaidOptions()); var exportFailure = Capture(rendered, "unbound-image-reference-control");
        var strict = MermaidRenderer.Render(source, new MermaidRenderOptions { WarningPolicy = MermaidWarningPolicy.Reject });
        Capture(strict, "unbound-image-reference-strict-control");
        Assert.Null(exportFailure); Assert.False(rendered.HasErrors);
        var warning = Assert.Single(rendered.Diagnostics); Assert.Equal(2, warning.Span.Line); Assert.Equal(MermaidDiagnosticCodes.UnsupportedStatement, warning.Code);
        Assert.DoesNotContain("<image", rendered.Artifact!.ToSvg()); Assert.True(strict.HasErrors);
    }

    [Fact]
    public void NullNativeCaptionAndGlyphPoliciesHaveIncumbentByteControls() {
        var failures = new List<Exception?>();
        foreach (var mode in new[] { TopologyNodeDisplayMode.Icon, TopologyNodeDisplayMode.Tile, TopologyNodeDisplayMode.Artwork }) {
            var chart = TopologyChart.Create().WithTitle("").WithViewport(320, 240, 12).WithLegend(null);
            var image = new RgbaImage(2, 2, new byte[] { 255,0,0,255, 0,255,0,128, 0,0,255,0, 255,255,0,255 });
            chart.Nodes.Add(new TopologyNode { Id = "node", Label = "Caption", Subtitle = "Detail", X = 80, Y = 65, Width = 64, Height = 64, DisplayMode = mode,
                Shape = TopologyNodeShape.Rectangle, Color = "#123456", IconId = "common:server",
                Artwork = mode == TopologyNodeDisplayMode.Artwork ? TopologyIconArtwork.Portrait(image) : null,
                ShowStatusBadge = false, PreserveDisplayModeSize = true });
            failures.Add(CaptureArtifact(chart.ToVisualArtifact(), "ordinary-null-caption-glyph-" + mode));
        }
        const string source = "flowchart LR\na[Plan] --> b[Deliver]"; CaptureInput(source, "ordinary-source-before-assets");
        var ordinary = MermaidRenderer.Render(source, BrowserProofFonts.MermaidOptions()); failures.Add(Capture(ordinary, "ordinary-source-before-assets"));
        Assert.All(failures, Assert.Null); Assert.Empty(ordinary.Diagnostics);
        Assert.Equal(new[] { "a", "b" }, Assert.IsType<MermaidFlowchartDocument>(ordinary.Document).Nodes.Select(node => node.Id));
    }

    private static void CaptureInput(string source, string name) => Write(name + ".input.mmd", source);
    private static Exception? Capture(MermaidRenderResult result, string name) {
        Write(name + ".diagnostics.json", JsonSerializer.Serialize(result.Diagnostics.Select(item =>
            new { item.Code, item.Message, item.Severity, item.Span.Line, item.Span.Column, item.Span.Length })));
        return result.Artifact == null ? null : CaptureArtifact(result.Artifact, name);
    }
    private static Exception? CaptureArtifact(VisualArtifact artifact, string name) {
        Exception? failure = null;
        Observe("svg", () => Write(name + ".svg", artifact.ToSvg()));
        Observe("png", () => { var bytes = artifact.ToPng(); var output = Output(); if (output != null) File.WriteAllBytes(Path.Combine(output, name + ".native.png"), bytes); });
        Observe("portable", () => Write(name + ".json", artifact.ToInterchangeJson()));
        return failure;
        void Observe(string lane, Action action) {
            try { action(); } catch (Exception exception) {
                failure ??= exception; Write(name + "." + lane + ".failure.json", JsonSerializer.Serialize(new { Type = exception.GetType().FullName, exception.Message }));
            }
        }
    }
    private static void Write(string name, string text) { var output = Output(); if (output != null) File.WriteAllText(Path.Combine(output, name), text); }
    private static string? Output() {
        var output = Environment.GetEnvironmentVariable("CFX_FLOWCHART_ASSET_CAPTURE_DIRECTORY"); if (string.IsNullOrWhiteSpace(output)) return null;
        Directory.CreateDirectory(output); return output;
    }
}
