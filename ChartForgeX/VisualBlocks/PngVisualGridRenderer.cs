using System;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;
using ChartForgeX.Rendering;

namespace ChartForgeX.VisualBlocks;

/// <summary>
/// Renders visual grids to dependency-free PNG images.
/// </summary>
public sealed class PngVisualGridRenderer {
    private readonly PngChartRenderer _chartRenderer = new();
    private readonly PngVisualBlockRenderer _blockRenderer = new();

    /// <summary>Renders a visual grid to PNG bytes.</summary>
    public byte[] Render(VisualGrid grid) => PngWriter.WriteRgba(RenderImage(grid));

    internal RgbaImage RenderImage(VisualGrid grid) => RenderCanvas(grid).ToImage();

    internal RgbaCanvas RenderCanvas(VisualGrid grid) {
        if (grid == null) throw new ArgumentNullException(nameof(grid));
        var layout = VisualGridLayout.FromGrid(grid);
        var theme = grid.Theme ?? VisualGridLayout.ItemTheme(grid.Items[0]);
        var background = theme.Background.A == 0 ? theme.CardBackground : theme.Background;
        var canvas = new RgbaCanvas(layout.Width, layout.Height, 1, TrueTypeFont.TryLoadForFamily(theme.FontFamily, out _), grid.PngOutputScale);
        canvas.Clear(background);
        if (background.A == 255) {
            var surfaceInset = ChartSurfacePolish.EdgeSafeSurfaceInset(layout.Width, layout.Height);
            canvas.FillRoundedRectVerticalGradient(surfaceInset, surfaceInset, Math.Max(1, layout.Width - surfaceInset * 2), Math.Max(1, layout.Height - surfaceInset * 2), 0, ChartSurfacePolish.GradientTop(background), ChartSurfacePolish.GradientBottom(background));
        }
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
            var child = cell.Item.Chart != null ? RenderChildChart(cell.Item.Chart) : RenderChildBlock(cell.Item.Block!);
            canvas.DrawImageScaled(cell.X, cell.Y, cell.Width, cell.Height, child.OutputWidth, child.OutputHeight, child.ToOutputPixels());
        }

        return canvas;
    }

    private RgbaCanvas RenderChildChart(Chart chart) {
        var transparentBackground = chart.Options.TransparentBackground;
        try {
            chart.Options.TransparentBackground = true;
            return _chartRenderer.RenderCanvas(chart);
        }
        finally {
            chart.Options.TransparentBackground = transparentBackground;
        }
    }

    private RgbaCanvas RenderChildBlock(IVisualBlock block) {
        var transparentBackground = block.Options.TransparentBackground;
        try {
            block.Options.TransparentBackground = true;
            return _blockRenderer.RenderCanvas(block);
        }
        finally {
            block.Options.TransparentBackground = transparentBackground;
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

        return value.Substring(0, low) + suffix;
    }

}
