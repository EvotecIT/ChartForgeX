using System;
using System.Collections.Generic;
using System.Globalization;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;

namespace ChartForgeX.VisualBlocks;

internal static class VisualBlockRendering {
    public static void Validate(IVisualBlock block) {
        if (block == null) throw new ArgumentNullException(nameof(block));
        if (block is ChartTable table) {
            if (table.Columns.Count == 0) throw new InvalidOperationException("Chart tables must contain at least one column.");
            foreach (var row in table.Rows) if (row.Cells.Count != table.Columns.Count) throw new InvalidOperationException("Chart table rows must match the column count.");
            return;
        }

        if (block is ChartList list) {
            if (list.Items.Count == 0) throw new InvalidOperationException("Chart lists must contain at least one item.");
            return;
        }

        if (block is MetricCard card) {
            if (card.Label.Length == 0) throw new InvalidOperationException("Metric cards must define a label.");
            if (card.Value.Length == 0) throw new InvalidOperationException("Metric cards must define a value.");
            if (card.Symbol.Length > 12) throw new InvalidOperationException("Metric card symbols must be twelve characters or fewer.");
            foreach (var detail in card.Details) {
                if (detail.Label.Length == 0) throw new InvalidOperationException("Metric card details must define a label.");
                if (detail.Value.Length == 0) throw new InvalidOperationException("Metric card details must define a value.");
            }

            if (card.ActionLabel.Length > 48) throw new InvalidOperationException("Metric card action labels must be forty-eight characters or fewer.");
            if (card.ActionSymbol.Length > 4) throw new InvalidOperationException("Metric card action symbols must be four characters or fewer.");
            if (card.ActionUrl.Length > 0 && !IsSafeActionUrl(card.ActionUrl)) throw new InvalidOperationException("Metric card action URLs must be relative URLs, http(s), or mailto links.");
            if (card.MiniBarMinimum.HasValue && card.MiniBarMaximum.HasValue && card.MiniBarMaximum.Value <= card.MiniBarMinimum.Value) throw new InvalidOperationException("Metric card mini bar maximum must be greater than minimum.");
            if (card.MiniBarHighlightIndex.HasValue && card.MiniBarHighlightIndex.Value >= card.MiniBars.Count) throw new InvalidOperationException("Metric card mini bar highlight index must reference an existing mini bar.");
            if (card.MiniSparklineMinimum.HasValue && card.MiniSparklineMaximum.HasValue && card.MiniSparklineMaximum.Value <= card.MiniSparklineMinimum.Value) throw new InvalidOperationException("Metric card mini sparkline maximum must be greater than minimum.");
            return;
        }

        if (block is RadialMetricCard radialCard) {
            if (radialCard.Label.Length == 0) throw new InvalidOperationException("Radial metric cards must define a label.");
            if (radialCard.Value.Length == 0) throw new InvalidOperationException("Radial metric cards must define a value.");
            if (radialCard.Layers.Count == 0) throw new InvalidOperationException("Radial metric cards must contain at least one radial layer.");
            foreach (var layer in radialCard.Layers) if (layer.Maximum <= layer.Minimum) throw new InvalidOperationException("Radial metric card layer maximum must be greater than minimum.");
            return;
        }

        throw new NotSupportedException("Unsupported visual block type: " + block.GetType().FullName);
    }

    public static ChartColor StatusColor(ChartTheme theme, VisualStatus status) {
        switch (status) {
            case VisualStatus.Positive: return theme.Positive;
            case VisualStatus.Warning: return theme.Warning;
            case VisualStatus.Negative: return theme.Negative;
            case VisualStatus.Info: return PaletteAt(theme, 0);
            case VisualStatus.Neutral: return theme.MutedText;
            default: return theme.MutedText;
        }
    }

    public static VisualStatus ParseStatus(string value) {
        if (string.IsNullOrWhiteSpace(value)) return VisualStatus.None;
        var text = value.Trim();
        if (EqualsAny(text, "ok", "healthy", "success", "pass", "passed", "online", "ready")) return VisualStatus.Positive;
        if (EqualsAny(text, "warn", "warning", "attention", "degraded", "partial")) return VisualStatus.Warning;
        if (EqualsAny(text, "error", "failed", "fail", "critical", "down", "offline")) return VisualStatus.Negative;
        if (EqualsAny(text, "info", "note", "pending", "unknown")) return VisualStatus.Info;
        return VisualStatus.Neutral;
    }

