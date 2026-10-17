using System;
using System.Collections.Generic;

namespace ChartForgeX.Composition;

internal enum VisualCanvasInfoTileTextRole {
    Label,
    Value,
    Detail
}

internal sealed class VisualCanvasInfoTileMetrics {
    public VisualCanvasInfoTileMetrics(double x, double y, double width, double height, double padX, double iconBox, double iconX, double iconY, double textX, double textMax, bool hasMiniChart, double chartX, double chartY, double chartWidth, double chartHeight, double progressRailY, double progressRailHeight) {
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
        ProgressRailY = progressRailY;
        ProgressRailHeight = progressRailHeight;
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
    public double ProgressRailY { get; }
    public double ProgressRailHeight { get; }
}

internal sealed class VisualCanvasInfoTileTextLine {
    public VisualCanvasInfoTileTextLine(VisualCanvasInfoTileTextRole role, string text, double x, double y, double fontSize, bool truncated) {
        Role = role;
        Text = text;
        X = x;
        Y = y;
        FontSize = fontSize;
        Truncated = truncated;
    }

    public VisualCanvasInfoTileTextRole Role { get; }
    public string Text { get; }
    public double X { get; }
    public double Y { get; }
    public double FontSize { get; }
    /// <summary>The CSS weight the role asks for; SVG writes it and PNG resolves the same face from it.</summary>
    public int Weight => WeightFor(Role);
    public bool Truncated { get; }

    public static int WeightFor(VisualCanvasInfoTileTextRole role) =>
        role == VisualCanvasInfoTileTextRole.Label ? VisualCanvasFontWeights.TileLabel : role == VisualCanvasInfoTileTextRole.Value ? VisualCanvasFontWeights.TileValue : VisualCanvasFontWeights.TileDetail;
}

internal sealed class VisualCanvasInfoTileTextLayoutResult {
    public VisualCanvasInfoTileTextLayoutResult(IReadOnlyList<VisualCanvasInfoTileTextLine> lines, bool hasTruncatedText, bool hasVerticalOverflow, double textWidth, double textHeight, double scale) {
        Lines = lines;
        HasTruncatedText = hasTruncatedText;
        HasVerticalOverflow = hasVerticalOverflow;
        TextWidth = textWidth;
        TextHeight = textHeight;
        Scale = scale;
    }

    public IReadOnlyList<VisualCanvasInfoTileTextLine> Lines { get; }
    public bool HasTruncatedText { get; }
    public bool HasVerticalOverflow { get; }
    public bool WasScaledDown { get; set; }
    public double TextWidth { get; }
    public double TextHeight { get; }
    public double Scale { get; }
}

internal sealed class VisualCanvasInfoTileRenderLayout {
    public VisualCanvasInfoTileRenderLayout(VisualCanvasInfoTileMetrics metrics,
        VisualCanvasInfoTileTextLayoutResult text) {
        Metrics = metrics;
        Text = text;
    }

    public VisualCanvasInfoTileMetrics Metrics { get; }
    public VisualCanvasInfoTileTextLayoutResult Text { get; }
}

internal static class VisualCanvasInfoTileTextLayout {
    public static VisualCanvasInfoTileRenderLayout CalculateLayout(VisualCanvasInfoTileLayer tile,
        ResolvedVisualCanvasStyle theme) {
        var metrics = CalculateMetrics(tile, theme);
        var text = BuildResult(tile, metrics.Y, metrics.Height, metrics.TextX, metrics.TextMax, theme);
        var requestedScale = tile.TextScale ?? theme.TileTextScale;
        if (requestedScale >= 1 && text.WasScaledDown && metrics.IconBox < 24) {
            metrics = CalculateMetrics(tile, theme, preserveFallbackIcon: true);
            text = BuildResult(tile, metrics.Y, metrics.Height, metrics.TextX, metrics.TextMax, theme);
        }

        return new VisualCanvasInfoTileRenderLayout(metrics, text);
    }

