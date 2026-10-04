using System.Reflection;
using ChartForgeX.Composition;
using ChartForgeX.Core;
using ChartForgeX.Html;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;
using ChartForgeX.Rendering;
using ChartForgeX.Svg;
using ChartForgeX.SvgRaster;
using ChartForgeX.Typography;
using Xunit;

namespace ChartForgeX.Tests;

[Collection(nameof(FontRegistryCollection))]
public sealed class FontVariationTests {
    private static byte[] Bytes(string name = "variable-true-type.ttf") {
        using var input = typeof(FontVariationTests).Assembly.GetManifestResourceStream("ChartForgeX.Tests.Fixtures.OpenType." + name)!;
        using var output = new MemoryStream(); input.CopyTo(output); return output.ToArray();
    }
    private static TrueTypeFont Face(double weight, string name = "variable-true-type.ttf") =>
        TrueTypeFont.TryLoad(Bytes(name))!.WithVariations(FontVariationSettings.Default.WithAxis("wght", weight));
    private static ChartRect Ink(TrueTypeFont face, char character) =>
        (ChartRect)typeof(TrueTypeFont).GetMethod("GlyphInk", BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(face, new object[] { face.MapGlyph(character) })!;

    [Theory]
    [InlineData(100, 192.8, 470, 670)]
    [InlineData(400, 215.2, 500, 700)]
    [InlineData(650, 243.2, 537.5, 737.5)]
    [InlineData(900, 252, 550, 750)]
    public void AxisMappingAdvancesLayoutAndGlobalMetricsAgree(double weight, double width, double xHeight, double capHeight) {
        var face = Face(weight);
        Assert.Equal(width, face.Measure("AB A\u0301 HxXé", 40), 6);
        Assert.Equal(xHeight, face.XHeight, 6); Assert.Equal(capHeight, face.CapHeight, 6);
        // These values were independently calculated from the original font by the OpenType oracle.
    }
    [Fact]
    public void BothAxesAffectOutlinesAndPairPositioning() {
        var face = Face(720).WithVariations(FontVariationSettings.Default.WithAxis("wght", 720).WithAxis("wdth", 120));
        Assert.Equal(289.009609375, face.Measure("AB A\u0301 HxXé", 40), 6);
        var bounds = Ink(face, 'H'); Assert.InRange(bounds.Width, 560, 563); Assert.InRange(bounds.Height, 740, 742);
    }
    [Theory]
    [InlineData(650, 243.2)]
    [InlineData(900, 252)]
    public void PhantomPointAdvancesWorkWithoutHvar(double weight, double width) =>
        Assert.Equal(width, Face(weight, "variable-phantom-metrics.ttf").Measure("AB A\u0301 HxXé", 40), 6);

    [Fact]
    public void CompositeOffsetsAndComponentPhantomMetricsFollowTheSelectedInstance() {
        var face = Face(650);
        var scaled = Ink(face, 'C');
        Assert.Equal(147, scaled.X); Assert.Equal(178.125, scaled.Y);
        Assert.Equal(228, scaled.Width); Assert.Equal(553.125, scaled.Height);
        // HVAR selects authored composite metrics. Without it, USE_MY_METRICS
        // takes the component's advance and ignores the composite's own deltas.
        Assert.Equal(1750, face.AdvanceWidth(face.MapGlyph('M')));
        var phantom = Face(650, "variable-phantom-metrics.ttf");
        Assert.Equal(675, phantom.AdvanceWidth(phantom.MapGlyph('M')));
    }

    [Fact]
    public void InstanceInkCachesDoNotLeakAcrossLanguageViewsOrConcurrentAxes() {
        var root = TrueTypeFont.TryLoad(Bytes())!;
        Parallel.For(0, 60, i => {
            var high = i % 2 == 0;
            var face = root.WithVariations(FontVariationSettings.Default.WithAxis("wght", high ? 900 : 100)).WithLanguage("TRK ");
            Assert.Equal(high ? 475 : 355, Ink(face, 'H').Width, 6);
            Assert.Equal(high ? 750 : 670, Ink(face, 'H').Height, 6);
        });
        Assert.Equal(400, Ink(root, 'H').Width);
    }
    [Theory]
    [InlineData(400, 100, 400)]
    [InlineData(650, 200, 425)]
    [InlineData(900, 300, 450)]
    public void CompactBlendsMoveAndResizeTheSameGlyph(double weight, double x, double width) {
        var face = Face(weight, "variable-compact.otf");
        var bounds = Ink(face, 'H'); Assert.Equal(x, bounds.X); Assert.Equal(width, bounds.Width);
        Assert.Equal(24, face.Measure("H", 40));
    }
    [Fact]
    public void VariationFeatureSubstitutionUsesTheFirstMatchingConditionSet() {
        Assert.Equal(2, Assert.Single(TextShaper.Shape(Face(650), "A")).Glyph);
        Assert.Equal(3, Assert.Single(TextShaper.Shape(Face(900), "A")).Glyph);
    }
    [Fact]
    public void MarkAnchorsVaryAtTheSameLogicalSizeAsAdvances() {
        var mark = TextShaper.Shape(Face(650), "A\u0301")[1];
        Assert.Equal(-300, mark.OffsetX); Assert.Equal(750, mark.OffsetY); Assert.Equal(0, mark.Advance);
        var smaller = TextShaper.Shape(Face(100), "A\u0301")[1];
        Assert.Equal(-300, smaller.OffsetX); Assert.Equal(750, smaller.OffsetY);
        // The absolute mark origin changes with the varied preceding advance and anchor.
        Assert.Equal(375, Face(650).AdvanceWidth(2) + mark.OffsetX);
        Assert.Equal(240, Face(100).AdvanceWidth(2) + smaller.OffsetX);
    }
    [Fact]
    public void AxisSelectionsAreCopiedClampedAndCaseSensitive() {
        var input = new Dictionary<string, double> { ["wght"] = 650 };
        var settings = new FontVariationSettings(input); input["wght"] = 100;
        Assert.Equal(650, settings["wght"]);
        var spec = FontSpec.FromFamily("CFX").WithVariation("wght", 650); var clone = spec.Clone().WithVariation("wdth", 80);
        Assert.Single(spec.Variations); Assert.Equal(2, clone.Variations.Count);
        Assert.Equal(Face(900).Measure("H", 40), Face(9000).Measure("H", 40));
        Assert.Equal(Face(400).Measure("H", 40), TrueTypeFont.TryLoad(Bytes())!.WithVariations(settings.WithAxis("wght", 400).WithAxis("WGHT", 900)).Measure("H", 40));
        var style = new TextStyle { Font = spec };
        Assert.Equal(650, new TextStyleOverride().Resolve(style).Font.Variations["wght"]);
        Assert.Empty(new TextStyleOverride { Variations = FontVariationSettings.Default }.Resolve(style).Font.Variations);
        Assert.Single(style.Font.Variations);
    }
    [Theory]
    [InlineData("wgt", 100)]
    [InlineData("w'gt", 100)]
    [InlineData("wght", double.NaN)]
    [InlineData("wght", double.PositiveInfinity)]
    public void InvalidPublicAxisCoordinatesAreRejected(string tag, double value) =>
        Assert.ThrowsAny<ArgumentException>(() => FontVariationSettings.Default.WithAxis(tag, value));

    [Fact]
    public void StaticFontsIgnoreUnknownAxesAndKeepTheirSyntheticStyle() {
        var face = TrueTypeFont.TryLoad(OpenTypeTestFonts.NameKeyed())!;
        var modified = TypographyFontResolver.WithVariations(new ResolvedTypeface(face, true, true), FontVariationSettings.Default.WithAxis("wght", 900));
        Assert.True(modified.SynthesizeBold); Assert.True(modified.SynthesizeItalic);
        Assert.Equal(face.Measure("H", 40), modified.Font!.Measure("H", 40));
    }
    [Fact]
    public void VariableWeightSuppressesSyntheticBoldButPreservesLanguage() {
        var face = Face(400).WithLanguage("TRK ");
        var resolved = TypographyFontResolver.WithVariations(new ResolvedTypeface(face, true, false), FontVariationSettings.Default.WithAxis("wght", 650));
        Assert.False(resolved.SynthesizeBold); Assert.Equal("TRK ", resolved.Font!.LanguageTag);
    }
    [Theory]
    [InlineData("gvar")]
    [InlineData("HVAR")]
    [InlineData("MVAR")]
    [InlineData("avar")]
    public void MalformedOptionalVariationTablesPreserveUsableText(string tag) {
        var bytes = Bytes();
        for (var i = 0; i < Read16(bytes, 4); i++) {
            var record = 12 + i * 16;
            if (System.Text.Encoding.ASCII.GetString(bytes, record, 4) != tag) continue;
            // Declare a truncated table; optional reads cannot leave its declared extent.
            bytes[record + 12] = bytes[record + 13] = bytes[record + 14] = 0; bytes[record + 15] = 2;
        }
        var face = TrueTypeFont.TryLoad(bytes)!.WithVariations(FontVariationSettings.Default.WithAxis("wght", 650));
        Assert.True(face.Measure("HH", 40) > 0); Assert.True(Ink(face, 'H').Width > 0);
    }
    private static int Read16(byte[] bytes, int at) => bytes[at] * 256 + bytes[at + 1];

    [Fact]
    public void SimpleAndShapedDrawingShareVariedVerticalMetrics() {
        var bytes = Bytes();
        for (var i = 0; i < Read16(bytes, 4); i++) {
            var record = 12 + i * 16;
            var tag = System.Text.Encoding.ASCII.GetString(bytes, record, 4);
            if (tag is "GSUB" or "GPOS") bytes[record] = (byte)'Z';
        }
        var face = TrueTypeFont.TryLoad(bytes)!.WithVariations(FontVariationSettings.Default.WithAxis("wght", 650));
        var direct = new RgbaCanvas(140, 80, 4, face);
        var shaped = new RgbaCanvas(140, 80, 4, face);
        face.Draw(direct, 10, 10, "HH", ChartColor.Black, 40);
        face.DrawGlyphs(shaped, 10, 10, TextShaper.Shape(face, "HH", 40), ChartColor.Black, 40, false);
        Assert.Equal(shaped.ToOutputPixels(), direct.ToOutputPixels());
    }

    [Theory]
    [InlineData(650, 62.5, 675)]
    [InlineData(900, 50, 700)]
    public void LeftPhantomOriginsAndLastComponentMetricsAreAppliedOnlyAtTheTop(double weight, double left, double advance) {
        var face = Face(weight, "variable-origin-metrics.ttf");
        Assert.Equal(left, Ink(face, 'H').X);
        Assert.Equal(left - 100, Ink(face, 'D').X);
        Assert.Equal(left + 100, Ink(face, 'N').X);
        Assert.Equal(advance, face.AdvanceWidth(face.MapGlyph('D')));
        Assert.Equal(advance, face.AdvanceWidth(face.MapGlyph('N')));
    }
    [Fact]
    public void ExplicitDefaultAndUnknownOnlyAxesPreserveDefaultPhantomMetrics() {
        var root = TrueTypeFont.TryLoad(Bytes("variable-phantom-metrics.ttf"))!;
        var explicitDefault = root.WithVariations(FontVariationSettings.Default.WithAxis("wght", 400));
        var unknown = root.WithVariations(FontVariationSettings.Default.WithAxis("WGHT", 900));
        Assert.Equal(1600, root.AdvanceWidth(root.MapGlyph('M')));
        Assert.Equal(root.Measure("H DMN", 40), explicitDefault.Measure("H DMN", 40));
        Assert.Equal(root.Measure("H DMN", 40), unknown.Measure("H DMN", 40));
        Assert.Equal(Ink(root, 'M'), Ink(explicitDefault, 'M'));
    }
    [Theory]
    [InlineData(400, 0, 600)]
    [InlineData(650, -37.5, 675)]
    [InlineData(900, -50, 700)]
    public void EmptyMetricsComponentsRetainTheirVariedPhantomOrigins(double weight, double left, double advance) {
        var face = Face(weight, "variable-empty-metrics.ttf");
        Assert.Equal(left, Ink(face, 'D').X);
        Assert.Equal(left + 200, Ink(face, 'N').X);
        Assert.Equal(advance, face.AdvanceWidth(face.MapGlyph('D')));
        Assert.Equal(advance, face.AdvanceWidth(face.MapGlyph('N')));
    }
    [Fact]
    public void DefaultCoordinateFeaturesApplyWithoutExplicitAxisSelection() {
        var root = TrueTypeFont.TryLoad(Bytes("variable-default-feature.ttf"))!;
        var selected = root.WithVariations(FontVariationSettings.Default.WithAxis("wght", 400));
        Assert.Equal(5, Assert.Single(TextShaper.Shape(root, "H")).Glyph);
        Assert.Equal(5, Assert.Single(TextShaper.Shape(selected, "H")).Glyph);
        Assert.Equal(root.Measure("H", 40), selected.Measure("H", 40));
    }
    [Fact]
    public void CompactResetViewsDoNotRetainThePriorBlendStore() {
        var root = TrueTypeFont.TryLoad(Bytes("variable-compact.otf"))!;
        var varied = root.WithVariations(FontVariationSettings.Default.WithAxis("wght", 900)).WithLanguage("TRK ");
        Assert.Equal(300, Ink(varied, 'H').X);
        var reset = varied.WithVariations(FontVariationSettings.Default);
        Assert.Equal(100, Ink(reset, 'H').X);
        Assert.Equal(100, Ink(root.WithLanguage("TRK "), 'H').X);
    }
    [Fact]
    public void MalformedCompactBlendCountsRejectTheGlyphAndRetainTheUsableFace() {
        var root = TrueTypeFont.TryLoad(Bytes("variable-malformed.otf"))!;
        var canvas = new RgbaCanvas(180, 100, 4, root);
        Assert.False(root.Draw(canvas, 10, 10, "H", ChartColor.Black, 40));
        Assert.True(root.Draw(canvas, 10, 10, "A", ChartColor.Black, 40));
        Assert.Contains(canvas.ToOutputPixels().Where((_, i) => i % 4 == 3), alpha => alpha > 0);
    }

    [Fact]
    public void CompositionAndInheritedSvgAxesUseTheSameGlyphsAndReset() {
        WithRegisteredFont(path => {
            var style = new TextStyle { Font = FontSpec.FromFile(path).WithVariation("wght", 650), FontSize = 40, Color = ChartColor.Black, Hinting = TextHinting.None };
            Assert.Equal(54, TextLayoutEngine.Measure("HH", style).Width);
            var direct = ImageComposition.Create(180, 100, ChartColor.Transparent).DrawText(10, 0, 160, "HH", style, TextWrapMode.NoWrap).ToImage();
            var svg = "<svg xmlns='http://www.w3.org/2000/svg' width='180' height='100'><g font-family='CFX Variation' font-size='40' style=\"font-variation-settings:'wght' 650\"><text x='10' y='33.5'>HH</text></g></svg>";
            var image = SvgRasterizer.ToImage(svg);
            Assert.Equal(direct.Pixels, image.Pixels);
            var nested = svg.Replace("HH</text>", "H<tspan style='font-variation-settings:normal'>H</tspan></text>");
            Assert.NotEqual(image.Pixels, SvgRasterizer.ToImage(nested).Pixels);
            Assert.True(TypographyCss.TryVariations("'wght' 650, \"wdth\" 80", out var settings)); Assert.Equal(80, settings!["wdth"]);
            Assert.False(TypographyCss.TryVariations("'wght' NaN", out _));
        });
    }
    [Fact]
    public void ChartGridAndRadialRolesCarryAxisControlsToStaticAndHtmlOutput() {
        WithRegisteredFont(_ => {
            var grid = new ChartGrid { Title = "HHHH" }; grid.Add(Chart.Create());
            grid.TitleStyle.WithFontFamily("CFX Variation").WithVariation("wght", 650).WithOpenTypeLanguage("TRK");
            var svg = new SvgChartGridRenderer().Render(grid); Assert.Contains("font-variation-settings:", svg); Assert.Contains("font-language-override:", svg);
            Assert.Contains("font-variation-settings:", new HtmlChartGridRenderer().RenderFragment(grid));
            Assert.NotEmpty(new PngChartGridRenderer().Render(grid));
            var chart = Chart.Create().WithSize(360, 260).WithDataLabels().WithDataLabelStyle(s => s.WithFontFamily("CFX Variation").WithVariation("wdth", 125)).AddRadialBar("HHHH", new[] { new ChartPoint(0, 40) });
            Assert.Contains("font-variation-settings:", new SvgChartRenderer().Render(chart)); Assert.NotEmpty(new PngChartRenderer().Render(chart));
        });
    }
    private static void WithRegisteredFont(Action<string> action) {
        var path = Path.Combine(Path.GetTempPath(), "cfx-var-" + Guid.NewGuid().ToString("N") + ".ttf");
        File.WriteAllBytes(path, Bytes());
        try { FontRegistry.Register("CFX Variation", path); action(path); }
        finally { FontRegistry.Clear(); File.Delete(path); }
    }
}
