using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;

namespace ChartForgeX.Rendering;

/// <summary>
/// Chooses the colour of text drawn on a filled mark (heatmap and hexbin values, categorical cell text, Gantt lane item
/// labels), once for SVG and PNG, from two theme colours that SVG colour variables write by role: the surface behind the
/// marks (<see cref="ChartStateMark.Backdrop"/>, <see cref="SvgColorRole.Surface"/>) on a strong fill, and the text colour
/// (<see cref="SvgColorRole.Text"/>) on a weak one. Whether a fill is strong comes from where the cell sits on its scale,
/// or how fully a state mark is filled, not from its colour in one theme. Token sets whose strong marks stand out from the
/// surface in light and dark themes therefore pick the same role in both, and one SVG with colour variables serves both.
/// </summary>
/// <remarks>
/// A colour that does not reach <see cref="MinimumContrast"/> against the fill gives way to the other colour when that
/// one contrasts more, so pale caller colours and fills close to the surface stay readable. That check runs in each theme,
/// so one SVG serves both themes only when every strong fill reaches the minimum against the surface behind the marks in
/// both: true for the Graphite tokens on the card (<see cref="ChartMarkBackdrop.Card"/>), not for a light page surface
/// under the medium severity fill.
/// </remarks>
internal static class ChartMarkText {
    /// <summary>
    /// The contrast (WCAG ratio) below which the other colour is used when it contrasts more: 3:1, the WCAG minimum for
    /// large text and graphics. Mid-tone token fills cannot reach the 4.5:1 of small text with either theme colour.
    /// </summary>
    public const double MinimumContrast = 3.0;

    /// <summary>Returns the text colour on a matrix or hexbin heatmap cell drawn by <see cref="ChartHeatmapSurface.CellBlend"/>.</summary>
    public static ChartColorBlend OnHeatmapCell(Chart chart, ChartColor? highColor, double value, double min, double max) =>
        For(chart, ChartHeatmapSurface.CellBlend(chart, highColor, value, min, max).Color, ChartHeatmapSurface.IsStrongCell(chart, highColor, value, min, max));

    /// <summary>Returns the text colour on a state mark: fully filled marks are strong, quiet and outlined tints weak.</summary>
    public static ChartColorBlend OnStateMark(Chart chart, ChartStateMark mark) => For(chart, mark.Surface, mark.FillOpacity >= 0.999);

    private static ChartColorBlend For(Chart chart, ChartColor fill, bool strong) {
        var surface = ChartStateMark.Backdrop(chart);
        var text = chart.Options.Theme.Text;
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
