using System.Globalization;
using System.Xml.Linq;
using ChartForgeX.Markup.Mermaid;
using ChartForgeX.Mermaid;
using ChartForgeX.Raster;
using ChartForgeX.Themes;
using ChartForgeX.VisualArtifacts;
using ChartForgeX.VisualBlocks;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class MermaidRailroadNativeRenderingTests {
    public static IEnumerable<object[]> SourcePoses() {
        foreach (var source in MermaidRailroadParserTests.CommonSources())
            foreach (var width in new[] { 390, 1200 }) foreach (var dark in new[] { false, true })
                yield return new[] { source[0], source[1], width, dark };
    }

    [Theory]
    [MemberData(nameof(SourcePoses))]
    public void EveryFrontendUsesTheSameNativeDirectFencedSvgPngAndMeasuredFrame(string dialect, string source, int width, bool dark) {
        var options = new MermaidRenderOptions {
            WarningPolicy = MermaidWarningPolicy.Reject,
            Presentation = new MermaidPresentationOptions { ThemeMode = dark ? VisualThemeMode.Dark : VisualThemeMode.Light, FontFamily = BrowserProofFonts.Family },
            Railroad = new MermaidRailroadRenderOptions { Width = width, Height = 240 }
        };
        var direct = MermaidRenderer.Render(source, options); Assert.Empty(direct.Diagnostics);
        var artifact = Assert.IsType<VisualArtifact>(direct.Artifact); var model = Assert.IsType<RailroadDiagramBlock>(artifact.Model);
        Assert.Equal(VisualArtifactHtmlSizing.PreserveSize, artifact.HtmlSizing);
        Assert.Equal(new[] { "value", "part" }, model.Rules.Select(rule => rule.Id));
        var fenced = new MermaidVisualMarkupParser(options).Parse("```mermaid\n" + source + "\n```"); Assert.Empty(fenced.Diagnostics);
        var fromFence = Assert.Single(fenced.Artifacts); var svg = artifact.ToSvg(); var png = artifact.ToPng();
        Assert.Equal(svg, fromFence.ToSvg()); Assert.Equal(png, fromFence.ToPng());
        Assert.Equal(svg, artifact.ToSvg()); Assert.Equal(png, artifact.ToPng());
        var xml = XDocument.Parse(svg); var image = RasterImageDecoder.Decode(png);
        Assert.Equal(Number(xml.Root!, "width"), image.Width); Assert.Equal(Number(xml.Root!, "height"), image.Height);
        Assert.Equal(image.Width, artifact.NaturalSize!.Value.Width); Assert.Equal(image.Height, artifact.NaturalSize.Value.Height);
        Assert.True(image.Width >= width); Assert.Contains(BrowserProofFonts.Family, svg, StringComparison.Ordinal);
        Assert.Equal(2, xml.Descendants().Count(node => (string?)node.Attribute("data-cfx-role") == "railroad-rule"));
        Assert.DoesNotContain(xml.Descendants(), node => node.Name.LocalName is "script" or "foreignObject");
        Save(dialect, dark, width, svg, png);
    }

    [Fact]
    public void SourceAndCapturedHostTypographyDriveNaturalMeasurementWithoutClippingOrMutatingDefaults() {
        const string source = "---\nconfig:\n  theme: dark\n  fontFamily: Source Font\n---\nrailroad-ebnf-beta\nvalue = \"an intentionally wide terminal\" [ value ] ;";
        var options = new MermaidRenderOptions {
            Presentation = new MermaidPresentationOptions { FontFamily = BrowserProofFonts.Family, ThemeMode = VisualThemeMode.Light, FontSize = 18 },
            Railroad = new MermaidRailroadRenderOptions { Width = 100, Height = 100, Presentation = new MermaidPresentationOptions { FontSize = 23 } }
        };
        var parser = new MermaidVisualMarkupParser(options);
        options.Presentation.FontSize = 9; options.Railroad.Presentation.FontSize = 9;
        var fence = parser.Parse("```mermaid\n" + source + "\n```"); Assert.Empty(fence.Diagnostics);
        var artifact = Assert.Single(fence.Artifacts); var block = Assert.IsType<RailroadDiagramBlock>(artifact.Model);
        Assert.Equal(23, block.Options.Theme.Typography.DataLabelSize); Assert.Equal(VisualThemeMode.Light, block.Options.ThemeMode);
        Assert.Equal(BrowserProofFonts.Family, block.Options.Theme.Typography.Family);
        Assert.True(block.Options.Size.Width > 100); Assert.True(block.Options.Size.Height > 100);
        Assert.True(artifact.ToPng().Length > 64);
    }

    [Fact]
    public void NaturalGrammarUsesCapturedOutputAndPixelLimitsAndDeferredCancellation() {
        var limits = new MermaidResourceLimits { MaximumOutputBytes = 100, MaximumRasterPixels = 100 };
        var options = new MermaidRenderOptions { ParseOptions = new MermaidParseOptions { ResourceLimits = limits } };
        const string source = "railroad-ebnf-beta\nvalue = \"x\" { value } ;";
        var result = MermaidRenderer.Render(source, options); Assert.Empty(result.Diagnostics);
        limits.MaximumOutputBytes = 1000000; limits.MaximumRasterPixels = 1000000;
        Assert.Equal(nameof(MermaidResourceLimits.MaximumOutputBytes), Assert.Throws<MermaidResourceLimitException>(() => result.Artifact!.ToSvg()).LimitName);
        Assert.Equal(nameof(MermaidResourceLimits.MaximumRasterPixels), Assert.Throws<MermaidResourceLimitException>(() => result.Artifact!.ToPng()).LimitName);
        using var cancellation = new CancellationTokenSource();
        var deferred = MermaidRenderer.Render(source, null, cancellation.Token); Assert.Empty(deferred.Diagnostics); cancellation.Cancel();
        Assert.Throws<OperationCanceledException>(() => deferred.Artifact!.ToSvg()); Assert.Throws<OperationCanceledException>(() => deferred.Artifact!.ToPng());
    }

    private static double Number(XElement element, string name) => double.Parse(element.Attribute(name)!.Value, CultureInfo.InvariantCulture);
    private static void Save(string dialect, bool dark, int width, string svg, byte[] png) {
        var directory = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY"); if (string.IsNullOrWhiteSpace(directory)) return;
        Directory.CreateDirectory(directory); var path = Path.Combine(directory, "railroad-" + dialect + "-" + width + "-" + (dark ? "dark" : "light"));
        File.WriteAllText(path + ".svg", svg); File.WriteAllBytes(path + ".png", png);
    }
}
