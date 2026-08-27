using System;
using System.Collections.Generic;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;

namespace ChartForgeX.Raster;

public sealed partial class PngChartRenderer {
    private static void DrawReadablePngLabel(RgbaCanvas c, double x, double y, string label, ChartColor text, ChartColor halo, double fontSize, TextStyleOverride? style = null) {
        text = style == null ? text : PngStyleColor(style, text);
        var italic = style?.Italic == true;
        var font = style == null ? CurrentOutlineFont : PngStyleFont(style);
        var emphasized = style == null || PngStyleEmphasized(style, fallback: true);
        foreach (var layer in ChartTextHalo.ReadableRasterLayers(fontSize)) c.DrawText(x + layer.Dx, y + layer.Dy, label, ApplyOpacity(halo, layer.Opacity), fontSize, font, italic);
        if (emphasized) c.DrawTextEmphasized(x, y, label, text, fontSize, font, italic);
        else c.DrawText(x, y, label, text, fontSize, font, italic);
        if (style != null) DrawPngUnderline(c, x, y + fontSize, label, style, text, fontSize, emphasized: true);
    }

    private static void DrawReadablePngLabel(RgbaCanvas c, ChartRect plot, double x, double y, string label, ChartColor text, ChartColor halo, double fontSize, TextStyleOverride? style = null) {
        FitReadablePngLabel(label, fontSize, Math.Max(8, plot.Width - ChartVisualPrimitives.DataLabelPlotInset * 2), Math.Max(8, plot.Height - ChartVisualPrimitives.DataLabelPlotInset * 2), out var fittedLabel, out var fittedFontSize, style);
        if (fittedLabel.Length == 0) return;
        var width = style == null ? EstimatePngEmphasizedTextWidth(fittedLabel, fittedFontSize) : EstimatePngStyledTextWidth(fittedLabel, fittedFontSize, style, emphasized: true);
        var height = style == null ? EstimatePngTextHeight(fittedFontSize) : EstimatePngStyledTextHeight(fittedFontSize, style);
        DrawReadablePngLabel(c, Clamp(x, plot.Left + ChartVisualPrimitives.DataLabelPlotInset, plot.Right - width - ChartVisualPrimitives.DataLabelPlotInset), Clamp(y, plot.Top + ChartVisualPrimitives.DataLabelPlotInset, plot.Bottom - height - ChartVisualPrimitives.DataLabelPlotInset), fittedLabel, text, halo, fittedFontSize, style);
    }

    private static void DrawReadablePngLabelCentered(RgbaCanvas c, ChartRect bounds, string label, ChartColor text, ChartColor halo, double fontSize, TextStyleOverride? style = null) {
        FitReadablePngLabel(label, fontSize, Math.Max(8, bounds.Width - 8), Math.Max(8, bounds.Height - 6), out var fittedLabel, out var fittedFontSize, style);
        if (fittedLabel.Length == 0) return;
        var width = style == null ? EstimatePngEmphasizedTextWidth(fittedLabel, fittedFontSize) : EstimatePngStyledTextWidth(fittedLabel, fittedFontSize, style, emphasized: true);
        var height = style == null ? EstimatePngTextHeight(fittedFontSize) : EstimatePngStyledTextHeight(fittedFontSize, style);
        DrawReadablePngLabel(c, bounds.Left + (bounds.Width - width) / 2.0, bounds.Top + (bounds.Height - height) / 2.0, fittedLabel, text, halo, fittedFontSize, style);
    }

    private static bool IsPointCalloutSeries(ChartSeries series) => series.SemanticRole == "point-callout";

    private static void DrawPngPointCalloutLabel(RgbaCanvas c, Chart chart, ChartRect plot, double x, double y, string label, ChartDataLabelPlacement placement, double preferredFontSize) {
        var fontSize = Math.Max(preferredFontSize, 15);
        label = TrimReadablePngLabelToWidth(label, fontSize, Math.Max(72, plot.Width * 0.42));
        if (label.Length == 0) return;
        var padX = 12.0;
        var padY = 8.0;
        var width = EstimatePngEmphasizedTextWidth(label, fontSize) + padX * 2;
        var height = EstimatePngTextHeight(fontSize) + padY * 2;
        var gap = 14.0;
        var rectX = x - width / 2;
        var rectY = y - gap - height;
        if (placement == ChartDataLabelPlacement.Below) rectY = y + gap;
        else if (placement == ChartDataLabelPlacement.Left) {
            rectX = x - gap - width;
            rectY = y - height / 2;
        } else if (placement == ChartDataLabelPlacement.Right) {
            rectX = x + gap;
            rectY = y - height / 2;
        }

        rectX = Clamp(rectX, plot.Left + 4, plot.Right - width - 4);
        rectY = Clamp(rectY, plot.Top + 4, plot.Bottom - height - 4);
        var fill = ChartColor.FromRgba(20, 20, 22, 238);
        c.FillRoundedRect(rectX, rectY, width, height, 9, fill);
        DrawPngPointCalloutPointer(c, x, y, rectX, rectY, width, height, placement, fill);
        c.DrawTextEmphasized(rectX + (width - EstimatePngEmphasizedTextWidth(label, fontSize)) / 2.0, rectY + padY, label, ChartColor.White, fontSize);
    }