    public static VisualCanvasInfoTileMetrics CalculateMetrics(VisualCanvasInfoTileLayer tile,
        ResolvedVisualCanvasStyle theme, bool preserveFallbackIcon = false) {
        var x = Math.Round(tile.X);
        var y = Math.Round(tile.Y);
        var width = Math.Round(tile.Width);
        var height = Math.Round(tile.Height);
        var scale = tile.TextScale ?? theme.TileTextScale;
        var padX = Math.Min(Math.Max(20 * scale, Math.Min(28 * scale, width * 0.06)), width * 0.25);
        var iconBox = Math.Max(1, Math.Min(Math.Max(44 * scale, Math.Min(54 * scale, height - 34 * scale)), height - 12 * (scale - 1)));
        var innerWidth = Math.Max(0, width - 2 * padX);
        var textGap = Math.Min(22 * scale, innerWidth * 0.2);
        var minTextWidth = Math.Min(24 * scale, innerWidth * 0.25);
        if (preserveFallbackIcon) {
            var minIconBox = Math.Min(24, Math.Max(1, height - 12));
            iconBox = Math.Max(iconBox, minIconBox);
            textGap = Math.Min(textGap, Math.Max(0, innerWidth - iconBox));
            minTextWidth = Math.Min(minTextWidth,
                Math.Max(0, innerWidth - textGap - iconBox));
        }
        iconBox = Math.Min(iconBox, Math.Max(1, innerWidth - textGap - minTextWidth));
        textGap = Math.Min(textGap, iconBox * (22d / 54));
        var iconX = x + padX;
        var iconY = y + (height - iconBox) / 2;
        var textX = iconX + iconBox + textGap;
        var hasMiniChart = tile.MiniChartKind != VisualCanvasInfoTileMiniChartKind.None && tile.MiniChartSampleCount > 0;
        var chartWidth = hasMiniChart ? Math.Min(Math.Min(width * 0.24, Math.Max(82 * scale, width * 0.20)), Math.Max(0, x + width - padX - textX - 16 * scale - minTextWidth)) : 0;
        if (chartWidth <= 0) hasMiniChart = false;
        var chartX = x + width - padX - chartWidth;
        var chartY = y + Math.Min(Math.Max(24 * scale, height * 0.30), Math.Max(0, height - 1));
        var chartHeight = Math.Min(Math.Max(28 * scale, Math.Min(46 * scale, height * 0.42)), Math.Max(1, y + height - chartY));
        if (chartWidth < 17 || chartHeight < 15) {
            hasMiniChart = false;
            chartWidth = 0;
            chartX = x + width - padX;
        }
        var textMax = hasMiniChart
            ? Math.Max(0, chartX - textX - 16 * scale)
            : Math.Max(0, x + width - padX - textX);
        var railSize = 8 * scale;
        var railY = y + height - 16 * scale;
        if (railY < y || railY + railSize > y + height) {
            railSize = Math.Min(railSize, Math.Max(1, height * 0.12));
            railY = y + height - 2 * railSize;
        }
        return new VisualCanvasInfoTileMetrics(x, y, width, height, padX, iconBox, iconX, iconY, textX, textMax, hasMiniChart, chartX, chartY, chartWidth, chartHeight, railY, railSize);
    }

    /// <summary>The icon text size both renderers use: the preferred size, reduced until the text fits inside the icon box.</summary>
    public static double IconFontSize(string icon, double iconBox, string fontFamily, TextMeasurementMode mode = TextMeasurementMode.PortableEstimate) {
        var size = iconBox * (icon.Length > 3 ? 0.34 : 0.42);
        var width = VisualCanvasTextFace.Resolve(fontFamily, VisualCanvasFontWeights.Emphasized, mode).Measure(icon, size);
        var available = Math.Max(4, iconBox - 8);
        return width > available ? Math.Max(1, size * available / width) : size;
    }