    public static ChartColor PaletteAt(ChartTheme theme, int index) {
        var palette = theme.Palette;
        return palette.Length == 0 ? theme.Text : palette[Math.Abs(index) % palette.Length];
    }

    public static string CssFontFamily(string value) {
        if (string.IsNullOrWhiteSpace(value)) return "system-ui, sans-serif";
        return value.Replace(";", " ").Replace("{", " ").Replace("}", " ").Replace("<", " ").Replace(">", " ");
    }

    public static string Escape(string value) => value.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;");

    public static string StableHash(params string[] values) {
        unchecked {
            var hash = 2166136261u;
            foreach (var value in values) {
                Add(ref hash, value.Length.ToString(CultureInfo.InvariantCulture));
                Add(ref hash, ":");
                Add(ref hash, value);
                Add(ref hash, "|");
            }

            return hash.ToString("x8", CultureInfo.InvariantCulture);
        }
    }

    public static double EstimateTextWidth(string text, double fontSize) {
        var width = 0.0;
        foreach (var ch in text) width += char.IsWhiteSpace(ch) ? 0.32 : char.IsUpper(ch) || char.IsDigit(ch) ? 0.62 : 0.54;
        return width * fontSize;
    }

    public static string FitText(string value, double fontSize, double maxWidth) {
        if (string.IsNullOrEmpty(value) || EstimateTextWidth(value, fontSize) <= maxWidth) return value;
        const string suffix = "...";
        if (EstimateTextWidth(suffix, fontSize) > maxWidth) return string.Empty;
        var low = 0;
        var high = value.Length;
        while (low < high) {
            var mid = (low + high + 1) / 2;
            if (EstimateTextWidth(value.Substring(0, mid) + suffix, fontSize) <= maxWidth) low = mid;
            else high = mid - 1;
        }

        return value.Substring(0, low) + suffix;
    }

    public static ChartRect ContentRect(VisualBlockOptions options) {
        return new ChartRect(
            options.Padding.Left,
            options.Padding.Top,
            Math.Max(1, options.Size.Width - options.Padding.Left - options.Padding.Right),
            Math.Max(1, options.Size.Height - options.Padding.Top - options.Padding.Bottom));
    }

    public static ChartColor SurfaceBackground(VisualBlockOptions options) => options.TransparentBackground ? ChartColor.Transparent : options.Theme.Background;

    public static ChartColor CardBackground(VisualBlockOptions options) => options.Theme.CardBackground;

    public static (double Minimum, double Maximum) MiniBarBounds(MetricCard card) {
        return ValueBounds(card.MiniBars, card.MiniBarMinimum, card.MiniBarMaximum, includeZero: true);
    }

    public static int MiniBarHighlightIndex(MetricCard card) => card.MiniBarHighlightIndex ?? card.MiniBars.Count - 1;

    public static (double Minimum, double Maximum) MiniSparklineBounds(MetricCard card) {
        return ValueBounds(card.MiniSparkline, card.MiniSparklineMinimum, card.MiniSparklineMaximum, includeZero: false);
    }

    public static VisualMiniBar[] CreateMiniBars(MetricCard card, double x, double y, double width, double height) {
        var theme = card.Options.Theme;
        var bounds = MiniBarBounds(card);
        var highlight = MiniBarHighlightIndex(card);
        var count = card.MiniBars.Count;
        var gap = count > 5 ? ChartVisualPrimitives.MiniBarDenseGap : ChartVisualPrimitives.MiniBarGap;
        var barWidth = Math.Max(ChartVisualPrimitives.MiniBarMinWidth, (width - gap * Math.Max(0, count - 1)) / count);
        var activeColor = card.MiniBarColor ?? (card.Status == VisualStatus.None ? PaletteAt(theme, 0) : StatusColor(theme, card.Status));
        var mutedColor = card.MiniBarMutedColor ?? theme.MutedText.WithAlpha(85);
        var bars = new VisualMiniBar[count];
        for (var i = 0; i < count; i++) {
            var value = card.MiniBars[i];
            var ratio = Math.Max(0, Math.Min(1, (value - bounds.Minimum) / (bounds.Maximum - bounds.Minimum)));
            var barHeight = Math.Max(ChartVisualPrimitives.MiniBarMinHeight, height * ratio);
            var currentX = x + i * (barWidth + gap);
            var currentY = y + height - barHeight;
            var highlighted = i == highlight;
            var color = highlighted ? activeColor : mutedColor.WithAlpha((byte)Math.Min(255, Math.Round(mutedColor.A * ChartVisualPrimitives.MiniBarMutedOpacity)));
            bars[i] = new VisualMiniBar(i, value, currentX, currentY, barWidth, barHeight, Math.Min(ChartVisualPrimitives.MiniBarRadiusMax, barWidth * 0.45), color, highlighted);
        }

        return bars;
    }

