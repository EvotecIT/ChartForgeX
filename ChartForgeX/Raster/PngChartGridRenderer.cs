using System;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;

namespace ChartForgeX.Raster;

/// <summary>
/// Renders chart grids to dependency-free PNG images.
/// </summary>
public sealed class PngChartGridRenderer {
    private readonly PngChartRenderer _chartRenderer = new();

    /// <summary>
    /// Renders a chart grid to PNG bytes.
    /// </summary>
    /// <param name="grid">The chart grid to render.</param>
    /// <returns>A PNG image.</returns>
    public byte[] Render(ChartGrid grid) {
        if (grid == null) throw new ArgumentNullException(nameof(grid));
        var layout = ChartGridLayout.FromGrid(grid);
        var theme = grid.Theme ?? grid.Charts[0].Options.Theme;
        var background = theme.Background.A == 0 ? theme.CardBackground : theme.Background;
        var output = new RgbaCanvas(layout.Width, layout.Height, 1, null, grid.PngOutputScale);
        output.Clear(background);
        if (background.A > 0) {
            var inset = ChartSurfacePolish.EdgeSafeSurfaceInset(layout.Width, layout.Height);
            output.FillRoundedRectVerticalGradient(inset, inset, Math.Max(1, layout.Width - inset * 2), Math.Max(1, layout.Height - inset * 2), 0, ChartSurfacePolish.GradientTop(background), ChartSurfacePolish.GradientBottom(background));
        }
        if (layout.HeaderHeight > 0) {
            var headerWidth = Math.Max(8, layout.Width - grid.Padding * 2);
            var titleFontSize = StyleFontSize(grid.TitleStyle, theme.TitleFontSize);
            var subtitleFontSize = StyleFontSize(grid.SubtitleStyle, theme.SubtitleFontSize);
            if (grid.Title.Length > 0) DrawStyledText(output, grid.Padding, Math.Max(0, grid.Padding - titleFontSize * 0.3), FitText(grid.Title, titleFontSize, headerWidth), grid.TitleStyle, theme.Text, titleFontSize, emphasized: true);
            if (grid.Subtitle.Length > 0) DrawStyledText(output, grid.Padding + 2, grid.Padding + titleFontSize + subtitleFontSize * 0.3, FitText(grid.Subtitle, subtitleFontSize, headerWidth), grid.SubtitleStyle, theme.MutedText, subtitleFontSize, emphasized: false);
        }

        foreach (var cell in layout.Cells) {
            var chartCanvas = _chartRenderer.RenderCanvas(cell.Chart);
            output.DrawImageScaled(cell.X, cell.Y, cell.Width, cell.Height, chartCanvas.OutputWidth, chartCanvas.OutputHeight, chartCanvas.ToOutputPixels());
        }

        return PngWriter.WriteRgba(output.OutputWidth, output.OutputHeight, output.ToOutputPixels());
    }

    private static double StyleFontSize(ChartTextStyle style, double fallback) => style.FontSize ?? fallback;

    private static ChartColor StyleColor(ChartTextStyle style, ChartColor fallback) => style.Color ?? fallback;

    private static void DrawStyledText(RgbaCanvas canvas, double x, double y, string text, ChartTextStyle style, ChartColor fallback, double fontSize, bool emphasized) {
        var color = StyleColor(style, fallback);
        if (emphasized) canvas.DrawTextEmphasized(x, y, text, color, fontSize);
        else canvas.DrawText(x, y, text, color, fontSize);
        if (!style.Underline || text.Length == 0) return;
        var width = emphasized ? RgbaCanvas.MeasureTextEmphasizedWidth(text, fontSize, null) : RgbaCanvas.MeasureTextWidth(text, fontSize, null);
        canvas.DrawLine(x, y + fontSize + 2, x + width, y + fontSize + 2, color, Math.Max(1, fontSize / 13.0));
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