    /// <summary>Lays out tile text using the resolved theme's font family and measurement mode.</summary>
    public static VisualCanvasInfoTileTextLayoutResult BuildResult(VisualCanvasInfoTileLayer tile, double tileY, double tileHeight, double textX, double maxWidth, ResolvedVisualCanvasStyle theme) {
        VisualCanvas.ValidateEnum(tile.TextFitPolicy, nameof(tile.TextFitPolicy));
        var faces = new TileFaces(theme.FontFamily, theme.TextMeasurementMode);
        var policy = tile.TextFitPolicy == VisualCanvasTextFitPolicy.Auto ? VisualCanvasTextFitPolicy.WrapThenShrink : tile.TextFitPolicy;
        var singleLine = policy == VisualCanvasTextFitPolicy.SingleLineEllipsis || policy == VisualCanvasTextFitPolicy.ShrinkToFit;
        var requestedScale = tile.TextScale ?? theme.TileTextScale;
        var customized = requestedScale != 1 || tile.LabelFontSize.HasValue || tile.ValueFontSize.HasValue || tile.DetailFontSize.HasValue || theme.TileLabelSize != 14 || theme.TileValueSize != 22 || theme.TileDetailSize != 13;
        var scale = requestedScale;
        var best = BuildCore(tile, tileY, tileHeight, textX, maxWidth, singleLine, scale, false, faces, theme);
        if (policy != VisualCanvasTextFitPolicy.ShrinkToFit && policy != VisualCanvasTextFitPolicy.WrapThenShrink) return best;

        var minimumScale = Math.Min(requestedScale, customized ? Math.Max(0.4, requestedScale * 0.45) : 0.72);
        for (var i = 0; i < 24 && RequiresFitAdjustment(best); i++) {
            var nextScale = Math.Max(minimumScale, scale - 0.04);
            if (nextScale >= scale) break;
            var next = BuildCore(tile, tileY, tileHeight, textX, maxWidth, singleLine, nextScale, false, faces, theme);
            best = next;
            scale = nextScale;
            if (!RequiresFitAdjustment(next)) break;
        }

        if (RequiresFitAdjustment(best) && scale > minimumScale) {
            best = BuildCore(tile, tileY, tileHeight, textX, maxWidth, singleLine,
                minimumScale, false, faces, theme);
            scale = minimumScale;
        }

        var detailOmitted = false;
        if (best.HasVerticalOverflow && tile.Detail.Length > 0) {
            var withoutDetail = BuildCore(tile, tileY, tileHeight, textX, maxWidth, singleLine, scale, true, faces, theme);
            if (withoutDetail.TextHeight < best.TextHeight || !withoutDetail.HasVerticalOverflow) {
                best = withoutDetail;
                detailOmitted = true;
            }
        }

        var minimumVerticalScale = Math.Min(requestedScale, 0.4);
        for (var i = 0; best.HasVerticalOverflow && i < 96 && scale > minimumVerticalScale; i++) {
            var nextScale = Math.Max(minimumVerticalScale, scale - 0.04);
            if (nextScale >= scale) break;
            best = BuildCore(tile, tileY, tileHeight, textX, maxWidth, singleLine,
                nextScale, detailOmitted, faces, theme);
            scale = nextScale;
        }

        best.WasScaledDown = best.Scale < requestedScale - 0.001;
        return best;
    }

    private static bool RequiresFitAdjustment(VisualCanvasInfoTileTextLayoutResult result) =>
        result.HasTruncatedText || result.HasVerticalOverflow;

