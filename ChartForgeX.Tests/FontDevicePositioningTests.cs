using ChartForgeX.Raster;
using ChartForgeX.Typography;
using ChartForgeX.Primitives;
using ChartForgeX.Composition;
using ChartForgeX.SvgRaster;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Font-authored pixel-size corrections must reach the shared measurement and glyph positions.</summary>
[Collection(nameof(FontRegistryCollection))]
public sealed class FontDevicePositioningTests {
    private static byte[] Bytes() {
        using var source = typeof(FontDevicePositioningTests).Assembly.GetManifestResourceStream("ChartForgeX.Tests.Fixtures.OpenType.device-positioning.ttf")!;
        using var bytes = new MemoryStream(); source.CopyTo(bytes); return bytes.ToArray();
    }
    private static TrueTypeFont Face() {
        return TrueTypeFont.TryLoad(Bytes())!;
    }

    [Theory]
    [InlineData("H", 11, 6.93)]
    [InlineData("H", 12, 9.56)]
    [InlineData("H", 13, 6.19)]
    [InlineData("IJ", 12, 16.92)]
    [InlineData("IJ", 13, 11.08)]
    [InlineData("IJ", 14, 23.24)]
    [InlineData("KM", 12, 33.8)]
    [InlineData("KM", 13, -10.05)]
    [InlineData("KM", 14, 143.1)]
    [InlineData("O", 12, 12.68)]
    [InlineData("P", 12, 3.48)]
    [InlineData("H", 12.5, 630 * .0125 - 2 * 12.5 / 13)]
    public void PublicFaceMeasurementUsesSignedDeviceAdvanceAtEachSize(string text, double size, double expectedWidth) {
        Assert.Equal(expectedWidth, Face().Measure(text, size), 6);
    }

    [Fact]
    public void ConcurrentMeasurementsDoNotReuseAnotherPixelSizesCorrections() {
        var face = Face();
        Parallel.For(0, 100, i => Assert.Equal(i % 2 == 0 ? 9.56 : 6.19, face.Measure("H", i % 2 == 0 ? 12 : 13), 6));
    }

    [Theory]
    [InlineData(12, 1.12, -.76)]
    [InlineData(13, -1.87, 1.26)]
    [InlineData(20, -1.8, .4)]
    [InlineData(21, 1.21, .42)]
    public void DevicePlacementReadsSignedTwoBitValuesAcrossPackedWords(double size, double x, double y) {
        var glyph = Assert.Single(TextShaper.Shape(Face(), "H", size));
        Assert.Equal(x, glyph.OffsetX * size / 1000, 6);
        Assert.Equal(y, glyph.OffsetY * size / 1000, 6);
    }

    [Theory]
    [InlineData("H\u0301", new double[] { 1.12, 7 }, new double[] { -.76, 8.44 })]
    [InlineData("H\u0301\u0300", new double[] { 1.12, 7, 4 }, new double[] { -.76, 8.44, 12.64 })]
    [InlineData("HI\u0301", new double[] { 0, 13.08 }, new double[] { 0, 8.2 })]
    [InlineData("بت", new double[] { 0, -8.4 }, new double[] { -1.8, 0 })]
    public void AnchorCorrectionsReachBaseLigatureStackedMarkAndCursivePositions(string text, double[] x, double[] y) {
        var glyphs = TextShaper.Shape(Face(), text, 12); Assert.Equal(x.Length, glyphs.Count);
        var pen = 0.0;
        for (var i = 0; i < glyphs.Count; i++) {
            Assert.Equal(x[i], pen + glyphs[i].OffsetX * .012, 6);
            Assert.Equal(y[i], glyphs[i].OffsetY * .012, 6);
            pen += glyphs[i].Advance!.Value * .012;
        }
    }

