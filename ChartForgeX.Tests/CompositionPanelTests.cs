using ChartForgeX.Composition;
using ChartForgeX.Primitives;
using ChartForgeX.Typography;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class CompositionPanelTests {
    [Fact]
    public void PanelPreservesTransparentCornersClampedRadiusAndInsideOutline() {
        var fill = ChartColor.FromRgba(25, 45, 75, 180);
        var border = ChartColor.FromRgba(90, 160, 220, 120);
        var panel = ImageComposition.CreatePanel(24, 14, fill, border, cornerRadius: 100).ToImage();
        var expected = ImageComposition.CreateTransparent(24, 14)
            .FillRoundedRectangle(0, 0, 24, 14, 7, fill)
            .StrokeRoundedRectangle(0, 0, 24, 14, 7, border).ToImage();
        Assert.Equal(expected.Pixels, panel.Pixels);
        Assert.Equal(0, panel.Pixels[3]);
        Assert.Equal(fill.A, panel.Pixels[(7 * 24 + 12) * 4 + 3]);

        var transparentInk = ChartColor.FromRgba(25, 45, 75, 0);
        Assert.Equal(ImageComposition.Create(4, 3, transparentInk).ToImage().Pixels,
            ImageComposition.CreatePanel(4, 3, transparentInk).ToImage().Pixels);
    }

    [Fact]
    public void MeasuredTextRetainsLineSpacingAlignmentAndOneTimeCasing() {
        var style = TextStyle.Create(18, ChartColor.FromHex("#DDEEFF"));
        style.TextCase = TextCaseTransform.ToggleCase;
        style.Alignment = TextAlignment.Right;
        style.UnderlineStyle = TextDecorationStyle.Single;
        var layout = TextLayoutEngine.Layout("SeRvEr\nDisk", 220, style, TextWrapMode.NoWrap);
        Assert.Equal("sErVeR", layout.Lines[0].Text);
        Assert.Equal("dISK", layout.Lines[1].Text);

        var actual = ImageComposition.CreateTransparent(260, 100)
            .DrawTextLayout(12, 8, 220, layout, style).ToImage();
        var paint = style.Clone();
        paint.TextCase = TextCaseTransform.None;
        paint.Alignment = TextAlignment.Left;
        var expected = ImageComposition.CreateTransparent(260, 100);
        for (var index = 0; index < layout.Lines.Count; index++) {
            var line = layout.Lines[index];
            expected.DrawText(12 + Math.Max(0, 220 - line.Width), 8 + index * layout.Metrics.LineHeight,
                Math.Max(1, line.Width + 2), line.Text, paint, TextWrapMode.NoWrap, null, TextTrimming.None);
        }
        Assert.Equal(expected.ToImage().Pixels, actual.Pixels);
        Assert.Equal(TextCaseTransform.ToggleCase, style.TextCase);
        Assert.Equal(TextAlignment.Right, style.Alignment);
    }

    [Fact]
    public void OversizedMeasuredTextDoesNotMoveBeforeItsRegion() {
        var style = TextStyle.Create(24, ChartColor.White);
        var layout = TextLayoutEngine.Layout("Wide", 100, style, TextWrapMode.NoWrap);
        var expected = ImageComposition.CreateTransparent(160, 50)
            .DrawTextLayout(12, 8, 100, layout, style).ToImage();
        style.Alignment = TextAlignment.Right;
        var actual = ImageComposition.CreateTransparent(160, 50)
            .DrawTextLayout(12, 8, 1, layout, style).ToImage();
        Assert.Equal(expected.Pixels, actual.Pixels);
    }

    [Fact]
    public void HeaderKeepsNoteCaseAndReservesItsWidthBeforeTrimmingTitle() {
        const double width = 260;
        var style = TextStyle.Create(18, ChartColor.FromRgba(220, 235, 255, 200));
        style.TextCase = TextCaseTransform.ToggleCase;
        var noteStyle = style.Clone();
        noteStyle.TextCase = TextCaseTransform.None;
        var note = TextLayoutEngine.Layout("10:42 AM", width, noteStyle, TextWrapMode.NoWrap, 1, TextTrimming.Ellipsis);
        var title = TextLayoutEngine.Layout("Service capacity with a long name", width - note.Metrics.Width - 12,
            style, TextWrapMode.NoWrap, 1, TextTrimming.Ellipsis);
        Assert.True(title.Trimmed);
        var paint = style.Clone();
        paint.TextCase = TextCaseTransform.None;
        var expected = ImageComposition.CreateTransparent(300, 70)
            .DrawText(12 + width - note.Metrics.Width, 8, note.Metrics.Width + 2, note.Lines[0].Text,
                noteStyle, TextWrapMode.NoWrap, null, TextTrimming.None)
            .DrawText(12, 8, Math.Max(1, title.Metrics.Width + 2), title.Lines[0].Text,
                paint, TextWrapMode.NoWrap, null, TextTrimming.None).ToImage();
        var actual = ImageComposition.CreateTransparent(300, 70);
        var height = actual.DrawTextHeader(12, 8, width, "Service capacity with a long name", "10:42 AM", style);
        Assert.Equal(Math.Max(note.Metrics.Height, title.Metrics.Height), height);
        Assert.Equal(expected.Pixels, actual.ToImage().Pixels);
        Assert.Equal(TextCaseTransform.ToggleCase, style.TextCase);
        Assert.Equal(0, actual.DrawTextHeader(0, 0, 260, null, " ", style));
        var narrow = ImageComposition.CreateTransparent(20, 30);
        Assert.Equal(0, narrow.DrawTextHeader(0, 0, 0.1, "Title", "Note", style));
        Assert.All(narrow.ToImage().Pixels, channel => Assert.Equal(0, channel));
    }

    [Fact]
    public void PatternsPreserveOddSpacingPhaseAndTranslucentIntersections() {
        var ink = ChartColor.FromRgba(70, 150, 220, 96);
        var grid = ImageComposition.CreateTransparent(23, 16).FillGridPattern(ink, spacing: 9).ToImage();
        var expectedGrid = ImageComposition.CreateTransparent(23, 16)
            .FillRectangle(4, 0, 1, 16, ink).FillRectangle(13, 0, 1, 16, ink).FillRectangle(22, 0, 1, 16, ink)
            .FillRectangle(0, 4, 23, 1, ink).FillRectangle(0, 13, 23, 1, ink).ToImage();
        Assert.Equal(expectedGrid.Pixels, grid.Pixels);
        Assert.True(grid.Pixels[(4 * 23 + 4) * 4 + 3] > ink.A);

        var dots = ImageComposition.CreateTransparent(23, 16).FillDotPattern(ink, spacing: 9, radius: 1.2).ToImage();
        var expectedDots = ImageComposition.CreateTransparent(23, 16)
            .FillCircle(4.5, 4.5, 1.2, ink).FillCircle(13.5, 4.5, 1.2, ink).FillCircle(22.5, 4.5, 1.2, ink)
            .FillCircle(4.5, 13.5, 1.2, ink).FillCircle(13.5, 13.5, 1.2, ink).FillCircle(22.5, 13.5, 1.2, ink).ToImage();
        Assert.Equal(expectedDots.Pixels, dots.Pixels);
    }

    [Fact]
    public void InvalidPatternArgumentsAreRejectedBeforePainting() {
        var image = ImageComposition.CreateTransparent(24, 16);
        Assert.Throws<ArgumentOutOfRangeException>(() => image.FillGridPattern(ChartColor.White, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => image.FillDotPattern(ChartColor.White, 8, offset: double.NaN));
        Assert.Throws<ArgumentOutOfRangeException>(() => image.FillDotPattern(ChartColor.White, 8, offset: 8));
        Assert.All(image.ToImage().Pixels, value => Assert.Equal(0, value));
    }
}
