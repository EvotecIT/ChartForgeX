using System;
using System.Collections.Generic;
using ChartForgeX.Raster;

namespace ChartForgeX.Composition;

internal enum VisualCanvasInfoTileTextRole {
    Label,
    Value,
    Detail
}

internal sealed class VisualCanvasInfoTileMetrics {
    public VisualCanvasInfoTileMetrics(double x, double y, double width, double height, double padX, double iconBox, double iconX, double iconY, double textX, double textMax, bool hasMiniChart, double chartX, double chartY, double chartWidth, double chartHeight) {
        X = x;
        Y = y;
        Width = width;
        Height = height;
        PadX = padX;
        IconBox = iconBox;
        IconX = iconX;
        IconY = iconY;
        TextX = textX;
        TextMax = textMax;
        HasMiniChart = hasMiniChart;
        ChartX = chartX;
        ChartY = chartY;
        ChartWidth = chartWidth;
        ChartHeight = chartHeight;
    }

    public double X { get; }
    public double Y { get; }
    public double Width { get; }
    public double Height { get; }
    public double PadX { get; }
    public double IconBox { get; }
    public double IconX { get; }
    public double IconY { get; }
    public double TextX { get; }
    public double TextMax { get; }
    public bool HasMiniChart { get; }
    public double ChartX { get; }
    public double ChartY { get; }
    public double ChartWidth { get; }
    public double ChartHeight { get; }
}

internal sealed class VisualCanvasInfoTileTextLine {
    public VisualCanvasInfoTileTextLine(VisualCanvasInfoTileTextRole role, string text, double x, double y, double fontSize, bool emphasized, bool truncated) {
        Role = role;
        Text = text;
        X = x;
        Y = y;
        FontSize = fontSize;
        Emphasized = emphasized;
        Truncated = truncated;
    }

    public VisualCanvasInfoTileTextRole Role { get; }
    public string Text { get; }
    public double X { get; }
    public double Y { get; }
    public double FontSize { get; }
    public bool Emphasized { get; }
    public bool Truncated { get; }
}

internal sealed class VisualCanvasInfoTileTextLayoutResult {
    public VisualCanvasInfoTileTextLayoutResult(IReadOnlyList<VisualCanvasInfoTileTextLine> lines, bool hasTruncatedText, double textWidth, double textHeight) {
        Lines = lines;
        HasTruncatedText = hasTruncatedText;
        TextWidth = textWidth;
        TextHeight = textHeight;
    }

    public IReadOnlyList<VisualCanvasInfoTileTextLine> Lines { get; }
    public bool HasTruncatedText { get; }
    public double TextWidth { get; }
    public double TextHeight { get; }
}

internal static class VisualCanvasInfoTileTextLayout {
    public static VisualCanvasInfoTileMetrics CalculateMetrics(VisualCanvasInfoTileLayer tile) {
        var x = Math.Round(tile.X);
        var y = Math.Round(tile.Y);
        var width = Math.Round(tile.Width);
        var height = Math.Round(tile.Height);
        var padX = Math.Max(20, Math.Min(28, width * 0.06));
        var iconBox = Math.Max(44, Math.Min(54, height - 34));
        var iconX = x + padX;
        var iconY = y + (height - iconBox) / 2;
        var textX = iconX + iconBox + 22;
        var hasMiniChart = tile.MiniChartKind != VisualCanvasInfoTileMiniChartKind.None && tile.MiniChartValues.Count > 0;
        var chartWidth = hasMiniChart ? Math.Min(width * 0.24, Math.Max(82, width * 0.20)) : 0;
        var chartX = x + width - padX - chartWidth;
        var chartY = y + Math.Max(24, height * 0.30);
        var chartHeight = Math.Max(28, Math.Min(46, height * 0.42));
        var textMax = hasMiniChart ? Math.Max(24, chartX - textX - 16) : Math.Max(24, width - (textX - x) - padX);
        return new VisualCanvasInfoTileMetrics(x, y, width, height, padX, iconBox, iconX, iconY, textX, textMax, hasMiniChart, chartX, chartY, chartWidth, chartHeight);
    }

    public static IReadOnlyList<VisualCanvasInfoTileTextLine> Build(VisualCanvasInfoTileLayer tile, double tileY, double tileHeight, double textX, double maxWidth) =>
        BuildResult(tile, tileY, tileHeight, textX, maxWidth).Lines;

    public static VisualCanvasInfoTileTextLayoutResult BuildForTile(VisualCanvasInfoTileLayer tile) {
        var metrics = CalculateMetrics(tile);
        return BuildResult(tile, metrics.Y, metrics.Height, metrics.TextX, metrics.TextMax);
    }

