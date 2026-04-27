using System;
using ChartForgeX.Core;
using ChartForgeX.Primitives;

namespace ChartForgeX.Raster;

public sealed partial class PngChartRenderer {
    private static void DrawReadablePngLabel(RgbaCanvas c, double x, double y, string label, ChartColor text, ChartColor halo, double fontSize) {
        var strongHalo = ApplyOpacity(halo, 0.72);
        var softHalo = ApplyOpacity(halo, 0.45);
        c.DrawText(x - 1, y, label, strongHalo, fontSize);
        c.DrawText(x + 1, y, label, strongHalo, fontSize);
        c.DrawText(x, y - 1, label, strongHalo, fontSize);
        c.DrawText(x, y + 1, label, strongHalo, fontSize);
        c.DrawText(x - 1, y - 1, label, softHalo, fontSize);
        c.DrawText(x + 1, y - 1, label, softHalo, fontSize);
        c.DrawText(x - 1, y + 1, label, softHalo, fontSize);
        c.DrawText(x + 1, y + 1, label, softHalo, fontSize);
        c.DrawTextEmphasized(x, y, label, text, fontSize);
    }

    private static void DrawReadablePngLabel(RgbaCanvas c, ChartRect plot, double x, double y, string label, ChartColor text, ChartColor halo, double fontSize) {
        FitReadablePngLabel(label, fontSize, Math.Max(8, plot.Width - 4), Math.Max(8, plot.Height - 4), out var fittedLabel, out var fittedFontSize);
        if (fittedLabel.Length == 0) return;
        var width = EstimatePngEmphasizedTextWidth(fittedLabel, fittedFontSize);
        var height = EstimatePngTextHeight(fittedFontSize);
        DrawReadablePngLabel(c, Clamp(x, plot.Left + 2, plot.Right - width - 2), Clamp(y, plot.Top + 2, plot.Bottom - height - 2), fittedLabel, text, halo, fittedFontSize);
    }

    private static void DrawReadablePngLabelCentered(RgbaCanvas c, ChartRect bounds, string label, ChartColor text, ChartColor halo, double fontSize) {
        FitReadablePngLabel(label, fontSize, Math.Max(8, bounds.Width - 4), Math.Max(8, bounds.Height - 4), out var fittedLabel, out var fittedFontSize);
        if (fittedLabel.Length == 0) return;
        var width = EstimatePngEmphasizedTextWidth(fittedLabel, fittedFontSize);
        var height = EstimatePngTextHeight(fittedFontSize);
        DrawReadablePngLabel(c, bounds.Left + (bounds.Width - width) / 2.0, bounds.Top + (bounds.Height - height) / 2.0, fittedLabel, text, halo, fittedFontSize);
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
    private static double PngTickFontSize(Chart chart) => chart.Options.Theme.TickLabelFontSize;
    private static double PngAxisTitleFontSize(Chart chart) => chart.Options.Theme.AxisTitleFontSize;
    private static double PngLegendFontSize(Chart chart) => chart.Options.Theme.LegendFontSize;
    private static int DetailTextScale(Chart chart) => chart.Options.Size.Width >= 1000 && chart.Options.Size.Height >= 560 ? 2 : 1;
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