    private static void DrawPngPointCalloutPointer(RgbaCanvas c, double x, double y, double rectX, double rectY, double width, double height, ChartDataLabelPlacement placement, ChartColor fill) {
        List<ChartPoint> points;
        if (placement == ChartDataLabelPlacement.Below) {
            var baseX = Clamp(x, rectX + 10, rectX + width - 10);
            points = new List<ChartPoint> { new(baseX - 6, rectY), new(baseX + 6, rectY), new(x, y + 5) };
        } else if (placement == ChartDataLabelPlacement.Left) {
            var baseY = Clamp(y, rectY + 10, rectY + height - 10);
            points = new List<ChartPoint> { new(rectX + width, baseY - 6), new(rectX + width, baseY + 6), new(x - 5, y) };
        } else if (placement == ChartDataLabelPlacement.Right) {
            var baseY = Clamp(y, rectY + 10, rectY + height - 10);
            points = new List<ChartPoint> { new(rectX, baseY - 6), new(rectX, baseY + 6), new(x + 5, y) };
        } else {
            var baseX = Clamp(x, rectX + 10, rectX + width - 10);
            points = new List<ChartPoint> { new(baseX - 6, rectY + height), new(baseX + 6, rectY + height), new(x, y - 5) };
        }

        c.FillPolygon(points, fill);
    }

    private static void DrawPngTextEmphasizedCenteredX(RgbaCanvas c, double centerX, double y, string text, ChartColor color, double fontSize) {
        c.DrawTextEmphasized(centerX - EstimatePngEmphasizedTextWidth(text, fontSize) / 2.0, y, text, color, fontSize);
    }

    private static void DrawPngTextEmphasizedCenteredX(RgbaCanvas c, double centerX, double y, string text, ChartColor color, double fontSize, double maxWidth) {
        var fittedFontSize = TextFontSizeForEmphasizedWidth(text, Math.Max(8, maxWidth), fontSize);
        var fittedText = TrimReadablePngLabelToWidth(text, fittedFontSize, Math.Max(8, maxWidth));
        if (fittedText.Length == 0) return;
        DrawPngTextEmphasizedCenteredX(c, centerX, y, fittedText, color, fittedFontSize);
    }

    private static ChartColor ReadableLabelHalo(Chart chart) {
        var color = chart.Options.Theme.CardBackground;
        return color.A == 0 ? ChartColor.White : color;
    }

    private static ChartColor ApplyOpacity(ChartColor color, double opacity) => ChartColorMath.WithOpacity(color, opacity);

