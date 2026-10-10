using System.Buffers.Binary;
using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Html;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;
using ChartForgeX.Svg;
using ChartForgeX.SvgRaster;
using ChartForgeX.Typography;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Palette selection, default fallback, style inheritance and shared-table bounds.</summary>
[Collection(nameof(FontRegistryCollection))]
public sealed class FontPaletteTests {
    private static byte[] Bytes(string name = "color-palettes") {
        using var stream = typeof(FontPaletteTests).Assembly.GetManifestResourceStream("ChartForgeX.Tests.Fixtures.OpenType." + name + ".ttf")!;
        using var buffer = new MemoryStream(); stream.CopyTo(buffer); return buffer.ToArray();
    }
    private static TrueTypeFont Font(string name = "color-palettes") => TrueTypeFont.TryLoad(Bytes(name))!;
    private static RgbaCanvas Draw(TrueTypeFont face, string text = "😀", int density = 1, ChartColor? foreground = null) {
        var canvas = new RgbaCanvas(220, 160, 1, face, density, useDefaultOutlineFont: false);
        Assert.True(face.Draw(canvas, 40, 0, text, foreground ?? new ChartColor(0, 255, 0), 100)); return canvas;
    }
    private static byte[] Pixel(RgbaCanvas canvas, int x, int y) => canvas.ToOutputPixels().Skip((y * canvas.OutputWidth + x) * 4).Take(4).ToArray();

