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

public sealed class MermaidWardleyOverlayTests {
    internal static string Fixture(bool dark = false) {
        var source = File.ReadAllText(Path.Combine(TestRepository.Root, "tests", "mermaid-conformance", "fixtures", "wardley-native-overlays.mmd"));
        return !dark ? source : source.Replace("theme: default", "theme: dark", StringComparison.Ordinal)
            .Replace("'#f8fafc'", "'#182235'", StringComparison.Ordinal).Replace("'#ffffff'", "'#243247'", StringComparison.Ordinal)
            .Replace("'#172033'", "'#f1f5f9'", StringComparison.Ordinal).Replace("'#334155'", "'#cbd5e1'", StringComparison.Ordinal)
            .Replace("'#d1fae5'", "'#103b36'", StringComparison.Ordinal).Replace("'#065f46'", "'#a7f3d0'", StringComparison.Ordinal);
    }
    internal static MermaidRenderOptions Options(int width, string? font = null) => new() { Wardley = new MermaidWardleyRenderOptions {
        Width = width, Height = 600, Presentation = new MermaidPresentationOptions { FontFamily = font } } };

    [Theory]
    [InlineData(false, 390)] [InlineData(true, 390)] [InlineData(false, 1100)] [InlineData(true, 1100)]
    public void DirectFenceAndNativePixelsRetainRealBoxChildrenAndIndependentPaint(bool dark, int width) {
        var source = Fixture(dark); var result = MermaidRenderer.Render(source, Options(width)); Assert.Empty(result.Diagnostics);
        var document = Assert.IsType<MermaidWardleyDocument>(result.Document); var artifact = result.Artifact!; var map = Assert.IsType<WardleyMapBlock>(artifact.Model);
        var svg = artifact.ToSvg(); var png = artifact.ToPng(); WardleyOverlayTests.Capture(map, "wardley-source-" + width + (dark ? "-dark" : "-light")); var xml = XDocument.Parse(svg);
        Assert.Equal(source, document.SourceText); Assert.NotNull(document.AnnotationsBox); Assert.Equal(Array.FindIndex(source.Split('\n'), line => line.StartsWith("annotations ", StringComparison.Ordinal)) + 1, document.AnnotationsBox!.Span.Line);
        Assert.Equal(5, map.Nodes.Count); Assert.Equal(2, map.Pipelines.Single().Components.Count); Assert.Equal(2, map.Annotations.Count);
        Assert.Equal("Service_Scale", map.Links.Last().FromId); Assert.Equal("Market", map.Links.Last().ToId);
        var child = Assert.Single(xml.Descendants(), e => (string?)e.Attribute("data-node-id") == "Service_Scale");
        var link = WardleyOverlayTests.Roles(xml, "wardley-link").Last(); Assert.Equal((string?)child.Attribute("cx"), (string?)link.Attribute("x1")); Assert.Equal((string?)child.Attribute("cy"), (string?)link.Attribute("y1"));
        Assert.Null(Assert.Single(WardleyOverlayTests.Roles(xml, "wardley-pipeline")).Attribute("stroke-dasharray")); Assert.Single(WardleyOverlayTests.Roles(xml, "wardley-pipeline-evolution")); Assert.Equal(6, WardleyOverlayTests.Roles(xml, "wardley-grid").Length);
        var box = Assert.Single(WardleyOverlayTests.Roles(xml, "wardley-annotation-box")).Elements().Single(e => e.Name.LocalName == "rect");
        Assert.Equal(dark ? "#103B36" : "#D1FAE5", ChartColor.Parse((string)box.Attribute("fill")!).ToHex()); Assert.Equal("#059669", ChartColor.Parse((string)box.Attribute("stroke")!).ToHex());
        Assert.Equal(VisualArtifactHtmlSizing.PreserveSize, artifact.HtmlSizing); Assert.DoesNotContain("<script", svg, StringComparison.OrdinalIgnoreCase);
        var image = RasterImageDecoder.Decode(png); Assert.Equal(width, image.Width); Assert.Equal(600, image.Height);
        Assert.True(Pixels(image, ChartColor.Parse(dark ? "#103B36" : "#D1FAE5")) > 128, "The source annotation fill must reach native pixels.");
        var fenced = new MermaidVisualMarkupParser(Options(width)).Parse("```mermaid\n" + source + "\n```"); Assert.Empty(fenced.Diagnostics);
        Assert.Equal(svg, Assert.Single(fenced.Artifacts).ToSvg()); Assert.Equal(png, fenced.Artifacts[0].ToPng());
        var envelope = VisualArtifactInterchangeEnvelope.FromJson(artifact.ToInterchangeJson()); Assert.Equal("true", envelope.Extensions["mermaid.annotation-box"]); Assert.Equal("2", envelope.Extensions["mermaid.annotations"]);
        Assert.Empty(envelope.Nodes); // The common portable envelope does not fabricate a typed Wardley reconstruction.
        var clone = artifact.Clone(); Assert.Equal(svg, clone.ToSvg()); Assert.Same(map.Style, ((WardleyMapBlock)clone.Model!).Style);
    }
    private static int Pixels(RgbaImage image, ChartColor color) { var count = 0; for (var i = 0; i < image.Pixels.Length; i += 4)
        if (image.Pixels[i] == color.R && image.Pixels[i + 1] == color.G && image.Pixels[i + 2] == color.B && image.Pixels[i + 3] == 255) count++; return count; }

