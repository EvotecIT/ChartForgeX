using System;
using System.Globalization;
using ChartForgeX.Topology;
using ChartForgeX.Themes;
using ChartForgeX.Typography;

namespace ChartForgeX.Rendering;

/// <summary>A detached effective request and the same frozen actual face/cluster capture, independent of numeric geometry.</summary>
internal sealed class PreparedDiagramFont {
    private readonly FontSpec _font;
    internal PreparedDiagramFont(FontSpec font, string themeFamily, VisualSceneTextCapture? text = null) {
        _font = (font ?? throw new ArgumentNullException(nameof(font))).WithoutCapturedText();
        ThemeFamily = string.IsNullOrWhiteSpace(themeFamily) ? throw new ArgumentException("A captured context font requires its theme-family basis.", nameof(themeFamily)) : themeFamily;
        Text = text?.Freeze();
    }
    internal string ThemeFamily { get; }
    internal FontSpec Font => _font.Clone();
    internal VisualSceneTextCapture? Text { get; }
    internal TextStyleOverride Policy => new() { FontFamily = _font.Family, FontWeight = _font.Weight.ToString(CultureInfo.InvariantCulture),
        Italic = _font.Italic, ColorPaletteIndex = _font.ColorPaletteIndex, Variations = _font.Variations };
    internal FontSpec ForPreparation() => Text is {} captured ? _font.WithCapturedText(captured.ContinueTopology()) : _font.Clone();
    internal static PreparedDiagramFont? Capture(VisualRenderContext context) {
        var font = context.Font; var text = font.NativeTextCapture;
        return text == null && !font.CapturedExplicitFile && font.SameRequest(context.Theme.Typography.ToFontSpec()) ? null
            : new PreparedDiagramFont(font, context.Theme.Typography.Family, text);
    }
    internal static PreparedDiagramFont? ForRequest(TopologyChart chart) => chart.PreparedFont is {} fact
        && string.Equals(chart.Theme.Typography.Family, fact.ThemeFamily, StringComparison.Ordinal) ? fact : null;

    private static bool HasExplicitFrameFont(VisualFrame frame) => frame.TitleStyle != null || frame.SubtitleStyle != null || frame.LegendStyle != null;

    internal static VisualRenderContext ScopeContext(VisualRenderContext context, bool materialTextPolicy = false) {
        var font = context.Font;
        var capture = font.NativeTextCapture ?? context.NativeTextCapture;
        if (capture == null && font.FilePath == null && !font.CapturedExplicitFile
            && font.SameRequest(context.Theme.Typography.ToFontSpec()) && font.SameRequest(VisualTheme.Graphite().Typography.ToFontSpec()) && !HasExplicitFrameFont(context.Frame) && !materialTextPolicy) return context;
        capture = capture?.IsSnapshot == true ? capture.ContinueTopology() : capture ?? new VisualSceneTextCapture();
        var frame = context.Frame;
        TextStyle? Style(TextStyle? style) { if (style == null) return null; var copy = style.Clone(); copy.Font = style.Font.WithCapturedText(capture); return copy; }
        var scopedFrame = new VisualFrame(frame.Title, frame.Subtitle, frame.HasExplicitLegend ? frame.ShowLegend : null,
            frame.HasExplicitLegendPosition ? frame.LegendPosition : null, frame.ShowSurface, frame.TransparentBackground,
            Style(frame.TitleStyle), Style(frame.SubtitleStyle), Style(frame.LegendStyle), frame.LegendMaximumRows,
            frame.LegendMaximumHeightFraction, frame.ShowCard, frame.LegendTitle);
        return new VisualRenderContext(context.Layout, context.Theme, context.ThemeMode, scopedFrame, font.WithCapturedText(capture)).WithNativeTextCapture(capture);
    }
}