    public static VisualCanvasInfoTileTextLayoutResult BuildResult(VisualCanvasInfoTileLayer tile, double tileY, double tileHeight, double textX, double maxWidth) {
        VisualCanvas.ValidateEnum(tile.TextFitPolicy, nameof(tile.TextFitPolicy));
        var policy = tile.TextFitPolicy == VisualCanvasTextFitPolicy.Auto ? VisualCanvasTextFitPolicy.WrapThenShrink : tile.TextFitPolicy;
        var singleLine = policy == VisualCanvasTextFitPolicy.SingleLineEllipsis || policy == VisualCanvasTextFitPolicy.ShrinkToFit;
        var scale = 1.0;
        var best = BuildCore(tile, tileY, tileHeight, textX, maxWidth, singleLine, scale);
        if (policy != VisualCanvasTextFitPolicy.ShrinkToFit && policy != VisualCanvasTextFitPolicy.WrapThenShrink) return best;

        for (var i = 0; i < 8 && best.HasTruncatedText; i++) {
            scale -= 0.04;
            if (scale < 0.72) break;
            var next = BuildCore(tile, tileY, tileHeight, textX, maxWidth, singleLine, scale);
            best = next;
            if (!next.HasTruncatedText) break;
        }

        return best;
    }

    private static VisualCanvasInfoTileTextLayoutResult BuildCore(VisualCanvasInfoTileLayer tile, double tileY, double tileHeight, double textX, double maxWidth, bool singleLine, double scale) {
        var labelFont = Math.Max(10, (tileHeight < 72 ? 12.0 : 14.0) * scale);
        var valueFont = Math.Max(13, (tileHeight < 72 ? 17.0 : tileHeight < 92 ? 21.0 : 22.0) * scale);
        var detailFont = Math.Max(10, (tileHeight < 72 ? 11.0 : 13.0) * scale);
        var labelLineHeight = labelFont + 4;
        var valueLineHeight = valueFont + 4;
        var detailLineHeight = detailFont + 4;
        var topPadding = Math.Max(10, Math.Min(18, tileHeight * 0.16));
        var bottomPadding = tile.Progress.HasValue ? 30.0 : Math.Max(12, Math.Min(18, tileHeight * 0.15));
        var availableHeight = Math.Max(valueLineHeight, tileHeight - topPadding - bottomPadding);
        var hasDetail = tile.Detail.Length > 0;
        var detailLineLimit = singleLine ? (hasDetail ? 1 : 0) : hasDetail ? tileHeight >= 122 ? 2 : 1 : 0;
        var valueLineLimit = singleLine ? 1 : Math.Max(1, (int)Math.Floor((availableHeight - labelLineHeight - detailLineLimit * detailLineHeight) / valueLineHeight));
        valueLineLimit = singleLine ? 1 : Math.Min(tileHeight >= 132 ? 3 : 2, valueLineLimit);
        if (valueLineLimit < 1 && detailLineLimit > 0) {
            detailLineLimit = 0;
            valueLineLimit = 1;
        }

        var labelLines = Wrap(tile.Label, labelFont, maxWidth, true, 1);
        var valueLines = Wrap(tile.Value, valueFont, maxWidth, true, valueLineLimit);
        var detailLines = detailLineLimit > 0 ? Wrap(tile.Detail, detailFont, maxWidth, false, detailLineLimit) : WrapResult.Empty;
        var totalHeight =
            labelLines.Lines.Count * labelLineHeight +
            valueLines.Lines.Count * valueLineHeight +
            detailLines.Lines.Count * detailLineHeight +
            (valueLines.Lines.Count > 0 ? 3 : 0) +
            (detailLines.Lines.Count > 0 ? 2 : 0);
        var y = tileY + Math.Max(topPadding, (tileHeight - bottomPadding - totalHeight) / 2);
        var lines = new List<VisualCanvasInfoTileTextLine>(labelLines.Lines.Count + valueLines.Lines.Count + detailLines.Lines.Count);
        foreach (var line in labelLines.Lines) {
            lines.Add(new VisualCanvasInfoTileTextLine(VisualCanvasInfoTileTextRole.Label, line.Text, textX, y, labelFont, true, line.Truncated));
            y += labelLineHeight;
        }

        y += 3;
        foreach (var line in valueLines.Lines) {
            lines.Add(new VisualCanvasInfoTileTextLine(VisualCanvasInfoTileTextRole.Value, line.Text, textX, y, valueFont, true, line.Truncated));
            y += valueLineHeight;
        }

        if (detailLines.Lines.Count > 0) {
            y += 2;
            foreach (var line in detailLines.Lines) {
                lines.Add(new VisualCanvasInfoTileTextLine(VisualCanvasInfoTileTextRole.Detail, line.Text, textX, y, detailFont, false, line.Truncated));
                y += detailLineHeight;
            }
        }

        var textWidth = 0.0;
        foreach (var line in lines) {
            var lineWidth = Measure(line.Text, line.FontSize, line.Emphasized);
            if (lineWidth > textWidth) textWidth = lineWidth;
        }

        return new VisualCanvasInfoTileTextLayoutResult(lines, labelLines.Truncated || valueLines.Truncated || detailLines.Truncated, textWidth, totalHeight);
    }

