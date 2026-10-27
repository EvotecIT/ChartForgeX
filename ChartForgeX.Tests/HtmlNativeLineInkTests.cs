using System.Globalization;
using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;
using ChartForgeX.Rendering;
using ChartForgeX.Typography;
using Xunit;

namespace ChartForgeX.Tests;

[Collection(nameof(FontRegistryCollection))]
public sealed class HtmlNativeLineInkTests : IDisposable {
    [Theory]
    [InlineData("color-colr0", "😀")]
    [InlineData("color-cbdt1", "😀")]
    [InlineData("bitmap-1-1-1", "A")]
    public void AutomaticNonOutlineInkUsesTheSamePreparedNativePixelsAndKeepsExplicitContourAdmission(string fixture, string text) {
        var before = Prepare(fixture, text); var svg = before.ToSvg(); var png = before.ToPng();
        var automatic = before.ToSvg(Automatic()); Capture(fixture, before, automatic);
        var image = Image(automatic); var pixels = Decode(image);
        AssertCrop(before.ToRgba(), image, pixels);
        Assert.DoesNotContain(XDocument.Parse(automatic).Descendants(), node => node.Name.LocalName == "text");
        Assert.Contains("data-cfx-native-raster-text-ink", automatic, StringComparison.Ordinal);
        Assert.DoesNotContain("data-cfx-owned-font-text", automatic, StringComparison.Ordinal);
        Assert.Throws<NotSupportedException>(() => before.ToSvg(new VisualSvgOptions().WithTextPresentation(VisualSvgTextPresentation.NativeContours)));
        Assert.Equal(svg, before.ToSvg()); Assert.Equal(png, before.ToPng());
        Assert.Equal(svg, before.ToSvg(new VisualSvgOptions().WithTextPresentation(VisualSvgTextPresentation.Browser)));
        Assert.Contains(pixels.Pixels.Chunk(4), pixel => pixel[3] > 0);
        Assert.Equal(text, image.Parent!.Parent!.Attribute("data-cfx-caption")?.Value);
        var painted = Enumerable.Range(0, pixels.Width * pixels.Height).Where(index => pixels.Pixels[index * 4 + 3] != 0).ToArray();
        var left = painted.Min(index => index % pixels.Width); var top = painted.Min(index => index / pixels.Width);
        var right = painted.Max(index => index % pixels.Width) + 1; var bottom = painted.Max(index => index / pixels.Width) + 1;
        Assert.Equal($"{left} {top} {right-left} {bottom-top} {pixels.Width} {pixels.Height}", image.Attribute("data-cfx-image-alpha-bounds")?.Value);
    }
    [Theory] [InlineData(false)] [InlineData(true)]
    public void OverlappingMultilineInkAndItsSingleStrokeMaskRetainNativeComposition(bool stroke) {
        var before = Prepare("color-colr0", "😀\n😀", stroke, .8);
        var text = Assert.Single(before.Scene.Nodes.OfType<VisualSceneText>());
        var first = text.Text.Face.Font!.MeasureGlyphInk(text.Text.Lines[0].Glyphs, text.Text.Size, false) ?? throw new InvalidOperationException("The first fixture line requires actual ink.");
        var second = text.Text.Face.Font!.MeasureGlyphInk(text.Text.Lines[1].Glyphs, text.Text.Size, false) ?? throw new InvalidOperationException("The second fixture line requires actual ink.");
        // The fixture's real layer ink must overlap at the admitted 0.8 line height, before testing one native stroke mask.
        Assert.True(Math.Min(first.Bottom, second.Bottom + text.Text.Metrics.LineHeight)
            > Math.Max(first.Top, second.Top + text.Text.Metrics.LineHeight));
        var firstInk = LineInk(0); var secondInk = LineInk(1); var intersects = false;
        for (var alpha = 3; alpha < firstInk.Pixels.Length; alpha += 4)
            if (firstInk.Pixels[alpha] != 0 && secondInk.Pixels[alpha] != 0) { intersects = true; break; }
        Assert.True(intersects, "The separately rendered glyph/stroke masks must actually overlap.");
        RgbaImage LineInk(int index) {
            var source = text.Text; var line = source.Lines[index];
            var single = new VisualScenePreparedText(source.Face, source.Style, source.Size, source.Ascent,
                new[] { line }, new TextMetrics(line.Width, source.Metrics.LineHeight, source.Metrics.LineHeight));
            var node = new VisualSceneText(single, text.X, text.Baseline + index * source.Metrics.LineHeight, ChartColor.White,
                text.Alignment, text.Role, text.Id, stroke: stroke ? ChartColor.White : null, strokeWidth: text.StrokeWidth);
            return new PreparedVisual(new VisualScene(before.Size, new VisualSceneNode[] { node },
                Array.Empty<VisualDiagnostic>(), Array.Empty<VisualSemanticRegion>())).ToRgba();
        }
        var automatic = before.ToSvg(Automatic()); Capture("overlap-" + stroke, before, automatic);
        var image = Image(automatic); AssertCrop(before.ToRgba(), image, Decode(image));
        Assert.Single(XDocument.Parse(automatic).Descendants(), node => node.Name.LocalName == "image");
        Assert.Equal(2, Assert.Single(before.Scene.Nodes.OfType<VisualSceneText>()).Text.Lines.Count);
    }
    [Theory] [InlineData(2)] [InlineData(3)]
    public void StrokedMultilineCaptureRetainsTheOriginalNativePixelPhaseAtIntegerOutputDensity(int scale) {
        var before = Prepare("color-colr0", "😀\n😀", stroke: true, lineHeight: .8);
        var node = Assert.Single(before.Scene.Nodes.OfType<VisualSceneText>()); Assert.Equal(2, node.Text.Lines.Count);
        var first = node.Text.Face.Font!.MeasureGlyphInk(node.Text.Lines[0].Glyphs, node.Text.Size, false)!.Value;
        var second = node.Text.Face.Font!.MeasureGlyphInk(node.Text.Lines[1].Glyphs, node.Text.Size, false)!.Value;
        Assert.True(Math.Min(first.Bottom, second.Bottom + node.Text.Metrics.LineHeight)
            > Math.Max(first.Top, second.Top + node.Text.Metrics.LineHeight));
        var raster = new VisualRenderOptions(scale: scale, supersampling: 2);
        var full = before.ToRgba(raster); var automatic = before.ToSvg(Automatic(raster));
        Capture("stroke-output-grid-" + scale, before, automatic);
        if (Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY") is { Length: > 0 } output) {
            Directory.CreateDirectory(output);
            File.WriteAllBytes(Path.Combine(output, "s10-native-ink-stroke-output-grid-" + scale + ".full-native.png"), PngWriter.WriteRgba(full));
        }
        var image = Image(automatic); AssertCrop(full, image, Decode(image), scale);
    }
    [Theory]
    [InlineData("color-colr0", "😀", 2)] [InlineData("color-colr0", "😀", 3)]
    [InlineData("color-cbdt1", "😀", 2)] [InlineData("color-cbdt1", "😀", 3)]
    [InlineData("bitmap-1-1-1", "A", 2)] [InlineData("bitmap-1-1-1", "A", 3)]
    public void IntegerOutputAndCoverageGridRetainsTheExactFullNativeCrop(string fixture, string text, int scale) {
        var before = Prepare(fixture, text); var ordinarySvg = before.ToSvg(); var ordinaryPng = before.ToPng();
        var raster = new VisualRenderOptions(scale: scale, supersampling: 2);
        var full = before.ToRgba(raster); var automatic = before.ToSvg(Automatic(raster));
        Capture("output-grid-" + fixture + "-" + scale, before, automatic);
        if (Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY") is { Length: > 0 } output) {
            Directory.CreateDirectory(output);
            File.WriteAllBytes(Path.Combine(output, "s10-native-ink-output-grid-" + fixture + "-" + scale + ".full-native.png"), PngWriter.WriteRgba(full));
        }
        var image = Image(automatic); AssertCrop(full, image, Decode(image), scale);
        Assert.Equal(ordinarySvg, before.ToSvg()); Assert.Equal(ordinaryPng, before.ToPng());
    }
    [Fact]
    public void OrdinaryOutlineAndNullModesKeepTheirExistingPaintWithoutRasterInk() {
        const string family = "CFX S10 Outline Control"; FontRegistry.Register(family, OpenTypeTestFonts.NameKeyed());
        var builder = new VisualSceneBuilder(new VisualSize(200, 130), FontSpec.FromFamily(family));
        builder.Text("HO", 70, 80, 24, ChartColor.Black); var before = new PreparedVisual(builder.Build());
        var svg = before.ToSvg(); var png = before.ToPng(); var automatic = before.ToSvg(Automatic()); Capture("outline", before, automatic);
        Assert.DoesNotContain("data-cfx-native-raster-text-ink", automatic, StringComparison.Ordinal);
        Assert.Contains("data-cfx-native-text-resources", automatic, StringComparison.Ordinal);
        Assert.Equal(svg, before.ToSvg(new VisualSvgOptions())); Assert.Equal(png, before.ToPng());
        var node = Assert.Single(before.Scene.Nodes.OfType<VisualSceneText>());
        var repeated = new PreparedVisual(new VisualScene(before.Size, new VisualSceneNode[] { node, node },
            Array.Empty<VisualDiagnostic>(), Array.Empty<VisualSemanticRegion>()));
        var emission = repeated.ExportSvgEmission(VisualSvgTextPresentation.Automatic);
        Assert.Single(emission.Fonts!.Faces);
        Assert.Equal(2, XDocument.Parse(emission.Svg).Descendants().Count(element => element.Attribute("data-cfx-owned-font-text") != null));
    }
    [Fact]
    public void ActualCapturePixelAndUriBudgetsAndCancellationRejectBeforeReturningPartialSvg() {
        var before = Prepare("color-cbdt1", "😀"); var baselinePng = before.ToPng(); Capture("budget-control", before, before.ToSvg(Automatic()));
        Assert.Throws<ArgumentOutOfRangeException>(() => before.ToSvg(Automatic(new VisualRenderOptions(pixelBudget: 1))));
        Assert.Throws<OperationCanceledException>(() => VisualSceneSvgRenderer.Render(before.Scene, null, null, null, false,
            null, Automatic(), false, new CancellationToken(true)));
        Assert.Throws<VisualResourceLimitException>(() => VisualSceneSvgRenderer.Render(before.Scene, null, null, null, false,
            null, Automatic(), false, CancellationToken.None, maximumCharacters: 100));
        var doubled = before.ToSvg(Automatic(new VisualRenderOptions(scale: 2)));
        var image = Image(doubled); Assert.Equal(checked((int)Number(image, "width")) * 2, Decode(image).Width);
        Assert.Equal(checked((int)Number(image, "height")) * 2, Decode(image).Height);
        Assert.Equal(baselinePng, before.ToPng());
    }
    [Fact]
    public void RepeatedImmutableTextPlacementsReuseTheirCaptureAndCountEveryActualUriEmission() {
        var before = Prepare("color-cbdt1", "😀"); var node = Assert.Single(before.Scene.Nodes.OfType<VisualSceneText>());
        var repeated = new PreparedVisual(new VisualScene(before.Size, new VisualSceneNode[] { node, node },
            Array.Empty<VisualDiagnostic>(), Array.Empty<VisualSemanticRegion>()));
        var svg = repeated.ToSvg(Automatic()); Capture("repeated", repeated, svg);
        var images = XDocument.Parse(svg).Descendants().Where(element => element.Name.LocalName == "image").ToArray();
        Assert.Equal(2, images.Length); Assert.Equal(images[0].Attribute("href")!.Value, images[1].Attribute("href")!.Value);
        var oneUriLength = images[0].Attribute("href")!.Value.Length;
        Assert.Throws<VisualResourceLimitException>(() => VisualSceneSvgRenderer.Render(repeated.Scene, null, null, null, false,
            null, Automatic(), false, CancellationToken.None, maximumCharacters: oneUriLength + 1));
    }
    [Fact]
    public void AggregateDistinctNativeInkCapturesUseTheExistingDeclaredPixelCeiling() {
        var before = Prepare("color-cbdt1", "😀"); var source = Assert.Single(before.Scene.Nodes.OfType<VisualSceneText>());
        var nodes = Enumerable.Range(0, 5000).Select(index => (VisualSceneNode)new VisualSceneText(source.Text, source.X, source.Baseline,
            source.Color, source.Alignment, source.Role, "dense-ink-" + index)).ToArray();
        var dense = new PreparedVisual(new VisualScene(before.Size, nodes, Array.Empty<VisualDiagnostic>(), Array.Empty<VisualSemanticRegion>()));
        // Every individual crop fits. The same preflight rejects their total before the first native canvas/copy.
        Assert.Throws<ArgumentOutOfRangeException>(() => dense.ToSvg(Automatic()));
        Assert.NotEmpty(before.ToSvg(Automatic()));
    }
    [Fact]
    public void CallerAndReturnedPngMutationCannotRebindPreparedInkAndMalformedPixelsUseTheExistingDecoder() {
        var bytes = Bytes("color-cbdt1"); var before = Prepare("color-cbdt1", "😀", bytes: bytes);
        var svg = before.ToSvg(Automatic()); var png = before.ToPng(); Capture("detached", before, svg);
        var uri = Image(svg).Attribute("href")!.Value; var encoded = Convert.FromBase64String(uri.Substring("data:image/png;base64,".Length));
        bytes.AsSpan().Clear(); encoded.AsSpan().Clear(); FontRegistry.Clear();
        Assert.Equal(svg, before.ToSvg(Automatic())); Assert.Equal(png, before.ToPng());
        Assert.Throws<NotSupportedException>(() => PngReader.Decode(encoded));
        Assert.StartsWith("data:image/png;base64,", uri, StringComparison.Ordinal);
        Assert.DoesNotContain("<script", svg, StringComparison.OrdinalIgnoreCase);
    }
    internal static Chart Model(string fixture) {
        var bitmap = fixture.StartsWith("bitmap", StringComparison.Ordinal);
        var family = bitmap ? BrowserProofFonts.Family : Register(fixture, Bytes(fixture));
        var text = bitmap ? "A" : "😀";
        var chart = HtmlOwnedFontDeliveryTests.Model(family).WithTitle("").WithXLabels(text, text);
        if (bitmap) chart.Options.PngFontPath = FixturePath(fixture);
        return chart;
    }
    internal static PreparedVisual Prepare(string fixture, string text, bool stroke = false, double lineHeight = 1.2, byte[]? bytes = null) {
        var bitmap = fixture.StartsWith("bitmap", StringComparison.Ordinal);
        // The retained bitmap fixture intentionally covers A/B only, so it is a valid explicit file face,
        // not a text-family registry face that claims the general CoverageProbe repertoire.
        var font = bitmap ? FontSpec.FromFile(FixturePath(fixture)) : FontSpec.FromFamily(Register(fixture, bytes ?? Bytes(fixture)));
        var style = new TextStyle { Font = font, FontSize = bitmap ? 12 : 32, Color = ChartColor.FromRgba(20, 160, 60, 192),
            Hinting = TextHinting.Full, LineHeight = lineHeight };
        var builder = new VisualSceneBuilder(new VisualSize(240, 210), style.Font);
        builder.Text(text, 80.25, 90.5, style, "native-color-caption", "caption", stroke: stroke ? ChartColor.Black : null, strokeWidth: stroke ? 1.5 : 0);
        return new PreparedVisual(builder.Build());
    }
    private static string FixturePath(string fixture) => Path.Combine(TestRepository.Root, "ChartForgeX.Tests", "Fixtures", "OpenType", fixture + ".ttf");
    private static string Register(string fixture, byte[] bytes) { var family = "CFX S10 Native Ink " + fixture; FontRegistry.Register(family, bytes); return family; }
    private static byte[] Bytes(string fixture) {
        using var source = typeof(HtmlNativeLineInkTests).Assembly.GetManifestResourceStream("ChartForgeX.Tests.Fixtures.OpenType." + fixture + ".ttf")
            ?? throw new InvalidOperationException("The retained native glyph fixture is not embedded: " + fixture);
        using var buffer = new MemoryStream(); source.CopyTo(buffer); return buffer.ToArray();
    }
    private static VisualSvgOptions Automatic(VisualRenderOptions? raster = null) => new VisualSvgOptions(effectRasterOptions: raster).WithTextPresentation(VisualSvgTextPresentation.Automatic);
    private static XElement Image(string svg) => Assert.Single(XDocument.Parse(svg).Descendants(), node => node.Name.LocalName == "image");
    private static RgbaImage Decode(XElement image) => PngReader.Decode(Convert.FromBase64String(image.Attribute("href")!.Value.Substring("data:image/png;base64,".Length)));
    private static double Number(XElement element, string name) => double.Parse(element.Attribute(name)!.Value, CultureInfo.InvariantCulture);
    private static void AssertCrop(RgbaImage native, XElement image, RgbaImage captured, int scale = 1) {
        var left = checked((int)(Number(image, "x") * scale)); var top = checked((int)(Number(image, "y") * scale));
        Assert.Equal(checked((int)(Number(image, "width") * scale)), captured.Width); Assert.Equal(checked((int)(Number(image, "height") * scale)), captured.Height);
        Assert.InRange(left, 0, native.Width - captured.Width); Assert.InRange(top, 0, native.Height - captured.Height);
        for (var y = 0; y < captured.Height; y++) Assert.Equal(
            native.Pixels.AsSpan(((top + y) * native.Width + left) * 4, captured.Width * 4).ToArray(),
            captured.Pixels.AsSpan(y * captured.Width * 4, captured.Width * 4).ToArray());
    }
    private static void Capture(string name, PreparedVisual before, string automatic) {
        if (Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY") is not { Length: > 0 } output) return;
        Directory.CreateDirectory(output); var path = Path.Combine(output, "s10-native-ink-" + name);
        File.WriteAllText(path + ".core.svg", before.ToSvg()); File.WriteAllBytes(path + ".native.png", before.ToPng());
        File.WriteAllText(path + ".automatic.svg", automatic);
        foreach (var image in XDocument.Parse(automatic).Descendants().Where(node => node.Name.LocalName == "image"))
            File.WriteAllBytes(path + ".ink.png", Convert.FromBase64String(image.Attribute("href")!.Value.Substring("data:image/png;base64,".Length)));
    }
    public void Dispose() { FontRegistry.Clear(); _ = BrowserProofFonts.Family; }
}
