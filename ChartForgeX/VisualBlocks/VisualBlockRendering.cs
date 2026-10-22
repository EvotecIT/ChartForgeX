using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using ChartForgeX.Typography;

namespace ChartForgeX.VisualBlocks;

internal static partial class VisualBlockRendering {

    // Native block roles retain their relative sizes while sharing the chart body's typography setting.
    public static double BodyFontSize(ResolvedVisualTheme theme, double defaultSize) => defaultSize * theme.DataLabelFontSize / ResolvedVisualTheme.DefaultDataLabelFontSize;

    // Headings share a bounded portion of a fixed viewport; the remaining space belongs to the body.
    public static BlockHeadingMetrics Headings(IVisualBlock block) {
        var theme = block.Options.ResolvedTheme;
        var titleGap = block.Title.Length > 0 ? 7 + (block.Subtitle.Length == 0 ? 8 : 0) : 0;
        var subtitleGap = block.Subtitle.Length > 0 ? 13 : 0;
        var titleStyle = new TextStyle { Font = new FontSpec { Family = theme.FontFamily, Weight = 700 }, FontSize = theme.TitleFontSize, LineHeight = 1 };
        var subtitleStyle = new TextStyle { Font = new FontSpec { Family = theme.FontFamily }, FontSize = theme.SubtitleFontSize, LineHeight = 1 };
        var titleHeight = block.Title.Length > 0 ? TextLayoutEngine.Measure(SingleLineText(block.Title), titleStyle).Height : 0;
        var subtitleHeight = block.Subtitle.Length > 0 ? TextLayoutEngine.Measure(SingleLineText(block.Subtitle), subtitleStyle).Height : 0;
        var fonts = titleHeight + subtitleHeight;
        var available = ContentRect(block.Options).Height * .4;
        var gaps = titleGap + subtitleGap;
        var gapScale = gaps > 0 ? Math.Min(1, available / (gaps * 2)) : 1;
        var fontScale = fonts > 0 ? Math.Min(1, (available - gaps * gapScale) / fonts) : 1;
        var titleSize = theme.TitleFontSize * fontScale; var subtitleSize = theme.SubtitleFontSize * fontScale;
        titleStyle.FontSize = titleSize; subtitleStyle.FontSize = subtitleSize;
        var titleAscent = TextAscent(titleStyle); var subtitleAscent = TextAscent(subtitleStyle);
        var titleAdvance = block.Title.Length > 0 ? TextLayoutEngine.Measure(SingleLineText(block.Title), titleStyle).Height : 0;
        var subtitleAdvance = block.Subtitle.Length > 0 ? TextLayoutEngine.Measure(SingleLineText(block.Subtitle), subtitleStyle).Height : 0;
        return new BlockHeadingMetrics(titleSize, subtitleSize, titleAscent, subtitleAscent, titleAdvance + titleGap * gapScale, subtitleAdvance + subtitleGap * gapScale);
    }

    public readonly struct BlockHeadingMetrics {
        public BlockHeadingMetrics(double titleFontSize, double subtitleFontSize, double titleAscent, double subtitleAscent, double titleAdvance, double subtitleAdvance) {
            TitleFontSize = titleFontSize; SubtitleFontSize = subtitleFontSize; TitleAscent = titleAscent; SubtitleAscent = subtitleAscent; TitleAdvance = titleAdvance; SubtitleAdvance = subtitleAdvance;
        }
        public double TitleFontSize { get; }
        public double SubtitleFontSize { get; }
        public double TitleAscent { get; }
        public double SubtitleAscent { get; }
        public double TitleAdvance { get; }
        public double SubtitleAdvance { get; }
    }

    public static void Validate(IVisualBlock block) {
        if (block == null) throw new ArgumentNullException(nameof(block));
        if (block is RailroadDiagramBlock railroad) { ValidateRailroadDiagram(railroad); return; }
        if (block is JourneyDiagramBlock journey) { ValidateJourneyDiagram(journey); return; }
        if (block is TimelineDiagramBlock timeline) { ValidateTimelineDiagram(timeline); return; }
        if (block is PacketLayoutBlock packet) {
            ValidatePacketLayout(packet);
            return;
        }

        if (block is BlockLayoutBlock blockLayout) {
            ValidateBlockLayout(blockLayout);
            return;
        }

        if (block is GitGraphBlock gitGraph) {
            ValidateGitGraph(gitGraph);
            return;
        }

        if (block is VennDiagramBlock venn) {
            ValidateVennDiagram(venn);
            return;
        }

        if (block is FishboneDiagramBlock fishbone) {
            ValidateFishboneDiagram(fishbone);
            return;
        }

        if (block is WardleyMapBlock wardleyMap) {
            ValidateWardleyMap(wardleyMap);
            return;
        }

    }

