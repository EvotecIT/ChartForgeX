using System;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;

namespace ChartForgeX.VisualBlocks;

/// <summary>
/// Renders visual grids to dependency-free PNG images.
/// </summary>
public sealed class PngVisualGridRenderer {
    private readonly PngChartRenderer _chartRenderer = new();
    private readonly PngVisualBlockRenderer _blockRenderer = new();

    /// <summary>Renders a visual grid to PNG bytes.</summary>
    public byte[] Render(VisualGrid grid) {
        var canvas = RenderCanvas(grid);
        return PngWriter.WriteRgba(canvas.OutputWidth, canvas.OutputHeight, canvas.ToOutputPixels());
    }

    internal RgbaCanvas RenderCanvas(VisualGrid grid) {
        if (grid == null) throw new ArgumentNullException(nameof(grid));
        var layout = VisualGridLayout.FromGrid(grid);
        var theme = grid.Theme ?? VisualGridLayout.ItemTheme(grid.Items[0]);
        var background = theme.Background.A == 0 ? theme.CardBackground : theme.Background;
        var canvas = new RgbaCanvas(layout.Width, layout.Height, 1, null, grid.PngOutputScale);
        canvas.Clear(background);
        if (layout.HeaderHeight > 0) {
            var headerWidth = Math.Max(8, layout.Width - grid.Padding * 2);
            if (grid.Title.Length > 0) canvas.DrawTextEmphasized(grid.Padding, grid.Padding - theme.TitleFontSize * 0.28, FitText(grid.Title, theme.TitleFontSize, headerWidth), theme.Text, theme.TitleFontSize);
            if (grid.Subtitle.Length > 0) canvas.DrawText(grid.Padding + 2, grid.Padding + theme.TitleFontSize + theme.SubtitleFontSize * 0.25, FitText(grid.Subtitle, theme.SubtitleFontSize, headerWidth), theme.MutedText, theme.SubtitleFontSize);
        }

        foreach (var cell in layout.Cells) {
            var child = cell.Item.Chart != null ? _chartRenderer.RenderCanvas(cell.Item.Chart) : _blockRenderer.RenderCanvas(cell.Item.Block!);
            canvas.DrawImageScaled(cell.X, cell.Y, cell.Width, cell.Height, child.OutputWidth, child.OutputHeight, child.ToOutputPixels());
        }

        return canvas;
    }

    private static string FitText(string value, double fontSize, double maxWidth) {
        if (string.IsNullOrEmpty(value) || RgbaCanvas.MeasureTextWidth(value, fontSize, null) <= maxWidth) return value;
        const string suffix = "...";
        if (RgbaCanvas.MeasureTextWidth(suffix, fontSize, null) > maxWidth) return string.Empty;
        var low = 0;
        var high = value.Length;
        while (low < high) {
            var mid = (low + high + 1) / 2;
            if (RgbaCanvas.MeasureTextWidth(value.Substring(0, mid) + suffix, fontSize, null) <= maxWidth) low = mid;
            else high = mid - 1;
        }

        return value.Substring(0, low) + suffix;
    }
}
