using System;
using System.Globalization;
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
    public byte[] Render(ChartGrid grid) => PngWriter.WriteRgba(RenderImage(grid));

    internal RgbaImage RenderImage(ChartGrid grid) => RenderCanvas(grid).ToImage();

    internal RgbaCanvas RenderCanvas(ChartGrid grid) {
        if (grid == null) throw new ArgumentNullException(nameof(grid));
        var layout = ChartGridLayout.FromGrid(grid);
        var theme = grid.Theme ?? grid.Charts[0].Options.Theme;
        var background = theme.Background.A == 0 ? theme.CardBackground : theme.Background;
        using var emphasis = RgbaCanvas.OpenEmphasisScope();
        var output = new RgbaCanvas(layout.Width, layout.Height, 1, TypographyFontResolver.ResolveThemeFont(theme.FontFamily), grid.PngOutputScale);
        output.Clear(background);
        if (background.A == 255) {
            var inset = ChartSurfacePolish.EdgeSafeSurfaceInset(layout.Width, layout.Height);
            output.FillRoundedRectVerticalGradient(inset, inset, Math.Max(1, layout.Width - inset * 2), Math.Max(1, layout.Height - inset * 2), 0, ChartSurfacePolish.GradientTop(background), ChartSurfacePolish.GradientBottom(background));
        }
        if (layout.HeaderHeight > 0) {
            var titleStyle = grid.TitleStyle.WithDefaultFontWeight(800).WithDefaultFontFamily(theme.FontFamily);
            var subtitleStyle = grid.SubtitleStyle.WithDefaultFontWeight(400).WithDefaultFontFamily(theme.FontFamily);
            var headerWidth = Math.Max(8, layout.Width - grid.Padding * 2);
            var titleFontSize = StyleFontSize(titleStyle, theme.TitleFontSize);
            var subtitleFontSize = StyleFontSize(subtitleStyle, theme.SubtitleFontSize);
            if (grid.Title.Length > 0) DrawStyledText(output, grid.Padding, Math.Max(0, grid.Padding - titleFontSize * 0.3), ChartTextFitting.TrimEnd(grid.Title, titleFontSize, headerWidth, (text, size) => MeasureStyledTextWidth(output, text, size, titleStyle, emphasized: true)), titleStyle, theme.Text, titleFontSize, emphasized: true);
            if (grid.Subtitle.Length > 0) DrawStyledText(output, grid.Padding + 2, grid.Padding + titleFontSize + subtitleFontSize * 0.3, ChartTextFitting.TrimEnd(grid.Subtitle, subtitleFontSize, headerWidth, (text, size) => MeasureStyledTextWidth(output, text, size, subtitleStyle, emphasized: false)), subtitleStyle, theme.MutedText, subtitleFontSize, emphasized: false);
        }

        foreach (var cell in layout.Cells) {
            var density = ChartPanelDensity.OutputScale(cell.Chart.Options.Size, cell.Width, cell.Height, grid.PngOutputScale);
            var chartCanvas = _chartRenderer.RenderCanvas(cell.Chart, density);
            output.DrawImageScaled(cell.X, cell.Y, cell.Width, cell.Height, chartCanvas.OutputWidth, chartCanvas.OutputHeight, chartCanvas.ToOutputPixels());
        }

        return output;
    }

    private static double StyleFontSize(TextStyleOverride style, double fallback) {
        var size = style.FontSize ?? fallback;
        return style.Baseline is TextBaseline.Superscript or TextBaseline.Subscript ? size * 0.65 : size;
    }

    private static ChartColor StyleColor(TextStyleOverride style, ChartColor fallback) => style.Color ?? fallback;

    private static ResolvedTypeface StyleFace(TextStyleOverride style, bool fallback) =>
        TypographyFontResolver.WithColorPalette(TypographyFontResolver.WithVariations(TypographyFontResolver.WithLanguage(TypographyFontResolver.ResolveFace(style.FontFamily ?? "sans-serif",style.ResolveFontWeight(fallback ? 700 : 400),style.Italic), style.OpenTypeLanguageTag), style.Variations), style.ColorPaletteIndex);
    private static double MeasureStyledTextWidth(RgbaCanvas canvas, string text, double fontSize, TextStyleOverride style, bool emphasized) {
        text = style.TransformText(text, CultureInfo.InvariantCulture);
        return MeasureStyledTextWidthCore(canvas, text, fontSize, style, emphasized);
    }

    private static double MeasureStyledTextWidthCore(RgbaCanvas canvas, string text, double fontSize, TextStyleOverride style, bool emphasized) {
        var face = StyleFace(style,emphasized);
        return face.SynthesizeBold ? RgbaCanvas.MeasureTextEmphasizedWidth(text,fontSize,face.Font,face.SynthesizeItalic)
            : RgbaCanvas.MeasureTextWidthWithFont(text,fontSize,face.Font,face.SynthesizeItalic);
    }
    private static void DrawStyledText(RgbaCanvas canvas, double x, double y, string text, TextStyleOverride style, ChartColor fallback, double fontSize, bool emphasized) {
        text = style.TransformText(text, CultureInfo.InvariantCulture);
        var color = StyleColor(style, fallback);
        var face = StyleFace(style,emphasized);
        y += style.Baseline == TextBaseline.Superscript ? -fontSize * 0.35 : style.Baseline == TextBaseline.Subscript ? fontSize * 0.22 : 0;
        if (face.SynthesizeBold) canvas.DrawTextEmphasized(x,y,text,color,fontSize,face.Font,face.SynthesizeItalic);
        else canvas.DrawText(x,y,text,color,fontSize,face.Font,face.SynthesizeItalic);
        if (text.Length == 0) return;
        var width = MeasureStyledTextWidthCore(canvas, text, fontSize, style, emphasized);
        var thickness = Math.Max(1, fontSize / 13.0);
        var underline = style.UnderlineStyle ?? (style.Underline ? TextDecorationStyle.Single : TextDecorationStyle.None);
        var strike = style.StrikethroughStyle ?? (style.Strikethrough ? TextDecorationStyle.Single : TextDecorationStyle.None);
        RasterTextDecoration.Draw(canvas, x, x + width, y + fontSize + 2, underline, color, thickness);
        RasterTextDecoration.Draw(canvas, x, x + width, y + fontSize * 0.55, strike, color, thickness);
    }

}
