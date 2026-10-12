using ChartForgeX.Primitives;
using ChartForgeX.Raster;
using ChartForgeX.Typography;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class OutlineLogicalTextSizeTests {
    [Theory]
    [InlineData(.5, false)]
    [InlineData(6d / 7, false)]
    [InlineData(.5, true)]
    [InlineData(1, false)]
    public void CanvasMeasuresAndPaintsSubpixelOutlineTextAtItsRetainedLogicalSize(double size, bool italic) {
        var path = Path.Combine(AppContext.BaseDirectory, "Fixtures", "Fonts", "Carlito", "Carlito-Regular.ttf");
        var face = Assert.IsType<TrueTypeFont>(TrueTypeFont.TryLoadFromPath(path));
        var actual = new RgbaCanvas(32, 12, 16, face) { TextHinting = TextHinting.None };
        var expected = new RgbaCanvas(32, 12, 16, face) { TextHinting = TextHinting.None };
        actual.DrawText(4, 4, "HHH", ChartColor.Black, size, face, italic);
        Assert.True(face.Draw(expected, 4, 4, "HHH", ChartColor.Black, size, italic));
        Assert.Equal(expected.Pixels, actual.Pixels);
        Assert.Equal(face.Measure("HHH", size, italic), actual.MeasureTextWidth("HHH", size, italic), 10);
        Assert.Equal(face.Measure("HHH", size, italic), RgbaCanvas.MeasureTextWidth("HHH", size, face, italic), 10);
        Assert.Equal(face.Measure("HHH", size, italic), RgbaCanvas.MeasureTextWidthWithFont("HHH", size, face, italic), 10);
        Assert.Equal(face.LineHeight(size), actual.MeasureTextHeight(size), 10);
        Assert.Equal(face.LineHeight(size), RgbaCanvas.MeasureTextHeight(size, face), 10);
        Assert.Equal(face.Ascent(size), actual.TextAscent(size, false), 10);
    }
}
