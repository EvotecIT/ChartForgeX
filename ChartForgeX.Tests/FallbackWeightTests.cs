using ChartForgeX.Composition;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;
using ChartForgeX.SvgRaster;
using ChartForgeX.Typography;
using Xunit;

namespace ChartForgeX.Tests;

[Collection(nameof(FontRegistryCollection))]
public sealed class FallbackWeightTests {
    [Theory]
    [InlineData("regular", true)]
    [InlineData("bold", false)]
    public void ARealBoldPrimarySynthesizesOnlyRegularFallbackGlyphs(string role, bool shouldEmbolden) {
        var root = Path.Combine(Path.GetTempPath(), "cfx-fallback-weight-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try {
            foreach (var name in new[] { "primary", role }.Distinct()) {
                using var source = typeof(FallbackWeightTests).Assembly.GetManifestResourceStream("ChartForgeX.Tests.Fixtures.OpenType.fallback-weight-" + name + ".ttf")!;
                using var file = File.Create(Path.Combine(root, name + ".ttf")); source.CopyTo(file);
            }
            FontRegistry.Register("CFX Primary", Path.Combine(root, "primary.ttf"), 900);
            FontRegistry.Register("CFX Fallback", Path.Combine(root, role + ".ttf"), role == "regular" ? 400 : 700);
            var stack = FontSpec.FromFamily("CFX Primary, CFX Fallback"); stack.Weight = 900;
            var normal = FontSpec.FromFamily("CFX Fallback"); normal.Weight = role == "regular" ? 400 : 700;
            var primary = FontSpec.FromFamily("CFX Primary"); primary.Weight = 900;
            var stackFace = TypographyFontResolver.ResolveFace(stack);
            Assert.False(stackFace.SynthesizeBold);
            Assert.Equal(TypographyFontResolver.ResolveFace(normal).Font!.Measure("B", 40), stackFace.Font!.Measure("B", 40), 8);
            var actual = Draw(stack, "B"); var plain = Draw(normal, "B");
            if (shouldEmbolden) Assert.True(Alpha(actual) > Alpha(plain), "A regular fallback under a real bold face must receive synthetic weight.");
            else Assert.Equal(plain.Pixels, actual.Pixels);
            if (shouldEmbolden) {
                var synthesized = FontSpec.FromFamily("CFX Fallback"); synthesized.Weight = 900;
                Assert.Equal(Draw(synthesized, "B").Pixels, actual.Pixels);
                var glyphs = TextShaper.Shape(stackFace.Font, "B", 40);
                var plainInk = glyphs[0].Face.MeasureGlyphInk(glyphs, 40, false)!.Value;
                var actualInk = stackFace.Font.MeasureGlyphInk(glyphs, 40, false)!.Value;
                Assert.Equal(plainInk.Width + RgbaCanvas.EmphasisOffset(40), actualInk.Width, 8);
            }
            Assert.Equal(Draw(primary, "A").Pixels, Draw(stack, "A").Pixels);
        } finally { FontRegistry.Clear(); Directory.Delete(root, true); }
    }
    [Theory]
    [InlineData("variable-true-type.ttf", 'B')]
    [InlineData("color-colr0.ttf", 'A')]
    public void ExplicitVariableWeightAndColourFallbackRetainAuthoredInk(string name, char character) {
        var primary = Load("fallback-weight-primary.ttf");
        var fallback = Load(name);
        if (name.StartsWith("variable", StringComparison.Ordinal)) fallback = fallback.WithVariations(FontVariationSettings.Default.WithAxis("wght", 900));
        var glyphs = new[] { new ShapedGlyph(fallback, fallback.MapGlyph(character)) };
        Assert.NotEqual((ushort)0, glyphs[0].Glyph);
        var normal = primary.WithFallbackFamilies(Array.Empty<string>(), 400);
        var bold = primary.WithFallbackFamilies(Array.Empty<string>(), 900);
        RgbaCanvas Paint(TrueTypeFont owner) {
            var canvas = new RgbaCanvas(100, 60, 2, owner, 1, false);
            Assert.True(owner.DrawGlyphs(canvas, 20, 8, glyphs, ChartColor.Black, 40, false));
            return canvas;
        }
        Assert.Equal(Paint(normal).ToOutputPixels(), Paint(bold).ToOutputPixels());
        Assert.Equal(normal.MeasureGlyphInk(glyphs, 40, false), bold.MeasureGlyphInk(glyphs, 40, false));
    }
    private static TrueTypeFont Load(string name) {
        using var source = typeof(FallbackWeightTests).Assembly.GetManifestResourceStream("ChartForgeX.Tests.Fixtures.OpenType." + name)!;
        using var data = new MemoryStream(); source.CopyTo(data); return TrueTypeFont.TryLoad(data.ToArray())!;
    }
    [Theory]
    [InlineData(400)] [InlineData(900)]
    public void SvgSynthesizesFallbackCoverageOnceForRegularAndRealBoldPrimaries(int primaryWeight) {
        var root = Path.Combine(Path.GetTempPath(), "cfx-svg-fallback-weight-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(root);
        try {
            foreach (var role in new[] { "primary", "regular" }) {
                using var source = typeof(FallbackWeightTests).Assembly.GetManifestResourceStream("ChartForgeX.Tests.Fixtures.OpenType.fallback-weight-" + role + ".ttf")!;
                using var bytes = new MemoryStream(); source.CopyTo(bytes); var data = bytes.ToArray();
                if (role == "primary") {
                    var count = System.Buffers.Binary.BinaryPrimitives.ReadUInt16BigEndian(data.AsSpan(4, 2));
                    for (var i = 0; i < count; i++) {
                        var at = 12 + i * 16;
                        if (System.Text.Encoding.ASCII.GetString(data, at, 4) != "OS/2") continue;
                        var table = checked((int)System.Buffers.Binary.BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(at + 8, 4)));
                        System.Buffers.Binary.BinaryPrimitives.WriteUInt16BigEndian(data.AsSpan(table + 4, 2), (ushort)primaryWeight); break;
                    }
                }
                File.WriteAllBytes(Path.Combine(root, role + ".ttf"), data);
            }
            FontRegistry.Register("CFX Primary", Path.Combine(root, "primary.ttf"), primaryWeight);
            FontRegistry.Register("CFX Fallback", Path.Combine(root, "regular.ttf"), 400);
            RgbaImage Render(string family) => SvgRasterizer.ToImage($"<svg xmlns='http://www.w3.org/2000/svg' width='100' height='60'><text x='20' y='40' font-family='{family}' font-weight='900' font-size='40' fill='rgba(17,17,17,.5)'>B</text></svg>");
            var stack = Render("CFX Primary, CFX Fallback"); var direct = Render("CFX Fallback");
            Assert.Equal(direct.Pixels, stack.Pixels);
            Assert.InRange(Enumerable.Range(0, stack.Pixels.Length / 4).Max(i => stack.Pixels[i * 4 + 3]), 1, 128);
        } finally { FontRegistry.Clear(); Directory.Delete(root, true); }
    }
    private static RgbaImage Draw(FontSpec font, string text) {
        var style = TextStyle.Create(40, ChartColor.Black); style.Font = font;
        return ImageComposition.CreateTransparent(100, 60).DrawText(20, 8, 70, text, style, TextWrapMode.NoWrap, null, TextTrimming.None).ToImage();
    }
    private static long Alpha(RgbaImage image) => Enumerable.Range(0, image.Pixels.Length / 4).Sum(i => (long)image.Pixels[i * 4 + 3]);
}