    public static VisualMiniSparkline CreateMiniSparkline(MetricCard card, double x, double y, double width, double height) {
        var theme = card.Options.Theme;
        var bounds = MiniSparklineBounds(card);
        var color = card.MiniSparklineColor ?? (card.Status == VisualStatus.None ? PaletteAt(theme, 0) : StatusColor(theme, card.Status));
        var fillColor = card.MiniSparklineFillColor ?? color.WithAlpha((byte)Math.Round(255 * ChartVisualPrimitives.MiniSparklineFillOpacity));
        var points = new ChartPoint[card.MiniSparkline.Count];
        var step = width / Math.Max(1, card.MiniSparkline.Count - 1);
        for (var i = 0; i < card.MiniSparkline.Count; i++) {
            var ratio = Math.Max(0, Math.Min(1, (card.MiniSparkline[i] - bounds.Minimum) / (bounds.Maximum - bounds.Minimum)));
            points[i] = new ChartPoint(x + i * step, y + height - ratio * height);
        }

        var area = new ChartPoint[points.Length + 2];
        area[0] = new ChartPoint(points[0].X, y + height);
        for (var i = 0; i < points.Length; i++) area[i + 1] = points[i];
        area[area.Length - 1] = new ChartPoint(points[points.Length - 1].X, y + height);
        return new VisualMiniSparkline(points, area, color, fillColor, ChartVisualPrimitives.MiniSparklineStrokeWidth, ChartVisualPrimitives.MiniSparklineCurrentRadius);
    }

    private static bool IsSafeActionUrl(string value) {
        if (string.IsNullOrWhiteSpace(value)) return false;
        var text = value.Trim();
        if (text.StartsWith("#", StringComparison.Ordinal) || text.StartsWith("/", StringComparison.Ordinal) || text.StartsWith("./", StringComparison.Ordinal) || text.StartsWith("../", StringComparison.Ordinal)) return true;
        if (!Uri.TryCreate(text, UriKind.Absolute, out var uri)) return false;
        return string.Equals(uri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase)
            || string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)
            || string.Equals(uri.Scheme, Uri.UriSchemeMailto, StringComparison.OrdinalIgnoreCase);
    }

    private static bool EqualsAny(string text, params string[] values) {
        foreach (var value in values) if (string.Equals(text, value, StringComparison.OrdinalIgnoreCase)) return true;
        return false;
    }

    private static double Minimum(IReadOnlyList<double> values) {
        var minimum = double.PositiveInfinity;
        foreach (var value in values) minimum = Math.Min(minimum, value);
        return minimum;
    }

    private static double Maximum(IReadOnlyList<double> values) {
        var maximum = double.NegativeInfinity;
        foreach (var value in values) maximum = Math.Max(maximum, value);
        return maximum;
    }

    private static (double Minimum, double Maximum) ValueBounds(IReadOnlyList<double> values, double? configuredMinimum, double? configuredMaximum, bool includeZero) {
        var minimum = configuredMinimum ?? (includeZero ? Math.Min(0, Minimum(values)) : Minimum(values));
        var maximum = configuredMaximum ?? (includeZero ? Math.Max(0, Maximum(values)) : Maximum(values));
        if (maximum <= minimum) maximum = minimum + 1;
        return (minimum, maximum);
    }

    private static void Add(ref uint hash, string value) {
        foreach (var ch in value) {
            hash ^= ch;
            hash *= 16777619u;
        }
    }
}
