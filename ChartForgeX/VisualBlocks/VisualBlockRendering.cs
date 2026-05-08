using System;
using System.Globalization;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
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

    private static bool EqualsAny(string text, params string[] values) {
        foreach (var value in values) if (string.Equals(text, value, StringComparison.OrdinalIgnoreCase)) return true;
        return false;
    }

    private static void Add(ref uint hash, string value) {
        foreach (var ch in value) {
            hash ^= ch;
            hash *= 16777619u;
        }
    }
}
