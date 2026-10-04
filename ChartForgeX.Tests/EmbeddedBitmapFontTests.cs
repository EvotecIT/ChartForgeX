using System.Buffers.Binary;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;
using ChartForgeX.Typography;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class EmbeddedBitmapFontTests {
    private static byte[] Bytes(int index, int image, int depth = 1) {
        using var source = typeof(EmbeddedBitmapFontTests).Assembly.GetManifestResourceStream($"ChartForgeX.Tests.Fixtures.OpenType.bitmap-{index}-{image}-{depth}.ttf")!;
        using var output = new MemoryStream(); source.CopyTo(output); return output.ToArray();
    }
    private static TrueTypeFont Font(int index = 1, int image = 1, int depth = 1) => TrueTypeFont.TryLoad(Bytes(index, image, depth))!;
    private static RgbaCanvas Draw(TrueTypeFont face, TextHinting hinting, double size = 12, int density = 1, int antialias = 1, ChartColor? color = null, bool italic = false) {
        var canvas = new RgbaCanvas(40, 24, antialias, face, density, false) { TextHinting = hinting };
        Assert.True(face.Draw(canvas, 8, 0.4, "A", color ?? ChartColor.Black, size, italic));
        return canvas;
    }
    private static byte[] Crop(RgbaCanvas canvas, int x, int y, int width, int height) {
        var pixels = canvas.ToOutputPixels();
        return Enumerable.Range(y, height).SelectMany(row => Enumerable.Range(x, width).Select(col => pixels[(row * canvas.OutputWidth + col) * 4 + 3])).ToArray();
    }

    [Theory]
    [InlineData(1, 1)] [InlineData(3, 2)] [InlineData(2, 5)]
    [InlineData(4, 6)] [InlineData(5, 5)] [InlineData(3, 7)]
    public void FullPaintsAuthoredCoverageForAllObservedIndexAndImageLayouts(int index, int image) {
        var face = Font(index, image);
        Assert.False(face.IsColorFace);
        Assert.Equal(7.2, face.Measure("A", 12), 6);
        Assert.Equal(new byte[] { 0,255,0,255,255, 255,0,255,0,255, 255,255,0,255,0 }, Crop(Draw(face, TextHinting.Full), 6, 3, 5, 3));
    }

    [Theory]
    [InlineData(2)] [InlineData(4)] [InlineData(8)]
    public void GrayscaleCoverageReceivesForegroundOpacityOnce(int depth) {
        var canvas = Draw(Font(3, 7, depth), TextHinting.Full, color: new ChartColor(10,20,30,128));
        var mask = (1 << depth) - 1;
        var expected = Enumerable.Range(0, 3).SelectMany(y => Enumerable.Range(0, 5).Select(x =>
            (byte)((((x + y) % (mask + 1) * 255 + mask / 2) / mask * 128 + 127) / 255))).ToArray();
        Assert.Equal(expected, Crop(canvas, 6, 3, 5, 3));
    }

    [Fact]
    public void ExactStrikeUsesFinalOutputDensityIndependentlyOfAntialiasing() {
        var face = Font();
        Assert.Equal(Crop(Draw(face, TextHinting.Full), 6, 3, 5, 3), Crop(Draw(face, TextHinting.Full, size: 6, density: 2, antialias: 4), 14, 3, 5, 3));
        Assert.Equal(Draw(face, TextHinting.Full).ToOutputPixels(), Draw(face, TextHinting.Full, antialias: 4).ToOutputPixels());
    }

    [Theory]
    [InlineData(1, 1)] [InlineData(1, 4)]
    [InlineData(4, 1)] [InlineData(4, 4)]
    public void PresentationDensityPreservesTransparentCellsAndComposesCoverageOnce(int depth, int antialias) {
        var face = depth == 1 ? Font() : Font(3, 7, depth);
        var actual = Crop(Draw(face, TextHinting.Full, size: 4, density: 3, antialias: antialias, color: new ChartColor(10,20,30,128)), 22, 4, 5, 3);
        var expected = Crop(Draw(face, TextHinting.Full, color: new ChartColor(10,20,30,128)), 6, 3, 5, 3);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void AutoNoneFractionalSizesAndSyntheticSlantsKeepOutlineRendering() {
        var face = Font();
        var noBitmap = Bytes(1, 1);
        // Turn the optional EBDT version into an unsupported version, preserving the outline face.
        var offset = Table(noBitmap, "EBDT"); noBitmap[offset] = 0x7F;
        var outline = TrueTypeFont.TryLoad(noBitmap)!;
        foreach (var mode in new[] { TextHinting.Auto, TextHinting.None })
            Assert.Equal(Draw(outline, mode).ToOutputPixels(), Draw(face, mode).ToOutputPixels());
        Assert.Equal(Draw(outline, TextHinting.Full, 12.5).ToOutputPixels(), Draw(face, TextHinting.Full, 12.5).ToOutputPixels());
        Assert.Equal(Draw(outline, TextHinting.Full, italic: true).ToOutputPixels(), Draw(face, TextHinting.Full, italic: true).ToOutputPixels());
        Assert.Equal(Draw(outline, TextHinting.Full).OutputWidth, Draw(face, TextHinting.Full).OutputWidth);
    }

    [Fact]
    public void TruncatedBitmapRecordFallsBackToUsableOutline() {
        var bytes = Bytes(1, 1);
        var index = Table(bytes, "EBLC");
        BinaryPrimitives.WriteUInt32BigEndian(bytes.AsSpan(index + 76, 4), 5);
        var malformed = TrueTypeFont.TryLoad(bytes)!;
        bytes[Table(bytes, "EBDT")] = 0x7F;
        var outline = TrueTypeFont.TryLoad(bytes)!;
        Assert.Equal(Draw(outline, TextHinting.Full).ToOutputPixels(), Draw(malformed, TextHinting.Full).ToOutputPixels());
    }

    [Fact]
    public void SyntheticBoldAndSelectedVariableInstancesRetainOutlines() {
        var bytes = Bytes(1, 1);
        var face = TrueTypeFont.TryLoad(bytes)!;
        bytes[Table(bytes, "EBDT")] = 0x7F;
        var outline = TrueTypeFont.TryLoad(bytes)!;
        RgbaCanvas Bold(TrueTypeFont selected) {
            var canvas = new RgbaCanvas(40, 24, 1, selected, 1, false) { TextHinting = TextHinting.Full };
            Assert.True(selected.Draw(canvas, 8, 0.4, "A", ChartColor.Black, 12, false, 1));
            return canvas;
        }
        Assert.Equal(Bold(outline).ToOutputPixels(), Bold(face).ToOutputPixels());
        using var source = typeof(EmbeddedBitmapFontTests).Assembly.GetManifestResourceStream("ChartForgeX.Tests.Fixtures.OpenType.bitmap-variable.ttf")!;
        using var variableBytes = new MemoryStream(); source.CopyTo(variableBytes);
        var variable = TrueTypeFont.TryLoad(variableBytes.ToArray())!;
        var selected = variable.WithVariations(FontVariationSettings.Default.WithAxis("wght", 700));
        Assert.Equal(Draw(outline, TextHinting.Full).ToOutputPixels(), Draw(selected, TextHinting.Full).ToOutputPixels());
        Assert.Equal(Draw(face, TextHinting.Full).ToOutputPixels(), Draw(variable, TextHinting.Full).ToOutputPixels());
    }

    [Fact]
    public void FittedAndRotatedBuffersRetainAuthoredInkOutsideOutlineBounds() {
        using var source = typeof(EmbeddedBitmapFontTests).Assembly.GetManifestResourceStream("ChartForgeX.Tests.Fixtures.OpenType.bitmap-overhang.ttf")!;
        using var bytes = new MemoryStream(); source.CopyTo(bytes);
        var face = TrueTypeFont.TryLoad(bytes.ToArray())!;
        var direct = new RgbaCanvas(80, 80, 1, face, 1, false) { TextHinting = TextHinting.Full };
        Assert.True(face.Draw(direct, 40, 30, "A", ChartColor.Black, 12));
        var fitted = new RgbaCanvas(80, 80, 1, face, 1, false) { TextHinting = TextHinting.Full };
        // The 7.2px advance exceeds 6px, exercising the intermediate fitting buffer.
        fitted.DrawTextFitted(40, 30, "A", ChartColor.Black, 12, 6);
        var fittedPixels = fitted.ToOutputPixels();
        var fittedAlpha = Enumerable.Range(0, fittedPixels.Length / 4).Sum(i => (int)fittedPixels[i * 4 + 3]);
        Assert.InRange(fittedAlpha, 1650, 1800); // Nine coverage cells compressed to 75% width, with sampling tolerance.
        Assert.Contains(Crop(fitted, 26, 18, 5, 3), alpha => alpha > 0);
        var rotated = new RgbaCanvas(80, 80, 1, face, 1, false) { TextHinting = TextHinting.Full };
        rotated.DrawTextRotated(40, 30, "A", ChartColor.Black, 12, 90, 0, 0);
        Assert.Equal(Enumerable.Range(0, direct.ToOutputPixels().Length / 4).Sum(i => (int)direct.ToOutputPixels()[i * 4 + 3]),
            Enumerable.Range(0, rotated.ToOutputPixels().Length / 4).Sum(i => (int)rotated.ToOutputPixels()[i * 4 + 3]));
    }

    private static int Table(byte[] data, string tag) {
        var count = BinaryPrimitives.ReadUInt16BigEndian(data.AsSpan(4, 2));
        for (var i = 0; i < count; i++) {
            var at = 12 + i * 16;
            if (System.Text.Encoding.ASCII.GetString(data, at, 4) == tag) return checked((int)BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(at + 8, 4)));
        }
        throw new InvalidOperationException(tag);
    }
}