    private static WrapResult Wrap(string value, double fontSize, double maxWidth, bool emphasized, int maxLines) {
        if (string.IsNullOrEmpty(value) || maxLines <= 0) return WrapResult.Empty;
        var words = value.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        if (words.Length == 0) return new WrapResult(new[] { new WrappedLine(string.Empty, false) }, false);
        var lines = new List<WrappedLine>(maxLines);
        var current = string.Empty;
        var index = 0;
        var truncated = false;
        while (index < words.Length && lines.Count < maxLines) {
            var word = words[index];
            var candidate = current.Length == 0 ? word : current + " " + word;
            if (Measure(candidate, fontSize, emphasized) <= maxWidth) {
                current = candidate;
                index++;
                continue;
            }

            if (current.Length == 0) {
                var fitted = FitText(word, fontSize, maxWidth, emphasized, index < words.Length - 1);
                truncated |= fitted.Truncated || index < words.Length - 1;
                lines.Add(new WrappedLine(fitted.Text, fitted.Truncated || index < words.Length - 1));
                index++;
                continue;
            }

            if (lines.Count == maxLines - 1) {
                var remainder = current + " " + string.Join(" ", words, index, words.Length - index);
                var fitted = FitText(remainder, fontSize, maxWidth, emphasized, true);
                truncated = true;
                lines.Add(new WrappedLine(fitted.Text, true));
                return new WrapResult(lines, truncated);
            }

            lines.Add(new WrappedLine(current, false));
            current = string.Empty;
        }

        if (current.Length > 0 && lines.Count < maxLines) {
            var fitted = FitText(current, fontSize, maxWidth, emphasized, index < words.Length && Measure(current, fontSize, emphasized) > maxWidth);
            truncated |= fitted.Truncated || index < words.Length;
            lines.Add(new WrappedLine(fitted.Text, fitted.Truncated || index < words.Length));
        } else if (index < words.Length) {
            truncated = true;
        }

        return new WrapResult(lines, truncated);
    }

    private static FitResult FitText(string value, double fontSize, double maxWidth, bool emphasized, bool forceSuffix) {
        if (string.IsNullOrEmpty(value)) return new FitResult(string.Empty, false);
        const string suffix = "...";
        if (!forceSuffix && Measure(value, fontSize, emphasized) <= maxWidth) return new FitResult(value, false);
        if (Measure(suffix, fontSize, emphasized) > maxWidth) return new FitResult(string.Empty, true);
        var low = 0;
        var high = value.Length;
        while (low < high) {
            var mid = (low + high + 1) / 2;
            if (Measure(value.Substring(0, mid).TrimEnd() + suffix, fontSize, emphasized) <= maxWidth) low = mid;
            else high = mid - 1;
        }

        return new FitResult(value.Substring(0, low).TrimEnd() + suffix, true);
    }

    private static double Measure(string value, double fontSize, bool emphasized) =>
        emphasized ? RgbaCanvas.MeasureTextEmphasizedWidth(value, fontSize, null) : RgbaCanvas.MeasureTextWidth(value, fontSize, null);

    private readonly struct FitResult {
        public FitResult(string text, bool truncated) {
            Text = text;
            Truncated = truncated;
        }

        public string Text { get; }
        public bool Truncated { get; }
    }

    private readonly struct WrappedLine {
        public WrappedLine(string text, bool truncated) {
            Text = text;
            Truncated = truncated;
        }

        public string Text { get; }
        public bool Truncated { get; }
    }

    private sealed class WrapResult {
        public static readonly WrapResult Empty = new(Array.Empty<WrappedLine>(), false);

        public WrapResult(IReadOnlyList<WrappedLine> lines, bool truncated) {
            Lines = lines;
            Truncated = truncated;
        }

        public IReadOnlyList<WrappedLine> Lines { get; }
        public bool Truncated { get; }
    }
}
