using System;
using ChartForgeX.Primitives;
using ChartForgeX.Typography;

namespace ChartForgeX.Rendering;

/// <summary>
/// Shared label policy for text that names a row: schedule lanes, lane summaries, group headers and heatmap rows.
/// </summary>
/// <remarks>
/// Row text keeps one readable size and truncates to its gutter. It may shrink toward the row height but never below
/// <see cref="MinimumFontSize"/>, and it is hidden rather than drawn over its neighbours when even that size does not fit.
/// </remarks>
internal static class ChartRowText {
    /// <summary>The smallest row text size. Long names are truncated, never shrunk below this.</summary>
    internal const double MinimumFontSize = 10;

    // Ascender-to-descender height of ordinary UI faces relative to the font size.
    private const double GlyphHeightRatio = 1.2;

    /// <summary>Returns the style for row text of the given row height and, for oversized authored text, gutter width.</summary>
    internal static TextStyle Style(VisualSceneBuilder builder, TextStyle style, string text, double height, double width = 0) {
        if (height <= 0 || style.EffectiveFontSize <= MinimumFontSize) return style;
        var fitted = style.Clone();
        var metrics = builder.MeasureText(text, fitted);
        var ratio = metrics.Height > height + .001 ? height / Math.Max(.001, metrics.Height) : 1;
        // Ordinary-sized text keeps one size across rows and truncates; only oversized authored text also shrinks toward
        // the gutter width, never below the readable floor.
        if (width > 0 && metrics.Width > width + .001 && fitted.EffectiveFontSize > VisualTextFitting.MaximumOrdinaryFontSize)
            ratio = Math.Min(ratio, width / Math.Max(.001, metrics.Width));
        if (ratio < 1) fitted.FontSize = Math.Max(MinimumFontSize, fitted.EffectiveFontSize * ratio);
        return fitted;
    }

    /// <summary>
    /// Returns text bounds centred on a row band. The text may use the row's pitch above and below the band, so it is not
    /// omitted merely because the band is thinner than a line box; text whose glyphs do not fit the pitch is omitted.
    /// The pitch is the distance between neighbouring rows; zero lets the text use its full line box.
    /// </summary>
    internal static ChartRect Bounds(VisualSceneBuilder builder, TextStyle style, string text, double left, double width, double top, double height, double pitch = 0) {
        var textHeight = builder.MeasureText(text.Length == 0 ? "Mg" : text, style).Height;
        // The measured line box includes generous leading; neighbours collide only when the glyphs themselves do not fit.
        var lines = 1; foreach (var character in text) if (character == '\n') lines++;
        var fits = pitch <= 0 || pitch >= style.EffectiveFontSize * GlyphHeightRatio * lines;
        var rowHeight = fits ? Math.Max(height, textHeight) : Math.Max(height, pitch);
        return new ChartRect(left, top + (height - rowHeight) / 2, width, rowHeight);
    }
}
