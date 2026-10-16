using System;
using ChartForgeX.Themes;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;
using ChartForgeX.Rendering;

namespace ChartForgeX.VisualBlocks;

/// <summary>
/// Renders visual grids to dependency-free PNG images.
/// </summary>
public sealed class PngVisualGridRenderer {

    /// <summary>Renders a visual grid to PNG bytes.</summary>
    public byte[] Render(VisualGrid grid) => PngWriter.WriteRgba(RenderImage(grid));

    internal RgbaImage RenderImage(VisualGrid grid) => RenderCanvas(grid).ToImage();

    internal RgbaCanvas RenderCanvas(VisualGrid grid) {
        if (grid == null) throw new ArgumentNullException(nameof(grid));
        var layout = VisualGridLayout.FromGrid(grid);
        var theme = grid.ResolvedTheme;
        var background = theme.Background.A == 0 ? theme.CardBackground : theme.Background;
        using var emphasis = RgbaCanvas.OpenEmphasisScope();
        var canvas = new RgbaCanvas(layout.Width, layout.Height, 1, TypographyFontResolver.ResolveThemeFont(theme.FontFamily), grid.PngOutputScale);
        canvas.Clear(background);

        if (grid.FrameVisible) {
            var inset = Math.Max(8, grid.Padding * 0.5);
            canvas.StrokeRoundedRect(inset, inset, Math.Max(1, layout.Width - inset * 2), Math.Max(1, layout.Height - inset * 2), Math.Max(theme.CornerRadius, 26), theme.CardBorder, 1.4);
            if (background.A > 0) canvas.StrokeRoundedRect(inset + ChartVisualPrimitives.CardInnerHighlightInset, inset + ChartVisualPrimitives.CardInnerHighlightInset, Math.Max(1, layout.Width - inset * 2 - ChartVisualPrimitives.CardInnerHighlightInset * 2), Math.Max(1, layout.Height - inset * 2 - ChartVisualPrimitives.CardInnerHighlightInset * 2), Math.Max(theme.CornerRadius - ChartVisualPrimitives.CardInnerHighlightInset, 24), ChartColorMath.WithOpacity(ChartColor.White, ChartVisualPrimitives.CardInnerHighlightOpacity), 1);
        }
        if (layout.HeaderHeight > 0) {
            var headerWidth = Math.Max(8, layout.Width - grid.Padding * 2);
            if (grid.Title.Length > 0) canvas.DrawTextEmphasized(grid.Padding, grid.Padding - theme.TitleFontSize * 0.28, FitText(canvas, grid.Title, theme.TitleFontSize, headerWidth), theme.Text, theme.TitleFontSize);
            if (grid.Subtitle.Length > 0) canvas.DrawText(grid.Padding + 2, grid.Padding + theme.TitleFontSize + theme.SubtitleFontSize * 0.25, FitText(canvas, grid.Subtitle, theme.SubtitleFontSize, headerWidth), theme.MutedText, theme.SubtitleFontSize);
        }

        foreach (var cell in layout.Cells) {
            var size = VisualGridLayout.ItemSize(cell.Item);
            var density = ChartPanelDensity.OutputScale(size, cell.Width, cell.Height, grid.PngOutputScale);
            var child = cell.Item.Chart != null ? VisualGridChartRendering.Image(cell.Item.Chart, density) : RenderChildBlock(cell.Item.Block!, density);
            canvas.DrawImageScaled(cell.X, cell.Y, cell.Width, cell.Height, child.Width, child.Height, child.Pixels);
        }

        return canvas;
    }

    private static RgbaImage RenderChildBlock(IVisualBlock block, int density) {
        var transparentBackground = block.Options.TransparentBackground;
        var originalTheme = block.Options.Theme;
        try {
            block.Options.TransparentBackground = true;
            block.Options.Theme = originalTheme.Configure(builder => {
                var font = originalTheme.Typography;
                builder.Typography = new VisualTypography(font.Family, 15, font.SubtitleSize, font.AxisSize, font.LegendSize,
                    font.DataLabelSize, font.ScalarValueSize, font.CenterValueSize, font.AxisTitleSize, font.MonospaceFamily,
                    font.TileLabelSize, font.TileValueSize, font.TileDetailSize);
            });
            if (block is IFactualVisualPixels factual) return factual.RenderAtScale(density);
            if (SvgVisualBlockRenderer.IsNativeBlock(block)) return new PngVisualBlockRenderer().RenderCanvas(block, density).ToImage();
            return block.RenderRgba();
        }
        finally {
            block.Options.TransparentBackground = transparentBackground;
            block.Options.Theme = originalTheme;
        }
    }

    private static string FitText(RgbaCanvas canvas, string value, double fontSize, double maxWidth) {
        if (string.IsNullOrEmpty(value) || canvas.MeasureTextWidth(value, fontSize) <= maxWidth) return value;
        const string suffix = "...";
        if (canvas.MeasureTextWidth(suffix, fontSize) > maxWidth) return string.Empty;
        var low = 0;
        var high = value.Length;
        while (low < high) {
            var mid = (low + high + 1) / 2;
            if (canvas.MeasureTextWidth(value.Substring(0, mid) + suffix, fontSize) <= maxWidth) low = mid;
            else high = mid - 1;
        }

        return value.Substring(0, Typography.TextElementBoundary.Snap(value, low)) + suffix;
    }

}
