using ChartForgeX.Composition;
using ChartForgeX.Primitives;
using ChartForgeX.Typography;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>A single line wider than its region follows the requested trimming, as wrapped text past its last line does.</summary>
public sealed class NoWrapTrimmingTests {
    private const string Long = "Updated after the collector saw three versions of this notice";

    [Fact]
    public void EllipsisEndsAnOverlongLineWithinTheWidth() {
        var style = TextStyle.Create(24, ChartColor.White);
        var layout = TextLayoutEngine.Layout(Long, 200, style, TextWrapMode.NoWrap, 1, TextTrimming.Ellipsis);

        Assert.True(layout.Trimmed);
        var line = Assert.Single(layout.Lines);
        Assert.EndsWith("…", line.Text);
        Assert.True(line.Width <= 200, $"line width {line.Width} should fit 200");
    }

    [Fact]
    public void NoneKeepsTheTextAndReportsTheOverflow() {
        var style = TextStyle.Create(24, ChartColor.White);
        var layout = TextLayoutEngine.Layout(Long, 200, style, TextWrapMode.NoWrap, 1, TextTrimming.None);

        Assert.True(layout.Trimmed);
        Assert.Equal(Long, Assert.Single(layout.Lines).Text);
        Assert.False(TextLayoutEngine.Layout("Short", 200, style, TextWrapMode.NoWrap, 1, TextTrimming.Ellipsis).Trimmed);
    }

    [Fact]
    public void CompositionDrawsTheEllipsizedLineInsideItsRegion() {
        var style = TextStyle.Create(24, ChartColor.White);
        var image = ImageComposition.CreateTransparent(400, 40).DrawText(0, 4, 200, Long, style, TextWrapMode.NoWrap, 1, TextTrimming.Ellipsis).ToImage();
        for (var y = 0; y < image.Height; y++) {
            for (var x = 204; x < image.Width; x++) Assert.Equal(0, image.Pixels[(y * image.Width + x) * 4 + 3]);
        }
    }
}
