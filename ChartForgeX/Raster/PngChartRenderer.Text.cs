using System;
using System.Collections.Generic;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;

namespace ChartForgeX.Raster;

public sealed partial class PngChartRenderer {
    private static void DrawReadablePngLabel(RgbaCanvas c, double x, double y, string label, ChartColor text, ChartColor halo, double fontSize, ChartTextStyle? style = null) {
        text = style == null ? text : PngStyleColor(style, text);
        var strongHalo = ApplyOpacity(halo, ChartVisualPrimitives.PngTextHaloStrongOpacity);
        var softHalo = ApplyOpacity(halo, ChartVisualPrimitives.PngTextHaloSoftOpacity);
        var outerHalo = ApplyOpacity(halo, ChartVisualPrimitives.PngTextHaloOuterOpacity);
        var inner = ChartVisualPrimitives.PngTextHaloInnerOffset;
        var outer = ChartVisualPrimitives.PngTextHaloOuterOffset;
        c.DrawText(x - outer, y, label, outerHalo, fontSize);
        c.DrawText(x + outer, y, label, outerHalo, fontSize);
        c.DrawText(x, y - outer, label, outerHalo, fontSize);
        c.DrawText(x, y + outer, label, outerHalo, fontSize);
        c.DrawText(x - inner, y, label, strongHalo, fontSize);
        c.DrawText(x + inner, y, label, strongHalo, fontSize);
        c.DrawText(x, y - inner, label, strongHalo, fontSize);
        c.DrawText(x, y + inner, label, strongHalo, fontSize);
        c.DrawText(x - inner, y - inner, label, softHalo, fontSize);
        c.DrawText(x + inner, y - inner, label, softHalo, fontSize);
        c.DrawText(x - inner, y + inner, label, softHalo, fontSize);
        c.DrawText(x + inner, y + inner, label, softHalo, fontSize);
        c.DrawTextEmphasized(x, y, label, text, fontSize);
        if (style != null) DrawPngUnderline(c, x, y + fontSize, label, style, text, fontSize, emphasized: true);
    }

    private static void DrawReadablePngLabel(RgbaCanvas c, ChartRect plot, double x, double y, string label, ChartColor text, ChartColor halo, double fontSize, ChartTextStyle? style = null) {
        FitReadablePngLabel(label, fontSize, Math.Max(8, plot.Width - ChartVisualPrimitives.DataLabelPlotInset * 2), Math.Max(8, plot.Height - ChartVisualPrimitives.DataLabelPlotInset * 2), out var fittedLabel, out var fittedFontSize);
        if (fittedLabel.Length == 0) return;
        var width = EstimatePngEmphasizedTextWidth(fittedLabel, fittedFontSize);
        var height = EstimatePngTextHeight(fittedFontSize);
        DrawReadablePngLabel(c, Clamp(x, plot.Left + ChartVisualPrimitives.DataLabelPlotInset, plot.Right - width - ChartVisualPrimitives.DataLabelPlotInset), Clamp(y, plot.Top + ChartVisualPrimitives.DataLabelPlotInset, plot.Bottom - height - ChartVisualPrimitives.DataLabelPlotInset), fittedLabel, text, halo, fittedFontSize, style);
    }