    [Fact]
    public void ChildLinksResolveAfterDeclarationsAndExplicitMainIdentityWinsOverDisplayAlias() {
        const string source = "wardley-beta\nScale -> API\ncomponent API [0.5,0.3]\ncomponent Scale [0.9,0.1]\npipeline API {\ncomponent Scale [0.6] label [18,24]\n}\nAPI_Scale -> Scale";
        var result = MermaidRenderer.Render(source); Assert.Empty(result.Diagnostics); var map = Assert.IsType<WardleyMapBlock>(result.Artifact!.Model);
        Assert.Equal("Scale", map.Links[0].FromId); Assert.Equal("API_Scale", map.Links[1].FromId); Assert.Equal(18, map.Pipelines[0].Components[0].LabelOffsetX); Assert.NotEmpty(map.ToPng());
    }
    [Theory]
    [InlineData("component API [0.5,0.3]\npipeline API {\ncomponent Same [0.2]\ncomponent Same [0.7]\n}")]
    [InlineData("component API [0.5,0.3]\npipeline API {\ncomponent Scale [0.2]\n}\nUnknown -> Scale")]
    [InlineData("component API [0.5,0.3]\ncomponent API_Scale [0.7,0.7]\npipeline API {\ncomponent Scale [0.2]\n}")]
    [InlineData("component A [0.5,0.3]\ncomponent B [0.7,0.7]\npipeline A {\ncomponent Scale [0.2]\n}\npipeline B {\ncomponent Scale [0.8]\n}\nScale -> A")]
    public void UnknownCollidingOrAmbiguousPipelineIdentitiesAreLocatedErrors(string body) {
        var result = MermaidRenderer.Render("wardley-beta\n" + body); Assert.True(result.HasErrors); Assert.Null(result.Artifact); Assert.Contains(result.Diagnostics, d => d.Severity == MermaidDiagnosticSeverity.Error && d.Span.Line > 1);
    }
    [Theory]
    [InlineData("annotations [NaN,0.2]")] [InlineData("annotations [0.2,101]")]
    [InlineData("annotations [0.2,0.2]\nannotations [0.3,0.3]")]
    [InlineData("pipeline API {\ncomponent Scale [0.2] label [NaN,1]\n}")]
    public void MalformedOverlayCoordinatesDoNotProduceArtifacts(string body) {
        var result = MermaidRenderer.Render("wardley-beta\ncomponent API [0.5,0.3]\n" + body); Assert.True(result.HasErrors); Assert.Null(result.Artifact);
    }
    [Theory]
    [InlineData("width: -1")] [InlineData("width: '600'")] [InlineData("padding: .inf")] [InlineData("useMaxWidth: 'false'")]
    public void InvalidFamilyScalarsRemainLocatedAndSafe(string setting) {
        var result = MermaidRenderer.Render("---\nconfig:\n  wardley-beta:\n    " + setting + "\n---\nwardley-beta\ncomponent API [0.5,0.3]");
        Assert.True(result.HasErrors); Assert.Null(result.Artifact); Assert.Contains(result.Diagnostics, d => d.Code == MermaidDiagnosticCodes.InvalidConfiguration && d.Span.Line == 4);
    }
    [Theory]
    [InlineData("size [NaN,600]")] [InlineData("size [0,600]")] [InlineData("size [12.5,600]")] [InlineData("size [2147483648,600]")]
    public void InvalidSourceCanvasDimensionsAreLocatedBeforeNativeSizeConversion(string size) {
        var result = MermaidRenderer.Render("wardley-beta\n" + size + "\ncomponent API [0.5,0.3]");
        Assert.True(result.HasErrors); Assert.Null(result.Artifact); Assert.Contains(result.Diagnostics, d => d.Severity == MermaidDiagnosticSeverity.Error && d.Span.Line == 2);
    }