    private static VisualCanvasInfoTileTextLayoutResult BuildCore(VisualCanvasInfoTileLayer tile, double tileY, double tileHeight, double textX, double maxWidth, bool singleLine, double scale, bool omitDetail, TileFaces faces, ResolvedVisualCanvasStyle theme) {
        var smallTileScale = tileHeight < 72 ? 12.0 / 14.0 : 1;
        var labelFont = (tile.LabelFontSize ?? theme.TileLabelSize) * smallTileScale * scale;
        var valueFont = (tile.ValueFontSize ?? theme.TileValueSize) * (tileHeight < 72 ? 17.0 / 22.0 : tileHeight < 92 ? 21.0 / 22.0 : 1) * scale;
        var detailFont = (tile.DetailFontSize ?? theme.TileDetailSize) * (tileHeight < 72 ? 11.0 / 13.0 : 1) * scale;
        var requestedScale = tile.TextScale ?? theme.TileTextScale;
        var layoutScale = requestedScale == 1 ? requestedScale : scale;
        var lineSpacing = 4 * layoutScale;
        var labelLineHeight = labelFont + lineSpacing;
        var valueLineHeight = valueFont + lineSpacing;
        var detailLineHeight = detailFont + lineSpacing;
        var topPadding = Math.Max(10 * layoutScale, Math.Min(18 * layoutScale, tileHeight * 0.16));
        var bottomPadding = tile.Progress.HasValue
            ? 30 * layoutScale
            : Math.Max(12 * layoutScale, Math.Min(18 * layoutScale, tileHeight * 0.15));
        var availableHeight = Math.Max(valueLineHeight, tileHeight - topPadding - bottomPadding);
        var hasDetail = tile.Detail.Length > 0;
        var detailLineLimit = omitDetail || !hasDetail ? 0 : singleLine ? 1 : tileHeight >= 122 ? 2 : 1;
        var valueLineLimit = singleLine ? 1 : Math.Max(1, (int)Math.Floor((availableHeight - labelLineHeight - detailLineLimit * detailLineHeight) / valueLineHeight));
        valueLineLimit = singleLine ? 1 : Math.Min(tileHeight >= 132 ? 3 : 2, valueLineLimit);
        if (valueLineLimit < 1 && detailLineLimit > 0) {
            detailLineLimit = 0;
            valueLineLimit = 1;
        }

        var labelLines = Wrap(tile.Label, labelFont, maxWidth, faces.Label, 1);
        var valueLines = Wrap(tile.Value, valueFont, maxWidth, faces.Value, valueLineLimit);
        var detailLines = detailLineLimit > 0 ? Wrap(tile.Detail, detailFont, maxWidth, faces.Detail, detailLineLimit) : WrapResult.Empty;
        var totalHeight =
            labelLines.Lines.Count * labelLineHeight +
            valueLines.Lines.Count * valueLineHeight +
            detailLines.Lines.Count * detailLineHeight +
            (valueLines.Lines.Count > 0 ? 3 * lineSpacing / 4 : 0) +
            (detailLines.Lines.Count > 0 ? 2 * lineSpacing / 4 : 0);
        var y = tileY + Math.Max(topPadding, (tileHeight - bottomPadding - totalHeight) / 2);
        var lines = new List<VisualCanvasInfoTileTextLine>(labelLines.Lines.Count + valueLines.Lines.Count + detailLines.Lines.Count);
        foreach (var line in labelLines.Lines) {
            lines.Add(new VisualCanvasInfoTileTextLine(VisualCanvasInfoTileTextRole.Label, line.Text, textX, y, labelFont, line.Truncated));
            y += labelLineHeight;
        }

        y += 3 * lineSpacing / 4;
        foreach (var line in valueLines.Lines) {
            lines.Add(new VisualCanvasInfoTileTextLine(VisualCanvasInfoTileTextRole.Value, line.Text, textX, y, valueFont, line.Truncated));
            y += valueLineHeight;
        }

        if (detailLines.Lines.Count > 0) {
            y += 2 * lineSpacing / 4;
            foreach (var line in detailLines.Lines) {
                lines.Add(new VisualCanvasInfoTileTextLine(VisualCanvasInfoTileTextRole.Detail, line.Text, textX, y, detailFont, line.Truncated));
                y += detailLineHeight;
            }
        }

        var textWidth = 0.0;
        foreach (var line in lines) {
            var lineWidth = faces.For(line.Role).Measure(line.Text, line.FontSize);
            if (lineWidth > textWidth) textWidth = lineWidth;
        }

        var textBottom = tileY + Math.Max(topPadding, (tileHeight - bottomPadding - totalHeight) / 2) + totalHeight;
        var hasVerticalOverflow = textBottom > tileY + tileHeight - bottomPadding + 0.5;
        var detailOmitted = hasDetail && detailLineLimit == 0;
        return new VisualCanvasInfoTileTextLayoutResult(lines, labelLines.Truncated || valueLines.Truncated || detailLines.Truncated || detailOmitted, hasVerticalOverflow, textWidth, totalHeight, scale);
    }