    private static void DrawReadablePngLabelCentered(RgbaCanvas c, ChartRect bounds, string label, ChartColor text, ChartColor halo, double fontSize, ChartTextStyle? style = null) {
        FitReadablePngLabel(label, fontSize, Math.Max(8, bounds.Width - 8), Math.Max(8, bounds.Height - 6), out var fittedLabel, out var fittedFontSize);
        if (fittedLabel.Length == 0) return;
        var width = EstimatePngEmphasizedTextWidth(fittedLabel, fittedFontSize);
        var height = EstimatePngTextHeight(fittedFontSize);
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

    private static ChartColor ApplyOpacity(ChartColor color, double opacity) {
        var alpha = (byte)Math.Max(0, Math.Min(255, Math.Round(color.A * Math.Max(0, Math.Min(1, opacity)))));
        return ChartColor.FromRgba(color.R, color.G, color.B, alpha);
    }

    private static double EstimatePngTextWidth(string value, double fontSize) => Math.Ceiling(RgbaCanvas.MeasureTextWidth(value, fontSize, CurrentOutlineFont));
    private static double EstimatePngEmphasizedTextWidth(string value, double fontSize) => Math.Ceiling(RgbaCanvas.MeasureTextEmphasizedWidth(value, fontSize, CurrentOutlineFont));
    private static double EstimatePngTextHeight(double fontSize) => RgbaCanvas.MeasureTextHeight(fontSize, CurrentOutlineFont);
    private static double PngTickFontSize(Chart chart) => PngStyleFontSize(chart.Options.TickLabelStyle, chart.Options.Theme.TickLabelFontSize);
    private static ChartColor PngTickColor(Chart chart) => PngStyleColor(chart.Options.TickLabelStyle, chart.Options.Theme.MutedText);
    private static double PngAxisTitleFontSize(Chart chart) => PngStyleFontSize(chart.Options.AxisTitleStyle, chart.Options.Theme.AxisTitleFontSize);
    private static double PngLegendFontSize(Chart chart) => PngStyleFontSize(chart.Options.LegendStyle, chart.Options.Theme.LegendFontSize);
    private static double PngDataLabelFontSize(Chart chart, ChartSeries? series = null, int pointIndex = -1) => PngStyleFontSize(DataLabelStyle(chart, series, pointIndex), chart.Options.Theme.DataLabelFontSize);
    private static int DetailTextScale(Chart chart) => chart.Options.Size.Width >= 1000 && chart.Options.Size.Height >= 560 ? 2 : 1;
    private static ChartDataLabelPlacement DataLabelPlacement(Chart chart, ChartSeries? series) => series?.DataLabelPlacement ?? chart.Options.DataLabelPlacement;
    private static ChartColor DataLabelConnectorColor(Chart chart) => chart.Options.DataLabelConnectorColor ?? chart.Options.Theme.MutedText;
    private static ChartColor PngStyleColor(ChartTextStyle style, ChartColor fallback) => style.Color ?? fallback;
    private static double PngStyleFontSize(ChartTextStyle style, double fallback) => style.FontSize ?? fallback;
    private static ChartTextStyle SeriesDataLabelStyle(Chart chart, ChartSeries? series) => DataLabelStyle(chart, series);

    private static ChartTextStyle DataLabelStyle(Chart chart, ChartSeries? series, int pointIndex = -1) {
        if (series != null && pointIndex >= 0 && pointIndex < series.PointDataLabelStyles.Count) {
            var pointStyle = series.PointDataLabelStyles[pointIndex];
            if (pointStyle != null && pointStyle.HasOverrides) return pointStyle;
        }

        return series != null && series.DataLabelStyle.HasOverrides ? series.DataLabelStyle : chart.Options.DataLabelStyle;
    }

    private static void DrawPngUnderline(RgbaCanvas c, double x, double y, string text, ChartTextStyle style, ChartColor color, double fontSize, bool emphasized) {
        if (!style.Underline || text.Length == 0) return;
        var width = emphasized ? EstimatePngEmphasizedTextWidth(text, fontSize) : EstimatePngTextWidth(text, fontSize);
        c.DrawLine(x, y + 2, x + width, y + 2, color, Math.Max(1, fontSize / 13.0));
    }

    private static void DrawPngTextStyled(RgbaCanvas c, double x, double y, string text, ChartTextStyle style, ChartColor fallback, double fontSize, bool emphasized) {
        var color = PngStyleColor(style, fallback);
        if (emphasized) c.DrawTextEmphasized(x, y, text, color, fontSize);
        else c.DrawText(x, y, text, color, fontSize);
        DrawPngUnderline(c, x, y + fontSize, text, style, color, fontSize, emphasized);
    }

    private static double TextFontSizeForWidth(string value, double maxWidth, double preferredFontSize) => TextFontSizeForWidth(value, maxWidth, preferredFontSize, false);
    private static double TextFontSizeForEmphasizedWidth(string value, double maxWidth, double preferredFontSize) => TextFontSizeForWidth(value, maxWidth, preferredFontSize, true);

    private static double TextFontSizeForWidth(string value, double maxWidth, double preferredFontSize, bool emphasized) {
        for (var fontSize = Math.Max(8, preferredFontSize); fontSize > 8; fontSize -= 1) {
            var width = emphasized ? EstimatePngEmphasizedTextWidth(value, fontSize) : EstimatePngTextWidth(value, fontSize);
            if (width <= maxWidth) return fontSize;
        }

        return 8;
    }

    private static double FitReadablePngLabelFontSize(string value, double preferredFontSize, double maxWidth, double maxHeight) {
        for (var fontSize = Math.Max(8, preferredFontSize); fontSize > 8; fontSize -= 1) {
            if (EstimatePngEmphasizedTextWidth(value, fontSize) <= maxWidth && EstimatePngTextHeight(fontSize) <= maxHeight) return fontSize;
        }

        return 8;
    }

    private static void FitReadablePngLabel(string value, double preferredFontSize, double maxWidth, double maxHeight, out string fittedValue, out double fittedFontSize) {
        fittedFontSize = FitReadablePngLabelFontSize(value, preferredFontSize, maxWidth, maxHeight);
        fittedValue = TrimReadablePngLabelToWidth(value, fittedFontSize, maxWidth);
    }

    private static string TrimReadablePngLabelToWidth(string value, double fontSize, double maxWidth) {
        if (string.IsNullOrEmpty(value) || EstimatePngEmphasizedTextWidth(value, fontSize) <= maxWidth) return value;
        const string suffix = "...";
        if (EstimatePngEmphasizedTextWidth(suffix, fontSize) > maxWidth) return string.Empty;
        var low = 0;
        var high = value.Length;
        while (low < high) {
            var mid = low + (high - low + 1) / 2;
            var candidate = value.Substring(0, mid).TrimEnd() + suffix;
            if (EstimatePngEmphasizedTextWidth(candidate, fontSize) <= maxWidth) low = mid;
            else high = mid - 1;
        }

        return low == 0 ? suffix : value.Substring(0, low).TrimEnd() + suffix;
    }

    private static string TrimPngLabelToWidth(string value, double fontSize, double maxWidth) {
        if (string.IsNullOrEmpty(value) || EstimatePngTextWidth(value, fontSize) <= maxWidth) return value;
        const string suffix = "...";
        if (EstimatePngTextWidth(suffix, fontSize) > maxWidth) return string.Empty;
        var low = 0;
        var high = value.Length;
        while (low < high) {
            var mid = low + (high - low + 1) / 2;
            var candidate = value.Substring(0, mid).TrimEnd() + suffix;
            if (EstimatePngTextWidth(candidate, fontSize) <= maxWidth) low = mid;
            else high = mid - 1;
        }

        return low == 0 ? suffix : value.Substring(0, low).TrimEnd() + suffix;
    }
}