    private static double EstimatePngTextWidth(string value, double fontSize) => EstimatePngTextWidth(value, fontSize, italic: false);
    private static double EstimatePngTextWidth(string value, double fontSize, bool italic) => Math.Ceiling(RgbaCanvas.MeasureTextWidth(value, fontSize, CurrentOutlineFont, italic));
    private static double EstimatePngEmphasizedTextWidth(string value, double fontSize) => EstimatePngEmphasizedTextWidth(value, fontSize, italic: false);
    private static double EstimatePngEmphasizedTextWidth(string value, double fontSize, bool italic) => Math.Ceiling(RgbaCanvas.MeasureTextEmphasizedWidth(value, fontSize, CurrentOutlineFont, italic));
    private static double EstimatePngStyledTextWidth(string value, double fontSize, TextStyleOverride style, bool emphasized) {
        var font = PngStyleFont(style);
        return Math.Ceiling(PngStyleEmphasized(style, emphasized)
            ? RgbaCanvas.MeasureTextEmphasizedWidth(value, fontSize, font, style.Italic)
            : RgbaCanvas.MeasureTextWidthWithFont(value, fontSize, font, style.Italic));
    }
    private static double EstimatePngStyledTextHeight(double fontSize, TextStyleOverride style) => RgbaCanvas.MeasureTextHeight(fontSize, PngStyleFont(style));
    private static double EstimatePngTextHeight(double fontSize) => RgbaCanvas.MeasureTextHeight(fontSize, CurrentOutlineFont);
    private static double PngTickFontSize(Chart chart) => PngStyleFontSize(chart.Options.TickLabelStyle, chart.Options.Theme.TickLabelFontSize);
    private static ChartColor PngTickColor(Chart chart) => PngStyleColor(chart.Options.TickLabelStyle, chart.Options.Theme.MutedText);
    private static double PngAxisTitleFontSize(Chart chart) => PngStyleFontSize(chart.Options.AxisTitleStyle, chart.Options.Theme.AxisTitleFontSize);
    private static double PngLegendFontSize(Chart chart) => PngStyleFontSize(chart.Options.LegendStyle, chart.Options.Theme.LegendFontSize);
    private static double PngDataLabelFontSize(Chart chart, ChartSeries? series = null, int pointIndex = -1) => PngStyleFontSize(DataLabelStyle(chart, series, pointIndex), chart.Options.Theme.DataLabelFontSize);
    private static int DetailTextScale(Chart chart) => chart.Options.Size.Width >= 1000 && chart.Options.Size.Height >= 560 ? 2 : 1;
    private static ChartDataLabelPlacement DataLabelPlacement(Chart chart, ChartSeries? series) => series?.DataLabelPlacement ?? chart.Options.DataLabelPlacement;
    private static ChartColor DataLabelConnectorColor(Chart chart) => chart.Options.DataLabelConnectorColor ?? chart.Options.Theme.MutedText;
    private static ChartColor PngStyleColor(TextStyleOverride style, ChartColor fallback) => style.Color ?? fallback;
    private static double PngStyleFontSize(TextStyleOverride style, double fallback) => style.FontSize ?? fallback;
    private static TrueTypeFont? PngStyleFont(TextStyleOverride style) => CurrentOutlineFontIsExplicit || style.FontFamily == null ? CurrentOutlineFont : TrueTypeFont.TryLoadForFamily(style.FontFamily, out _) ?? CurrentOutlineFont;
    private static bool PngStyleEmphasized(TextStyleOverride style, bool fallback) => style.ResolveFontWeight(fallback ? 700 : 400) >= 600;
    private static TextStyleOverride SeriesDataLabelStyle(Chart chart, ChartSeries? series) => DataLabelStyle(chart, series);

    private static TextStyleOverride DataLabelStyle(Chart chart, ChartSeries? series, int pointIndex = -1) {
        if (series != null && pointIndex >= 0 && pointIndex < series.PointDataLabelStyles.Count) {
            var pointStyle = series.PointDataLabelStyles[pointIndex];
            if (pointStyle != null && pointStyle.HasOverrides) return pointStyle;
        }

        return series != null && series.DataLabelStyle.HasOverrides ? series.DataLabelStyle : chart.Options.DataLabelStyle;
    }

    private static void DrawPngUnderline(RgbaCanvas c, double x, double y, string text, TextStyleOverride style, ChartColor color, double fontSize, bool emphasized) {
        if (!style.Underline || text.Length == 0) return;
        var width = EstimatePngStyledTextWidth(text, fontSize, style, emphasized);
        c.DrawLine(x, y + 2, x + width, y + 2, color, Math.Max(1, fontSize / 13.0));
    }

    private static void DrawPngTextStyled(RgbaCanvas c, double x, double y, string text, TextStyleOverride style, ChartColor fallback, double fontSize, bool emphasized) {
        var color = PngStyleColor(style, fallback);
        var font = PngStyleFont(style);
        var effectiveEmphasis = PngStyleEmphasized(style, emphasized);
        if (effectiveEmphasis) c.DrawTextEmphasized(x, y, text, color, fontSize, font, style.Italic);
        else c.DrawText(x, y, text, color, fontSize, font, style.Italic);
        DrawPngUnderline(c, x, y + fontSize, text, style, color, fontSize, emphasized);
    }

    private static double TextFontSizeForWidth(string value, double maxWidth, double preferredFontSize) => TextFontSizeForWidth(value, maxWidth, preferredFontSize, false);
    private static double TextFontSizeForEmphasizedWidth(string value, double maxWidth, double preferredFontSize) => TextFontSizeForWidth(value, maxWidth, preferredFontSize, true);
    private static double TextFontSizeForWidth(string value, double maxWidth, double preferredFontSize, TextStyleOverride style) => TextFontSizeForStyledWidth(value, maxWidth, preferredFontSize, style, emphasized: false);
    private static double TextFontSizeForEmphasizedWidth(string value, double maxWidth, double preferredFontSize, TextStyleOverride style) => TextFontSizeForStyledWidth(value, maxWidth, preferredFontSize, style, emphasized: true);

    private static double TextFontSizeForStyledWidth(string value, double maxWidth, double preferredFontSize, TextStyleOverride style, bool emphasized) {
        for (var fontSize = Math.Max(8, preferredFontSize); fontSize > 8; fontSize -= 1) {
            if (EstimatePngStyledTextWidth(value, fontSize, style, emphasized) <= maxWidth) return fontSize;
        }

        return 8;
    }