    [Theory]
    [InlineData("color-colr0")]
    [InlineData("color-palettes")]
    public void AlternatePaletteChangesLayersAndKeepsForegroundAndAdvances(string name) {
        var face = Font(name); var selected = face.WithColorPalette(1); var canvas = Draw(selected);
        Assert.Equal(new byte[] { 255, 255, 0, 255 }, Pixel(canvas, 30, 60));
        Assert.Equal(new byte[] { 0, 255, 0, 255 }, Pixel(canvas, 45, 35));
        Assert.Equal(face.Measure("😀", 100), selected.Measure("😀", 100));
        Assert.Equal(Draw(face).ToOutputPixels(), Draw(selected.WithColorPalette(0)).ToOutputPixels());
        Assert.Equal(new byte[] { 255, 0, 0, 255 }, Pixel(Draw(face), 30, 60));
    }
    [Theory]
    [InlineData("😁")]
    [InlineData("😂")]
    [InlineData("😃")]
    [InlineData("👩‍💻")]
    public void GradientAndZwjPaintsUseSelectedPalette(string text) {
        var face = Font(); var a = Draw(face, text); var b = Draw(face.WithColorPalette(1), text);
        Assert.NotEqual(a.ToOutputPixels(), b.ToOutputPixels());
        Assert.Equal(face.Measure(text, 100), face.WithColorPalette(1).Measure(text, 100));
        Assert.Contains((byte)255, b.ToOutputPixels());
    }
    [Theory]
    [InlineData(1)] [InlineData(2)]
    public void PaletteAlphaAndForegroundOpacityApplyOnceAtEachOutputDensity(int density) {
        var canvas = Draw(Font().WithColorPalette(2), density: density, foreground: new ChartColor(0, 255, 0, 128));
        Assert.Equal(new byte[4], Pixel(canvas, 30 * density, 60 * density));
        Assert.Equal(new byte[] { 0, 255, 0, 128 }, Pixel(canvas, 45 * density, 35 * density));
        Assert.Equal((byte)16, Pixel(canvas, 80 * density, 60 * density)[3]);
    }
    [Theory]
    [InlineData("color-palettes")]
    [InlineData("color-sbix")]
    [InlineData("color-cbdt1")]
    public void MissingPalettesAndBitmapStrikesKeepDefaultOutput(string name) {
        var face = Font(name);
        Assert.Equal(Draw(face).ToOutputPixels(), Draw(face.WithColorPalette(65535)).ToOutputPixels());
    }
    [Fact]
    public void ManySharedPalettesRetainOneRecordStoreAndMalformedAlternatesUseDefault() {
        const int palettes = 65535; var bytes = new byte[12 + palettes * 2 + 12];
        void U16(int at, int value) => BinaryPrimitives.WriteUInt16BigEndian(bytes.AsSpan(at), (ushort)value);
        U16(2, 3); U16(4, palettes); U16(6, 3);
        BinaryPrimitives.WriteUInt32BigEndian(bytes.AsSpan(8), (uint)(bytes.Length - 12));
        bytes[^10] = 255; bytes[^9] = 255; // Default red BGRA record.
        U16(12 + 400 * 2, 65535);
        var before = GC.GetAllocatedBytesForCurrentThread();
        var data = FontColorPalettes.Read(new FontTableReader(bytes, 0, bytes.Length))!;
        Assert.InRange(GC.GetAllocatedBytesForCurrentThread() - before, 0, 1024 * 1024);
        Assert.Equal(new ChartColor(255, 0, 0), data.Color(0, 400));
        Assert.Equal(data.Color(0, 0), data.Color(0, palettes - 1));
        Assert.Equal(data.Color(0, 0), data.Color(0, 65535));
    }
    [Fact]
    public void PaletteOverridesInheritResetCloneAndValidateWithoutMutatingFallback() {
        var style = new TextStyle { Font = FontSpec.SystemSans().WithColorPalette(2) };
        Assert.Equal(2, style.Clone().Font.ColorPaletteIndex);
        Assert.Equal(2, new TextStyleOverride().Resolve(style).Font.ColorPaletteIndex);
        Assert.Equal(0, new TextStyleOverride().WithColorPalette(0).Resolve(style).Font.ColorPaletteIndex);
        Assert.Equal(2, style.Font.ColorPaletteIndex);
        Assert.Throws<ArgumentOutOfRangeException>(() => style.Font.ColorPaletteIndex = -1);
        Assert.Throws<ArgumentOutOfRangeException>(() => new TextStyleOverride().WithColorPalette(65536));
        Assert.True(new TextStyleOverride().WithColorPalette(0).HasOverrides);
    }
    [Fact]
    public void LanguageVariationAndFallbackViewsKeepPaletteSelection() {
        var face = Font().WithColorPalette(1).WithLanguage("TRK ").WithVariations(FontVariationSettings.Default.WithAxis("wght", 700)).WithFallbackFamilies(new[] { "CFX Colour" });
        Assert.Equal(1, face.ColorPaletteIndex);
        Assert.Equal(new byte[] { 255, 255, 0, 255 }, Pixel(Draw(face), 30, 60));
        WithRegisteredFont(path => {
            var resolved = TypographyFontResolver.ResolveFace(FontSpec.FromFile(path).WithColorPalette(1));
            Assert.Equal(1, resolved.Font!.ColorPaletteIndex);
        });
    }
    [Fact]
    public void WholeClusterFallbackRetainsPaletteWithoutContaminatingOtherRuns() {
        WithRegisteredFont(_ => {
            var primary = Font("language").WithFallbackFamilies(new[] { "CFX Palette" }).WithColorPalette(1);
            Assert.False(primary.HasGlyph('A'));
            var glyphs = TextShaper.Shape(primary, "A");
            Assert.Single(glyphs); Assert.Equal(1, glyphs[0].Face.ColorPaletteIndex);
            Assert.Contains(Draw(primary, "A").ToOutputPixels().Chunk(4), p => p.SequenceEqual(new byte[] { 255, 255, 0, 255 }));
            Parallel.For(0, 30, i => Assert.Contains(Draw(primary.WithColorPalette(i % 2), "A").ToOutputPixels().Chunk(4), p => p.SequenceEqual(i % 2 == 0 ? new byte[] { 255, 0, 0, 255 } : new byte[] { 255, 255, 0, 255 })));
        });
    }
    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void FittedAndRotatedTextKeepTheSelectedPalette(bool rotated) {
        var face = Font().WithColorPalette(1); var canvas = new RgbaCanvas(220, 220, 1, face, 2, useDefaultOutlineFont: false);
        if (rotated) canvas.DrawTextRotated(140, 40, "😀", ChartColor.Black, 32, 90, 0, 0);
        else canvas.DrawTextFitted(40, 40, "😀", ChartColor.Black, 32, 20, face);
        Assert.Contains(canvas.ToOutputPixels().Chunk(4), p => p[0] > 200 && p[1] > 200 && p[2] < 10 && p[3] > 200);
    }
    [Fact]
    public void NativePaletteRulesSafelyRoundTripQuotedFamiliesAndSharedNames() {
        const string family = "CFX \"Palette\";{}<&";
        var rules = TypographyPaletteCss.Rules("sans-serif", new TextStyleOverride().WithFontFamily("'" + family + "'").WithColorPalette(2));
        Assert.DoesNotContain("<", rules);
        WithRegisteredFont(path => {
            FontRegistry.Register(family, path);
            var registered = TrueTypeFont.TryLoadFromPath(path)!.WithSelectedFamily(family);
            var sheet = SvgRasterStyleSheet.Parse(new[] { rules });
            Assert.Equal(2, sheet.PaletteContext(TypographyPaletteCss.Name(2))!.Resolve(registered));
            var shared = SvgRasterStyleSheet.Parse(new[] { "@font-palette-values --shared{font-family:'Missing','CFX Palette';base-palette:1}@font-palette-values --shared{font-family:'Different';base-palette:2}" });
            Assert.Equal(1, shared.PaletteContext("--shared")!.Resolve(registered.WithSelectedFamily("CFX Palette")));
            Assert.Equal(0, shared.PaletteContext("--shared")!.Resolve(Font("language")));
        });
    }
    [Fact]
    public void SvgPaletteRulesInheritanceAndNormalResetReachRasterGlyphs() {
        WithRegisteredFont(_ => {
            var svg = "<svg xmlns='http://www.w3.org/2000/svg' width='440' height='160'><style>@font-palette-values --warm{font-family:'CFX Palette';base-palette:1}</style><g font-family='CFX Palette' font-size='100' fill='#00ff00' style='font-palette:--warm'><text x='40' y='80'>😀<tspan style='font-palette:normal'>😀</tspan></text></g></svg>";
            var actual = SvgRasterizer.ToImage(svg);
            var canvas = new RgbaCanvas(440, 160, 1, Font(), 1, useDefaultOutlineFont: false);
            Font().WithColorPalette(1).Draw(canvas, 40, 0, "😀", new ChartColor(0, 255, 0), 100);
            Font().Draw(canvas, 140, 0, "😀", new ChartColor(0, 255, 0), 100);
            Assert.Equal(canvas.ToOutputPixels(), actual.Pixels);
            var wrongFamily = svg.Replace("font-family:'CFX Palette';base-palette:1", "font-family:'Different';base-palette:1");
            Assert.NotEqual(actual.Pixels, SvgRasterizer.ToImage(wrongFamily).Pixels);
        });
    }
    [Fact]
    public void ImportedPaletteRuleMatchesTheActualFaceWhenEarlierStackFamiliesAreMissing() {
        WithRegisteredFont(_ => {
            const string rules = "@font-palette-values --selected{font-family:Missing;base-palette:2}@font-palette-values --selected{font-family:'CFX Palette';base-palette:1}";
            var svg = "<svg xmlns='http://www.w3.org/2000/svg' width='220' height='160'><style>" + rules + "</style><text x='40' y='80' font-family='Missing, CFX Palette' font-size='100' fill='#00ff00' style='font-palette:--selected'>😀</text></svg>";
            Assert.Equal(Draw(Font().WithColorPalette(1)).ToOutputPixels(), SvgRasterizer.ToImage(svg).Pixels);
            var primaryPath = Path.Combine(Path.GetTempPath(), "cfx-palette-primary-" + Guid.NewGuid().ToString("N") + ".ttf");
            File.WriteAllBytes(primaryPath, Bytes("language"));
            try {
                FontRegistry.Register("CFX Primary", primaryPath);
                var fallback = svg.Replace("Missing, CFX Palette", "CFX Primary, CFX Palette").Replace("😀</text>", "A</text>");
                Assert.Equal(Draw(Font().WithColorPalette(1), "A").ToOutputPixels(), SvgRasterizer.ToImage(fallback).Pixels);
            } finally { File.Delete(primaryPath); }
        });
    }
    [Fact]
    public void SeriesAndPointLabelStylesEmitTheirEffectivePaletteDefinitions() {
        var chart = Chart.Create().WithDataLabels().AddBar("Observed", new[] { new ChartPoint(1, 5), new ChartPoint(2, 4) });
        chart.Series[0].ConfigureDataLabelStyle(s => s.WithFontFamily("CFX Series").WithColorPalette(3));
        chart.Series[0].ConfigurePointDataLabelStyle(0, s => s.WithFontFamily("CFX Point").WithColorPalette(4));
        var svg = chart.ToSvg();
        Assert.Contains("base-palette:3", svg); Assert.Contains("base-palette:4", svg);
        Assert.Contains("font-palette:--cfx-font-palette-3", svg);
        Assert.Contains("font-palette:--cfx-font-palette-4", svg);
    }
    [Fact]
    public void ImportedPaletteKeepsTheChosenFamilyAfterFileCacheEviction() {
        WithRegisteredFont(path => {
            var svg = PaletteSvg("CFX Palette");
            var expected = SvgRasterizer.ToImage(svg).Pixels;
            var directory = Path.Combine(Path.GetTempPath(), "cfx-palette-eviction-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            try {
                for (var i = 0; i < 130; i++) {
                    var copy = Path.Combine(directory, i + ".ttf"); File.Copy(path, copy);
                    TextLayoutEngine.Measure("😀", new TextStyle { Font = FontSpec.FromFile(copy), FontSize = 100 });
                }
                Assert.Equal(expected, SvgRasterizer.ToImage(svg).Pixels);
            } finally { Directory.Delete(directory, recursive: true); }
        });
    }
    [Fact]
    public void NamedPalettesDistinguishRegisteredAliasesOfOnePhysicalFont() {
        WithRegisteredFont(path => {
            FontRegistry.Register("CFX Alias", path);
            var svg = PaletteSvg("CFX Palette").Replace("</style>", "@font-palette-values --selected{font-family:'CFX Alias';base-palette:2}</style>");
            Assert.Equal(Draw(Font().WithColorPalette(1)).ToOutputPixels(), SvgRasterizer.ToImage(svg).Pixels);
            var alias = svg.Replace("font-family='CFX Palette'", "font-family='CFX Alias'");
            Assert.Equal(Draw(Font().WithColorPalette(2)).ToOutputPixels(), SvgRasterizer.ToImage(alias).Pixels);
            Assert.Equal(Draw(Font()).ToOutputPixels(), SvgRasterizer.ToImage(PaletteSvg("CFX Alias")).Pixels);
        });
    }
    [Fact]
    public void FallbackPaletteUsesItsSelectedAliasThroughLanguageAndVariationViews() {
        WithRegisteredFont(path => {
            FontRegistry.Register("CFX Alias", path);
            var primary = Path.Combine(Path.GetTempPath(), "cfx-palette-primary-" + Guid.NewGuid().ToString("N") + ".ttf");
            File.WriteAllBytes(primary, Bytes("language"));
            try {
                FontRegistry.Register("CFX Primary", primary);
                var svg = PaletteSvg("CFX Primary, CFX Alias").Replace("</style>", "@font-palette-values --selected{font-family:'CFX Alias';base-palette:2}</style>").Replace("😀</text>", "A</text>").Replace("font-size='100'", "font-size='100' style='font-language-override:\"TRK\";font-variation-settings:\"wght\" 700'");
                Assert.Equal(Draw(Font().WithColorPalette(2), "A").ToOutputPixels(), SvgRasterizer.ToImage(svg).Pixels);
            } finally { File.Delete(primary); }
        });
    }
    private static string PaletteSvg(string family) => "<svg xmlns='http://www.w3.org/2000/svg' width='220' height='160'><style>@font-palette-values --selected{font-family:'CFX Palette';base-palette:1}</style><text x='40' y='80' font-family='" + family + "' font-size='100' fill='#00ff00' font-palette='--selected'>😀</text></svg>";
    [Fact]
    public void ChartGridPaginationAndHtmlExportNativePaletteRules() {
        WithRegisteredFont(_ => {
            var chart = Chart.Create().WithTitle("😀").ConfigureTitleStyle(s => s.WithFontFamily("CFX Palette").WithColorPalette(1));
            var svg = chart.ToSvg();
            Assert.Contains("@font-palette-values --cfx-font-palette-1", svg);
            Assert.Contains("font-palette:--cfx-font-palette-1", svg);
            var grid = new ChartGrid { Title = "😀", Subtitle = "😀" }; grid.Add(chart);
            grid.TitleStyle.WithFontFamily("CFX Palette").WithColorPalette(2); grid.SubtitleStyle.WithColorPalette(0);
            var page = Assert.Single(grid.Paginate(1)); Assert.Equal(2, page.Grid.TitleStyle.ColorPaletteIndex);
            var exported = page.Grid.ToSvg(); Assert.Contains("base-palette:2", exported);
            Assert.Contains("font-palette:normal", exported);
            Assert.DoesNotContain("base-palette:0", exported);
            Assert.Contains("base-palette:2", new HtmlChartGridRenderer().RenderFragment(page.Grid));
            Assert.NotEmpty(page.Grid.ToPng());
        });
    }
    private static void WithRegisteredFont(Action<string> action) {
        var path = Path.Combine(Path.GetTempPath(), "cfx-palettes-" + Guid.NewGuid().ToString("N") + ".ttf"); File.WriteAllBytes(path, Bytes());
        try { FontRegistry.Register("CFX Palette", path); action(path); }
        finally { FontRegistry.Clear(); File.Delete(path); }
    }
}