    private static WrapResult Wrap(string value, double fontSize, double maxWidth, VisualCanvasTextFace face, int maxLines) {
        if (string.IsNullOrEmpty(value) || maxLines <= 0) return WrapResult.Empty;
        if (face.Measure(value, fontSize) <= maxWidth) return new WrapResult(new[] { new WrappedLine(value, false) }, false);
        var words = value.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        if (words.Length == 0) return new WrapResult(new[] { new WrappedLine(string.Empty, false) }, false);
        var lines = new List<WrappedLine>(maxLines);
        var current = string.Empty;
        var index = 0;
        var truncated = false;
        while (index < words.Length && lines.Count < maxLines) {
            var word = words[index];
            var candidate = current.Length == 0 ? word : current + " " + word;
            if (face.Measure(candidate, fontSize) <= maxWidth) {
                current = candidate;
                index++;
                continue;
            }

            if (current.Length == 0) {
                var fitted = FitText(word, fontSize, maxWidth, face, index < words.Length - 1);
                truncated |= fitted.Truncated || index < words.Length - 1;
                lines.Add(new WrappedLine(fitted.Text, fitted.Truncated || index < words.Length - 1));
                index++;
                continue;
            }

            if (lines.Count == maxLines - 1) {
                var remainder = current + " " + string.Join(" ", words, index, words.Length - index);
                var fitted = FitText(remainder, fontSize, maxWidth, face, true);
                truncated = true;
                lines.Add(new WrappedLine(fitted.Text, true));
                return new WrapResult(lines, truncated);
            }

            lines.Add(new WrappedLine(current, false));
            current = string.Empty;
        }

        if (current.Length > 0 && lines.Count < maxLines) {
            var fitted = FitText(current, fontSize, maxWidth, face, index < words.Length && face.Measure(current, fontSize) > maxWidth);
            truncated |= fitted.Truncated || index < words.Length;
            lines.Add(new WrappedLine(fitted.Text, fitted.Truncated || index < words.Length));
        } else if (index < words.Length) {
            truncated = true;
        }

        return new WrapResult(lines, truncated);
    }

    private static FitResult FitText(string value, double fontSize, double maxWidth, VisualCanvasTextFace face, bool forceSuffix) {
        if (string.IsNullOrEmpty(value)) return new FitResult(string.Empty, false);
        const string suffix = "...";
        if (!forceSuffix && face.Measure(value, fontSize) <= maxWidth) return new FitResult(value, false);
        if (face.Measure(suffix, fontSize) > maxWidth) return new FitResult(string.Empty, true);
        var low = 0;
        var high = value.Length;
        while (low < high) {
            var mid = (low + high + 1) / 2;
            if (face.Measure(value.Substring(0, mid).TrimEnd() + suffix, fontSize) <= maxWidth) low = mid;
            else high = mid - 1;
        }

        return new FitResult(value.Substring(0, Typography.TextElementBoundary.Snap(value, low)).TrimEnd() + suffix, true);
    }

    private readonly struct TileFaces {
        public TileFaces(string fontFamily, TextMeasurementMode mode) {
            Label = VisualCanvasTextFace.Resolve(fontFamily, VisualCanvasFontWeights.TileLabel, mode);
            Value = VisualCanvasTextFace.Resolve(fontFamily, VisualCanvasFontWeights.TileValue, mode);
            Detail = VisualCanvasTextFace.Resolve(fontFamily, VisualCanvasFontWeights.TileDetail, mode);
        }

        public VisualCanvasTextFace Label { get; }
        public VisualCanvasTextFace Value { get; }
        public VisualCanvasTextFace Detail { get; }

        public VisualCanvasTextFace For(VisualCanvasInfoTileTextRole role) =>
            role == VisualCanvasInfoTileTextRole.Label ? Label : role == VisualCanvasInfoTileTextRole.Value ? Value : Detail;
    }

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
