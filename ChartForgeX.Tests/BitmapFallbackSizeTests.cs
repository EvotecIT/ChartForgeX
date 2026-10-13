using ChartForgeX.Primitives;
using ChartForgeX.Raster;
using ChartForgeX.Rendering;
using ChartForgeX.Typography;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>The existing no-outline canvas path must honor logical text size on fontless hosts.</summary>
public sealed class BitmapFallbackSizeTests {
    [Theory]
    [InlineData(.5)]
    [InlineData(1)]
    [InlineData(3.5)]
    public void PreparedScenePaintsItsSelectedBitmapFaceAtTheRetainedSize(double size) {
        var width = TinyFont.AdvanceFor('H') * 3 * size / 8;
        var height = TinyFont.Height * size / 8;
        var prepared = new VisualScenePreparedText(new ResolvedTypeface(null, false, false), new TextStyle(), size, height,
            new[] { new VisualSceneTextLine("HHH", width, Array.Empty<ShapedGlyph>()) }, new TextMetrics(width, height, height));
        var text = new VisualSceneText(prepared, 4, 4 + height, ChartColor.Black, TextAlignment.Left, "caption", "bitmap-caption");
        var scene = new VisualScene(new VisualSize(40, 16), new[] { text }, Array.Empty<VisualDiagnostic>(), Array.Empty<VisualSemanticRegion>());
        var image = VisualSceneRasterRenderer.Render(scene, scale: 8, supersampling: 2);
        var ink = Enumerable.Range(0, image.Pixels.Length / 4).Where(index => image.Pixels[index * 4 + 3] > 0).ToArray();
        Assert.NotEmpty(ink);
        Assert.True(ink.Max(index => index / image.Width) + 1 <= Math.Ceiling((4 + height) * 8) + 1);
        Assert.True(ink.Max(index => index % image.Width) + 1 <= Math.Ceiling((4 + width) * 8) + 1);
    }

    [Theory]
    [InlineData(.5, false)]
    [InlineData(1, false)]
    [InlineData(1, true)]
    [InlineData(3.5, false)]
    [InlineData(8, false)]
    [InlineData(32, false)]
    public void LogicalSizeScalesBothBitmapAdvanceAndActualRaster(double size, bool bold) {
        var canvas = new RgbaCanvas(160, 40, 2, null, 8, useDefaultOutlineFont: false);
        var ordinaryWidth = canvas.MeasureTextWidth("HHH", 8);
        Assert.Equal(ordinaryWidth * size / 8, canvas.MeasureTextWidth("HHH", size), 6);
        if (bold) canvas.DrawTextEmphasized(4, 4, "HHH", ChartColor.Black, size);
        else canvas.DrawText(4, 4, "HHH", ChartColor.Black, size);
        var pixels = canvas.ToOutputPixels();
        var width = canvas.OutputWidth;
        var ink = Enumerable.Range(0, pixels.Length / 4).Where(index => pixels[index * 4 + 3] > 0).ToArray();
        Assert.NotEmpty(ink);
        var top = ink.Min(index => index / width); var bottom = ink.Max(index => index / width) + 1;
        var right = ink.Max(index => index % width) + 1;
        Assert.True(bottom - top <= Math.Ceiling(size * 8) + 1, "Bitmap glyph height must scale with the requested logical size.");
        Assert.True(right <= Math.Ceiling((4 + canvas.MeasureTextWidth("HHH", size) + (bold ? .25 : 0)) * 8) + 1);
        var directory = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (!string.IsNullOrWhiteSpace(directory)) {
            Directory.CreateDirectory(directory);
            File.WriteAllBytes(Path.Combine(directory, "bitmap-fallback-" + size.ToString(System.Globalization.CultureInfo.InvariantCulture) + "-" + bold + ".png"),
                PngWriter.WriteRgba(new RgbaImage(canvas.OutputWidth, canvas.OutputHeight, pixels)));
        }
    }
}
