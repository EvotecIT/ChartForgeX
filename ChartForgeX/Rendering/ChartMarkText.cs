using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;

namespace ChartForgeX.Rendering;

/// <summary>
/// Chooses mark text once for SVG and PNG. Prepared numeric and fully filled state marks use opaque contrast ink;
/// quiet state marks use the canonical foreground when readable, with contrast ink as a fallback. Portable model-only
/// helpers also retain the surface/text policy for their existing consumers.
/// </summary>
/// <remarks>
/// In the portable surface/text policy, a colour that does not reach <see cref="MinimumContrast"/> against the fill gives way to the other colour when that
/// one contrasts more, so pale caller colours and fills close to the surface stay readable. That check runs in each theme,
/// so one SVG serves both themes only when every strong fill reaches the minimum against the surface behind the marks in
/// both: true for the Graphite tokens on the card (<see cref="ChartMarkBackdrop.Card"/>), not for a light page surface
/// under the medium severity fill.
/// </remarks>
internal static class ChartMarkText {
    /// <summary>Chooses opaque text against an actual prepared fill, compositing translucent fills on their resolved backdrop.</summary>
    internal static ChartColorBlend OnPreparedMark(ChartColor fill, SvgColorRole fillRole, ChartColor backdrop) {
        if (fill.A == 255) return ChartColorBlend.Contrast(fill, fillRole);
        // An alpha fill is already a resolved colour: preserve its actual straight-alpha composition.
        // Do not infer a token from a matching opaque RGB value.
        var composed = new ChartColorBlend(backdrop, null, ChartColor.FromRgb(fill.R, fill.G, fill.B), null, fill.A / 255d);
        return ChartColorBlend.Contrast(composed);
    }

    /// <summary>Resolves prepared numeric matrix ink from canonical frame surfaces and observed scale strength.</summary>
    internal static ChartColorBlend OnHeatmapCell(Chart chart, VisualThemeColors colors, VisualFrame frame,
        ChartColor fill, ChartColor? high, double value, double min, double max,
        SvgColorRole fillRole = SvgColorRole.Ramp, ChartColorBlend? source = null) => source.HasValue
            ? ChartColorBlend.Contrast(source.Value) : ChartColorBlend.Contrast(fill, fillRole);

    /// <summary>Resolves prepared state-mark ink against the same composited surface as its fill and pattern.</summary>
    internal static ChartColorBlend OnStateMark(Chart chart, VisualThemeColors colors, VisualFrame frame, ChartStateMark mark) =>
        mark.FillOpacity >= .999
            ? ChartColorBlend.Contrast(mark.Surface, SvgColorRole.Status)
            : ChartColorMath.ContrastRatio(mark.Surface, Over(colors.Foreground, mark.Surface)) >= MinimumContrast
                ? ChartColorBlend.Solid(colors.Foreground, SvgColorRole.Text)
                : ChartColorBlend.Contrast(mark.Surface, SvgColorRole.Status);

    /// <summary>
    /// The contrast (WCAG ratio) below which the other colour is used when it contrasts more: 3:1, the WCAG minimum for
    /// large text and graphics. Mid-tone token fills cannot reach the 4.5:1 of small text with either theme colour.
    /// </summary>
    public const double MinimumContrast = 3.0;

    /// <summary>Returns the text colour on a matrix or hexbin heatmap cell drawn by <see cref="ChartHeatmapSurface.CellBlend(Chart, ChartColor?, double, double, double)"/>.</summary>
    public static ChartColorBlend OnHeatmapCell(Chart chart, ChartColor? highColor, double value, double min, double max) {
        var fill = ChartHeatmapSurface.CellBlend(chart, highColor, value, min, max);
        return chart.Options.Theme.UseGraphiteLayout
            ? ChartColorBlend.Contrast(fill.Color, fill.FromRole ?? SvgColorRole.Ramp)
            : For(chart, fill.Color, ChartHeatmapSurface.IsStrongCell(chart, highColor, value, min, max));
    }

    /// <summary>Returns the text colour on a state mark: fully filled marks are strong, quiet and outlined tints weak.</summary>
    public static ChartColorBlend OnStateMark(Chart chart, ChartStateMark mark) =>
        chart.Options.Theme.UseGraphiteLayout
            ? mark.FillOpacity >= 0.999 ? ChartColorBlend.Contrast(mark.Color, SvgColorRole.Status) : ChartColorBlend.Solid(chart.Options.Theme.Text, SvgColorRole.Text)
            : For(chart, mark.Surface, mark.FillOpacity >= 0.999);

    private static ChartColorBlend For(Chart chart, ChartColor fill, bool strong) {
        var surface = ChartStateMark.Backdrop(chart);
        var text = chart.Options.Theme.Text;
        return For(surface, text, fill, strong);
    }

    private static ChartColorBlend For(ChartColor surface, ChartColor text, ChartColor fill, bool strong) {
        // Translucent text is judged as it appears on the fill.
        var shownText = Over(text, fill);
        var preferred = strong ? surface : shownText;
        var other = strong ? shownText : surface;
        var preferredContrast = ChartColorMath.ContrastRatio(fill, preferred);
        var surfaceText = strong != (preferredContrast < MinimumContrast && ChartColorMath.ContrastRatio(fill, other) > preferredContrast);
        return ChartColorBlend.Solid(surfaceText ? surface : text, surfaceText ? SvgColorRole.Surface : SvgColorRole.Text);
    }

    private static ChartColor Over(ChartColor top, ChartColor bottom) {
        var alpha = top.A / 255.0;
        return ChartColor.FromRgb(
            (byte)System.Math.Round(top.R * alpha + bottom.R * (1 - alpha)),
            (byte)System.Math.Round(top.G * alpha + bottom.G * (1 - alpha)),
            (byte)System.Math.Round(top.B * alpha + bottom.B * (1 - alpha)));
    }
}