    private static double TextFontSizeForWidth(string value, double maxWidth, double preferredFontSize, bool emphasized, bool italic = false) {
        for (var fontSize = Math.Max(8, preferredFontSize); fontSize > 8; fontSize -= 1) {
            var width = emphasized ? EstimatePngEmphasizedTextWidth(value, fontSize, italic) : EstimatePngTextWidth(value, fontSize, italic);
            if (width <= maxWidth) return fontSize;
        }

        return 8;
    }

    private static double FitReadablePngLabelFontSize(string value, double preferredFontSize, double maxWidth, double maxHeight, TextStyleOverride? style = null) {
        for (var fontSize = Math.Max(8, preferredFontSize); fontSize > 8; fontSize -= 1) {
            var width = style == null ? EstimatePngEmphasizedTextWidth(value, fontSize) : EstimatePngStyledTextWidth(value, fontSize, style, emphasized: true);
            var height = style == null ? EstimatePngTextHeight(fontSize) : EstimatePngStyledTextHeight(fontSize, style);
            if (width <= maxWidth && height <= maxHeight) return fontSize;
        }

        return 8;
    }

    private static void FitReadablePngLabel(string value, double preferredFontSize, double maxWidth, double maxHeight, out string fittedValue, out double fittedFontSize, TextStyleOverride? style = null) {
        fittedFontSize = FitReadablePngLabelFontSize(value, preferredFontSize, maxWidth, maxHeight, style);
        fittedValue = style == null ? TrimReadablePngLabelToWidth(value, fittedFontSize, maxWidth) : TrimReadablePngLabelToWidth(value, fittedFontSize, maxWidth, style);
    }

    private static string TrimReadablePngLabelToWidth(string value, double fontSize, double maxWidth, bool italic = false) {
        if (string.IsNullOrEmpty(value) || EstimatePngEmphasizedTextWidth(value, fontSize, italic) <= maxWidth) return value;
        const string suffix = "...";
        if (EstimatePngEmphasizedTextWidth(suffix, fontSize, italic) > maxWidth) return string.Empty;
        var low = 0;
        var high = value.Length;
        while (low < high) {
            var mid = low + (high - low + 1) / 2;
            var candidate = value.Substring(0, mid).TrimEnd() + suffix;
            if (EstimatePngEmphasizedTextWidth(candidate, fontSize, italic) <= maxWidth) low = mid;
            else high = mid - 1;
        }

        return low == 0 ? suffix : value.Substring(0, low).TrimEnd() + suffix;
    }

    private static string TrimReadablePngLabelToWidth(string value, double fontSize, double maxWidth, TextStyleOverride style) =>
        TrimPngStyledLabelToWidth(value, fontSize, maxWidth, style, emphasized: true);

    private static string TrimPngLabelToWidth(string value, double fontSize, double maxWidth, bool italic = false) {
        if (string.IsNullOrEmpty(value) || EstimatePngTextWidth(value, fontSize, italic) <= maxWidth) return value;
        const string suffix = "...";
        if (EstimatePngTextWidth(suffix, fontSize, italic) > maxWidth) return string.Empty;
        var low = 0;
        var high = value.Length;
        while (low < high) {
            var mid = low + (high - low + 1) / 2;
            var candidate = value.Substring(0, mid).TrimEnd() + suffix;
            if (EstimatePngTextWidth(candidate, fontSize, italic) <= maxWidth) low = mid;
            else high = mid - 1;
        }

        return low == 0 ? suffix : value.Substring(0, low).TrimEnd() + suffix;
    }

    private static string TrimPngLabelToWidth(string value, double fontSize, double maxWidth, TextStyleOverride style) =>
        TrimPngStyledLabelToWidth(value, fontSize, maxWidth, style, emphasized: false);

    private static string TrimPngStyledLabelToWidth(string value, double fontSize, double maxWidth, TextStyleOverride style, bool emphasized) {
        if (string.IsNullOrEmpty(value) || EstimatePngStyledTextWidth(value, fontSize, style, emphasized) <= maxWidth) return value;
        const string suffix = "...";
        if (EstimatePngStyledTextWidth(suffix, fontSize, style, emphasized) > maxWidth) return string.Empty;
        var low = 0;
        var high = value.Length;
        while (low < high) {
            var mid = low + (high - low + 1) / 2;
            var candidate = value.Substring(0, mid).TrimEnd() + suffix;
            if (EstimatePngStyledTextWidth(candidate, fontSize, style, emphasized) <= maxWidth) low = mid;
            else high = mid - 1;
        }

        return low == 0 ? suffix : value.Substring(0, low).TrimEnd() + suffix;
    }
}
