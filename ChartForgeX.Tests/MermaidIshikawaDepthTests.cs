using System.Xml.Linq;
using ChartForgeX;
using ChartForgeX.Markup.Mermaid;
using ChartForgeX.Mermaid;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;
using ChartForgeX.Themes;
using ChartForgeX.VisualArtifacts;
using ChartForgeX.VisualBlocks;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class MermaidIshikawaDepthTests {
    internal static string Fixture(bool dark = false) {
        var source = File.ReadAllText(Path.Combine(TestRepository.Root, "tests", "mermaid-conformance", "fixtures", "ishikawa-native-depth.mmd"));
        return dark ? source.Replace("theme: default", "theme: dark", StringComparison.Ordinal).Replace("'#e2e8f0'", "'#243247'", StringComparison.Ordinal).Replace("'#182235'", "'#f1f5f9'", StringComparison.Ordinal) : source;
    }
    internal static MermaidRenderOptions Options(bool dark, int width, string? font = null) => new() {
        Ishikawa = new MermaidIshikawaRenderOptions { Width = width, Height = 520,
            Presentation = new MermaidPresentationOptions { ThemeMode = dark ? VisualThemeMode.Dark : VisualThemeMode.Light, FontFamily = font } }
    };

    [Theory]
    [InlineData(false, 390)] [InlineData(true, 390)] [InlineData(false, 1100)] [InlineData(true, 1100)]
    public void DirectFenceAndNativeExportsKeepCompleteOrderedTreeSourceAndFamilyPaint(bool dark, int width) {
        var source = Fixture(dark); var rendered = MermaidRenderer.Render(source, Options(dark, width)); Assert.Empty(rendered.Diagnostics);
        var document = Assert.IsType<MermaidIshikawaDocument>(rendered.Document); var artifact = rendered.Artifact!; var block = Assert.IsType<FishboneDiagramBlock>(artifact.Model);
        Assert.Equal(source, document.SourceText); Assert.True(block.UsesNaturalLayout); Assert.Equal(28, block.Options.Padding.Left);
        Assert.Equal(15, block.Options.Theme.Typography.DataLabelSize); Assert.Equal(VisualArtifactHtmlSizing.PreserveSize, artifact.HtmlSizing);
        var svg = artifact.ToSvg(); var png = artifact.ToPng(); var xml = XDocument.Parse(svg);
        Assert.Equal(28, xml.Descendants().Count(e => (string?)e.Attribute("data-cfx-role") == "fishbone-cause"));
        Assert.Equal(FishboneNaturalLayoutTests.Facts(IshikawaDepthRecipe.Create()).Select(fact => fact.Text), FishboneNaturalLayoutTests.Facts(block).Select(fact => fact.Text));
        var spine = xml.Descendants().Single(e => (string?)e.Attribute("data-cfx-role") == "fishbone-spine"); Assert.Equal("#7C3AED", ChartColor.Parse((string)spine.Attribute("stroke")!).ToHex());
        var head = xml.Descendants().Single(e => (string?)e.Attribute("data-cfx-role") == "fishbone-effect"); Assert.Equal(dark ? "#243247" : "#E2E8F0", ChartColor.Parse((string)head.Attribute("fill")!).ToHex());
        foreach (var text in xml.Descendants().Where(e => e.Name.LocalName == "text")) Assert.Equal(dark ? "#F1F5F9" : "#182235", ChartColor.Parse((string)text.Attribute("fill")!).ToHex());
        foreach (var branch in xml.Descendants().Where(e => (string?)e.Attribute("data-cfx-role") is "fishbone-branch" or "fishbone-subcause-line" or "fishbone-parent-rail"))
            Assert.Equal("#7C3AED", ChartColor.Parse((string)branch.Attribute("stroke")!).ToHex());
        var fenced = new MermaidVisualMarkupParser(Options(dark, width)).Parse("```mermaid\n" + source + "\n```"); Assert.Empty(fenced.Diagnostics);
        Assert.Equal(svg, Assert.Single(fenced.Artifacts).ToSvg()); Assert.Equal(png, fenced.Artifacts[0].ToPng());
        var image = RasterImageDecoder.Decode(png); Assert.Equal(block.Options.Size.Width, image.Width); Assert.Equal(block.Options.Size.Height, image.Height);
        Assert.True(Pixels(image, dark ? ChartColor.Parse("#243247") : ChartColor.Parse("#E2E8F0")) > 256, "The authored effect fill must reach native PNG pixels.");
        Assert.True(Pixels(image, ChartColor.Parse("#7C3AED")) > 8, "The authored source rail color must reach native PNG pixels.");
        var saved = VisualArtifactInterchangeEnvelope.FromJson(artifact.ToInterchangeJson()); Assert.Equal(block.Options.Size.Width, saved.Width); Assert.Equal("29", saved.Extensions["mermaid.nodes"]);
        Assert.Empty(saved.Nodes); // Fishbone JSON retains the common envelope, not a fabricated typed topology.
        Assert.DoesNotContain("<script", artifact.ToHtmlPage(), StringComparison.OrdinalIgnoreCase);
        FishboneNaturalLayoutTests.Capture(block, "ishikawa-source-" + width + (dark ? "-dark" : "-light"));
    }

    private static int Pixels(RgbaImage image, ChartColor color) {
        var count = 0; for (var i = 0; i < image.Pixels.Length; i += 4)
            if (image.Pixels[i] == color.R && image.Pixels[i + 1] == color.G && image.Pixels[i + 2] == color.B && image.Pixels[i + 3] == 255) count++;
        return count;
    }

    [Fact]
    public void OmittedFamilySettingsPreserveTheIncumbentFittingSceneExactly() {
        const string source = "ishikawa-beta\nAPI\n  People\n    Data\n  Process\n    Review";
        var document = new MermaidParser().ParseIshikawa(source).Document!; var actual = document.ToFishboneDiagramBlock(new MermaidIshikawaRenderOptions { Title = "", Width = 900, Height = 520 });
        var expected = FishboneDiagramBlock.Create("API").WithTitle("Mermaid Ishikawa").WithSize(900, 520).WithPadding(30); expected.AddCause("People").AddChild("Data"); expected.AddCause("Process").AddChild("Review");
        Assert.False(actual.UsesNaturalLayout); Assert.Equal(expected.ToSvg(), actual.ToSvg()); Assert.Equal(expected.ToPng(), actual.ToPng());
    }

    [Theory]
    [InlineData("diagramPadding: -1")] [InlineData("diagramPadding: '28'")] [InlineData("diagramPadding: .inf")] [InlineData("useMaxWidth: 'false'")]
    public void InvalidFamilySettingsAreLocatedAndDoNotProducePartialArtifacts(string setting) {
        var result = MermaidRenderer.Render("---\nconfig:\n  ishikawa:\n    " + setting + "\n---\nishikawa-beta\nEffect\n  Cause");
        Assert.True(result.HasErrors); Assert.Null(result.Artifact); Assert.Contains(result.Diagnostics, d => d.Code == MermaidDiagnosticCodes.InvalidConfiguration && d.Span.Line == 4);
    }

    [Fact]
    public void SafePaintIsFamilyScopedAndExplicitHostThemeRetainsPrecedence() {
        var source = Fixture(); var host = VisualTheme.Report(); var options = Options(false, 900); options.Ishikawa!.Presentation!.Theme = host;
        var rendered = MermaidRenderer.Render(source, options); Assert.Empty(rendered.Diagnostics); var block = Assert.IsType<FishboneDiagramBlock>(rendered.Artifact!.Model);
        Assert.Equal(host.Resolve(VisualThemeMode.Light).Axis, block.Options.Theme.Resolve(VisualThemeMode.Light).Axis);
        Assert.Equal(host.Resolve(VisualThemeMode.Light).Surface, block.Options.Theme.Resolve(VisualThemeMode.Light).Surface);
        var invalid = MermaidRenderer.Render(source.Replace("'#e2e8f0'", "'url(https://example.invalid/paint)'", StringComparison.Ordinal));
        Assert.True(invalid.HasErrors); Assert.Null(invalid.Artifact);
        // mainBkg is now a shared topology role; use a genuinely inactive Ishikawa-only setting for this scope guard.
        var other = MermaidRenderer.Render("%%{init:{'ishikawa':{'diagramPadding':28}}}%%\nflowchart LR\na-->b");
        Assert.Contains(other.Diagnostics, d => d.Code == MermaidDiagnosticCodes.UnsupportedConfiguration && d.Message.Contains("ishikawa.diagramPadding", StringComparison.Ordinal));
    }

    [Fact]
    public void SizingOnlyChangesHostPolicyAndCapturedRasterBudgetStillBindsTheExpandedFrame() {
        var source = Fixture(); var first = MermaidRenderer.Render(source).Artifact!;
        var second = MermaidRenderer.Render(source.Replace("useMaxWidth: false", "useMaxWidth: true", StringComparison.Ordinal)).Artifact!;
        Assert.Equal(first.ToSvg(), second.ToSvg()); Assert.Equal(first.ToPng(), second.ToPng());
        Assert.Equal(VisualArtifactHtmlSizing.FitToWidth, second.HtmlSizing); Assert.Contains("data-cfx-html-sizing=\"preserve-size\"", first.ToHtmlPage());
        var limits = new MermaidResourceLimits { MaximumRasterPixels = 1 };
        var bounded = MermaidRenderer.Render(source, new MermaidRenderOptions { ParseOptions = new MermaidParseOptions { ResourceLimits = limits } }); Assert.False(bounded.HasErrors);
        limits.MaximumRasterPixels = int.MaxValue; Assert.Equal(nameof(limits.MaximumRasterPixels), Assert.Throws<MermaidResourceLimitException>(() => bounded.Artifact!.ToPng()).LimitName);
    }
}