    [Theory]
    [InlineData(4, false, false)]
    [InlineData(0x8000, false, false)]
    [InlineData(1, true, false)]
    [InlineData(1, false, true)]
    public void InvalidOptionalDeviceDataKeepsUsableDesignPositioning(int format, bool reversedRange, bool invalidOffset) {
        var bytes = Bytes();
        var gpos = 0;
        for (var i = 0; i < Read16(bytes, 4); i++) {
            var record = 12 + i * 16;
            if (System.Text.Encoding.ASCII.GetString(bytes, record, 4) == "GPOS") gpos = Read32(bytes, record + 8);
        }
        var list = gpos + Read16(bytes, gpos + 8);
        var lookup = list + Read16(bytes, list + 2);
        var single = lookup + Read16(bytes, lookup + 6);
        var valueFormat = Read16(bytes, single + 4);
        var field = single + 6;
        for (var bit = 1; bit <= 8; bit <<= 1) if ((valueFormat & bit) != 0) field += 2;
        var device = single + Read16(bytes, field);
        Write16(bytes, device + 4, format);
        if (reversedRange) Write16(bytes, device + 2, 1);
        if (invalidOffset) Write16(bytes, field, 65535);
        var face = TrueTypeFont.TryLoad(bytes)!;
        var glyph = Assert.Single(TextShaper.Shape(face, "H", 12));
        Assert.Equal(.12, glyph.OffsetX * .012, 6);
        Assert.Equal(-.76, glyph.OffsetY * .012, 6);
        Assert.Equal(9.56, face.Measure("H", 12), 6);
    }
    private static int Read16(byte[] bytes, int at) => bytes[at] * 256 + bytes[at + 1];
    private static int Read32(byte[] bytes, int at) => (Read16(bytes, at) << 16) | Read16(bytes, at + 2);
    private static void Write16(byte[] bytes, int at, int value) { bytes[at] = (byte)(value >> 8); bytes[at + 1] = (byte)value; }

    [Fact]
    public void CompositionAndMixedSizeSvgSpansUseTheSameCorrectionsAsDirectDrawing() {
        var path = Path.Combine(Path.GetTempPath(), "cfx-device-" + Guid.NewGuid().ToString("N") + ".ttf");
        File.WriteAllBytes(path, Bytes());
        try {
            var face = Face(); var style = new TextStyle { Font = FontSpec.FromFile(path), FontSize = 12, Color = ChartColor.Black, Hinting = TextHinting.None };
            Assert.Equal(19.12, TextLayoutEngine.Measure("HH", style).Width, 6);
            var composed = ImageComposition.Create(100, 100, ChartColor.Transparent).DrawText(0, 0, 100, "H\u0301\u0300", style, TextWrapMode.NoWrap).ToImage();
            var direct = new RgbaCanvas(100, 100, 1, face, 1, useDefaultOutlineFont: false) { TextHinting = TextHinting.None };
            face.Draw(direct, 0, 0, "H\u0301\u0300", ChartColor.Black, 12);
            Assert.Equal(direct.ToOutputPixels(), composed.Pixels);
            FontRegistry.Register("CFX Device SVG", path);
            var svg = "<svg xmlns='http://www.w3.org/2000/svg' width='100' height='100'><text x='0' y='20' fill='black' font-family='CFX Device SVG' font-size='12'>H&#x301;<tspan font-size='13'>H&#x301;</tspan></text></svg>";
            Assert.True(SvgRasterRenderer.TryRenderDocument(svg, null, 100, 100, out var actual));
            var expected = new RgbaCanvas(100, 100, 1, face, 1, useDefaultOutlineFont: false);
            face.Draw(expected, 0, 20 - face.Ascent(12), "H\u0301", ChartColor.Black, 12);
            face.Draw(expected, 9.56, 20 - face.Ascent(13), "H\u0301", ChartColor.Black, 13);
            // SVG's temporary surface interpolation can round fractional coverage differently.
            // A lost size correction moves whole glyph edges and exceeds this per-pixel bound.
            var expectedPixels = expected.ToOutputPixels();
            var maximumCoverageDifference = 0;
            var totalCoverageDifference = 0; var totalCoverage = 0;
            for (var i = 3; i < actual.Length; i += 4) {
                var difference = Math.Abs(actual[i] - expectedPixels[i]);
                maximumCoverageDifference = Math.Max(maximumCoverageDifference, difference);
                totalCoverageDifference += difference; totalCoverage += expectedPixels[i];
            }
            Assert.InRange(maximumCoverageDifference, 0, 64);
            Assert.True(totalCoverageDifference <= totalCoverage * .05, $"SVG fractional coverage difference: {totalCoverageDifference}/{totalCoverage}");
        } finally { FontRegistry.Clear(); File.Delete(path); }
    }
}