    [Fact]
    public void ExplicitHostThemeOwnsColorsAndSourceDimensionsPrecedeFamilyDefaults() {
        var options = Options(640); options.Wardley!.Presentation!.Theme = VisualTheme.Report();
        var result = MermaidRenderer.Render(Fixture(), options); Assert.Empty(result.Diagnostics); var map = Assert.IsType<WardleyMapBlock>(result.Artifact!.Model); Assert.Null(map.Style);
        var source = "%%{init:{'wardley-beta':{'width':700,'height':500,'padding':12,'useMaxWidth':true}}}%%\nwardley-beta\nsize [600,400]\ncomponent API [0.5,0.3]";
        var sized = MermaidRenderer.Render(source); Assert.Empty(sized.Diagnostics); var block = (WardleyMapBlock)sized.Artifact!.Model!;
        Assert.Equal(600, block.Options.Size.Width); Assert.Equal(400, block.Options.Size.Height); Assert.Equal(12, block.Options.Padding.Left); Assert.Equal(VisualArtifactHtmlSizing.FitToWidth, sized.Artifact.HtmlSizing);
        var unsafePaint = MermaidRenderer.Render(Fixture().Replace("'#d1fae5'", "'url(https://example.invalid/paint)'", StringComparison.Ordinal)); Assert.True(unsafePaint.HasErrors); Assert.Null(unsafePaint.Artifact);
    }
    [Theory]
    [InlineData("Genesis @ 0.3 / Concept -> Product @ 1", .3)]
    [InlineData("Genesis @ 0.3 / Concept -> Product", .5)]
    [InlineData("Genesis / Concept -> Product", .5)]
    public void SourceStageLabelsAndCompleteOrPartialBoundariesUseThePinnedDistribution(string stages, double boundary) {
        var result = MermaidRenderer.Render("wardley-beta\nevolution " + stages + "\ncomponent API [0.5,0.5]"); Assert.Empty(result.Diagnostics);
        var map = (WardleyMapBlock)result.Artifact!.Model!; Assert.Equal(new[] { "Genesis / Concept", "Product" }, map.Stages); Assert.Equal(boundary, map.StageBoundaries[0]); Assert.NotEmpty(map.ToPng());
    }

    [Fact]
    public void AnnotationBoxParticipatesInTheExistingCumulativeElementBudget() {
        var result = new MermaidParser().Parse("wardley-beta\ncomponent API [0.5,0.3]\nannotations [0.2,0.2]", new MermaidParseOptions {
            ResourceLimits = new MermaidResourceLimits { MaximumElements = 1 } });
        Assert.True(result.HasErrors); Assert.Contains(result.Diagnostics, d => d.Code == MermaidDiagnosticCodes.ResourceLimitExceeded);
    }
}