    public static ChartColor StatusColor(ResolvedVisualTheme theme, VisualStatus status) {
        switch (status) {
            case VisualStatus.Positive: return theme.Positive;
            case VisualStatus.Warning: return theme.Warning;
            case VisualStatus.Negative: return theme.Negative;
            case VisualStatus.Info: return theme.Info;
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

    public static ChartColor PaletteAt(ResolvedVisualTheme theme, int index) {
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

        return value.Substring(0, Typography.TextElementBoundary.Snap(value, low)) + suffix;
    }

    public static string FitMeasuredText(string value, double fontSize, double maxWidth, string fontFamily, int weight) {
        value = SingleLineText(value);
        var style = new TextStyle { Font = new FontSpec { Family = fontFamily, Weight = weight }, FontSize = fontSize, LineHeight = 1 };
        return ChartTextFitting.FitEnd(value, maxWidth, text => TextLayoutEngine.Measure(text, style).Width, "...");
    }

    public static double MeasureTextWidth(string value, double fontSize, string fontFamily, int weight) {
        var style = new TextStyle { Font = new FontSpec { Family = fontFamily, Weight = weight }, FontSize = fontSize, LineHeight = 1 };
        return TextLayoutEngine.Measure(SingleLineText(value), style).Width;
    }

    // These block captions are emitted as a single SVG text run. Match its default XML whitespace rules in both backends.
    public static string SingleLineText(string value) {
        var result = new StringBuilder(value.Length);
        var pendingSpace = false;
        foreach (var character in value) {
            if (character == ' ' || character == '\t' || character == '\r' || character == '\n') {
                pendingSpace = result.Length > 0;
                continue;
            }
            if (pendingSpace) result.Append(' ');
            result.Append(character);
            pendingSpace = false;
        }
        return result.ToString();
    }

    public static double FitFontSize(string value, double maxWidth, double preferredFontSize, double minimumFontSize) {
        var fontSize = Math.Max(minimumFontSize, preferredFontSize);
        while (fontSize > minimumFontSize && EstimateTextWidth(value, fontSize) > maxWidth) fontSize -= 0.5;
        return Math.Max(minimumFontSize, fontSize);
    }

    public static ChartRect ContentRect(VisualBlockOptions options) {
        return new ChartRect(
            options.Padding.Left,
            options.Padding.Top,
            Math.Max(1, options.Size.Width - options.Padding.Left - options.Padding.Right),
            Math.Max(1, options.Size.Height - options.Padding.Top - options.Padding.Bottom));
    }

    public static ChartColor SurfaceBackground(VisualBlockOptions options) =>
        options.TransparentBackground ? ChartColor.Transparent : options.ResolvedTheme.Background.A == 0 ? options.ResolvedTheme.CardBackground : options.ResolvedTheme.Background;

    public static ChartColor CardBackground(VisualBlockOptions options) => options.ResolvedTheme.CardBackground;

    public static double SegmentRatio(double value, double maximum) => maximum <= 0 ? 0 : Math.Max(0, Math.Min(1, value / maximum));

    public static double EffectiveStackGap(int count, double width, double preferredGap) {
        if (count <= 1 || width <= 0) return 0;
        return Math.Min(Math.Max(0, preferredGap), Math.Max(0, width * 0.35 / (count - 1)));
    }

    public static double EffectiveHeatmapGap(double plotWidth, double plotHeight, int columns, int rows, double desiredGap) {
        var gap = Math.Max(0, desiredGap);
        if (columns > 1) gap = Math.Min(gap, Math.Max(0, (plotWidth - columns) / (columns - 1)));
        if (rows > 1) gap = Math.Min(gap, Math.Max(0, (plotHeight - rows) / (rows - 1)));
        return gap;
    }

    public static bool IsFinite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);

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

    public static (double ItemWidth, double Gap) FitRepeatedItems(int count, double width, double preferredGap, double minimumItemWidth) {
        return FitRepeatedItems(count, width, Math.Max(0, count - 1), preferredGap, minimumItemWidth);
    }

    public static (double ItemWidth, double Gap) FitRepeatedItems(int count, double width, int gapCount, double preferredGap, double minimumItemWidth) {
        if (count <= 0 || width <= 0) return (0, 0);
        if (count == 1) return (Math.Max(0, width), 0);
        var gaps = Math.Max(0, gapCount);
        var minWidth = Math.Max(0, minimumItemWidth);
        var gap = gaps == 0 ? 0 : Math.Min(Math.Max(0, preferredGap), Math.Max(0, (width - minWidth * count) / gaps));
        var itemWidth = Math.Max(0, (width - gap * gaps) / count);
        if (itemWidth < minWidth && width < minWidth * count) {
            gap = 0;
            itemWidth = Math.Max(0, width / count);
        }

        return (itemWidth, gap);
    }
}
