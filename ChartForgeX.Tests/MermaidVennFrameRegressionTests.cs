using ChartForgeX.VisualArtifacts;
using System.Xml.Linq;
using ChartForgeX.Markup.Mermaid;
using ChartForgeX.Mermaid;
using ChartForgeX.Raster;
using ChartForgeX.Themes;
using ChartForgeX.VisualBlocks;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Parsing validates set semantics; actual host dimensions own paint fitting.</summary>
public sealed class MermaidVennFrameRegressionTests {
    [Fact]
    public void LargeFiniteOutlineParsesWithoutAssumingADefaultHostFrame() {
        const string source = "venn-beta\nset A [Alpha] : 10\nstyle A stroke-width:10000";
        var parsed = new MermaidParser().ParseVenn(source);
        Assert.Empty(parsed.Diagnostics);
        Assert.Equal(10000, Assert.Single(parsed.Document!.Sets).StrokeWidth);
        var defaultFrame = MermaidRenderer.Render(source);
        Assert.True(defaultFrame.HasErrors); Assert.Null(defaultFrame.Artifact);
        Assert.Equal(MermaidDiagnosticCodes.ConversionFailed, Assert.Single(defaultFrame.Diagnostics).Code);
        var fenced = new MermaidVisualMarkupParser().Parse("```mermaid\n" + source + "\n```");
        Assert.True(fenced.HasErrors); Assert.Empty(fenced.Artifacts);
        Assert.Contains(fenced.Diagnostics, item => item.Code == MermaidDiagnosticCodes.ConversionFailed);
        var largeFrame = MermaidRenderer.Render(source, new MermaidRenderOptions {
            Venn = new MermaidVennRenderOptions { Width = 12040, Height = 12040 } });
        Assert.Empty(largeFrame.Diagnostics); Assert.NotNull(largeFrame.Artifact);
        Assert.Equal(12040, Assert.IsType<VennDiagramBlock>(largeFrame.Artifact!.Model).Options.Size.Width);
        // No large raster is allocated: this source/host contract only converts its native model.
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ExplicitLargerFrameFitsOutlineAndKeepsDirectFencedSvgPngIdentical(bool dark) {
        const string source = "venn-beta\nset A [Alpha] : 10\nstyle A stroke-width:500";
        var options = new MermaidRenderOptions {
            Presentation = new MermaidPresentationOptions { ThemeMode = dark ? VisualThemeMode.Dark : VisualThemeMode.Light, FontFamily = BrowserProofFonts.Family },
            Venn = new MermaidVennRenderOptions { Width = 900, Height = 900 } };
        var direct = MermaidRenderer.Render(source, options); Assert.Empty(direct.Diagnostics);
        var artifact = direct.Artifact!; var svg = artifact.ToSvg(); var png = artifact.ToPng();
        var set = Assert.Single(XDocument.Parse(svg).Descendants(), item => (string?)item.Attribute("data-cfx-role") == "venn-set");
        Assert.Equal("500", (string?)set.Attribute("stroke-width"));
        var pixels = RasterImageDecoder.Decode(png); Assert.Equal((900, 900), (pixels.Width, pixels.Height));
        var fence = new MermaidVisualMarkupParser(options).Parse("```mermaid\n" + source + "\n```");
        Assert.Empty(fence.Diagnostics);
        var fenced = Assert.Single(fence.Artifacts); Assert.Equal(svg, fenced.ToSvg()); Assert.Equal(png, fenced.ToPng());
    }

    [Fact]
    public void ImpossibleMembershipRemainsLocatedEvenWhenItsOutlineNeedsALargerHost() {
        const string source = "venn-beta\nset A: 10\nset B: 2\nunion A,B: 3\nstyle A stroke-width:10000";
        var parsed = new MermaidParser().ParseVenn(source);
        Assert.True(parsed.HasErrors);
        var error = Assert.Single(parsed.Diagnostics, item => item.Severity == MermaidDiagnosticSeverity.Error);
        Assert.Equal((MermaidDiagnosticCodes.InvalidStatement, 4), (error.Code, error.Span.Line));
        Assert.Null(MermaidRenderer.Render(source, new MermaidRenderOptions {
            Venn = new MermaidVennRenderOptions { Width = 12040, Height = 12040 } }).Artifact);
    }

    [Fact]
    public void FrameIndependentValidationDoesNotSwallowCancellation() {
        using var cancellation = new CancellationTokenSource(); cancellation.Cancel();
        Assert.Throws<OperationCanceledException>(() => new MermaidParser().Parse(
            "venn-beta\nset A: 10\nstyle A stroke-width:10000", null, cancellation.Token));
    }
}
