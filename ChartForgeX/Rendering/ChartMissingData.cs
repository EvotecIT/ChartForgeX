using System;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;

namespace ChartForgeX.Rendering;

/// <summary>
/// Design rule 9: one library-wide look for missing data. A missing cell is the plot surface with a 45° hatch in
/// secondary ink, so it never reads as zero (a flat neutral tint) or as the lightest ramp step. Heatmaps, calendars,
/// treemaps and any family that draws a gap use this one painter and its legend swatch.
/// </summary>
internal static class ChartMissingData {
    /// <summary>The hatch ink opacity over the surface.</summary>
    internal const double HatchOpacity = .55;

    /// <summary>The hatch spacing in drawing pixels.</summary>
    internal const double HatchSpacing = 5;

    /// <summary>Paints one missing-data cell (or legend swatch) with the shared hatch.</summary>
    internal static void Paint(VisualSceneBuilder builder, ChartRect bounds, double radius, VisualThemeColors colors, string role) {
        builder.Rect(bounds, colors.Surface, radius: radius, role: role, paint: VisualChartPaint.Fill(SvgPaint.Of(colors.Surface, SvgColorRole.Surface)));
        Hatch(builder, ChartPathBuilder.RoundedRectangle(bounds, Math.Min(radius, Math.Min(bounds.Width, bounds.Height) / 2)), colors.MutedForeground, role);
    }

    /// <summary>Paints a missing-data shape of any outline with the shared hatch.</summary>
    internal static void Paint(VisualSceneBuilder builder, ChartPath outline, VisualThemeColors colors, string role) {
        builder.Path(outline, colors.Surface, role: role, close: true, paint: VisualChartPaint.Fill(SvgPaint.Of(colors.Surface, SvgColorRole.Surface)));
        Hatch(builder, outline, colors.MutedForeground, role);
    }

    /// <summary>Lays the shared missing-data hatch over an existing swatch, in the given secondary ink.</summary>
    internal static void Hatch(VisualSceneBuilder builder, ChartPath outline, ChartColor secondaryInk, string role) {
        var ink = secondaryInk.WithOpacity(HatchOpacity);
        builder.Pattern(outline, ChartFillPattern.DiagonalForward, ink, spacing: HatchSpacing, strokeWidth: 1, role: role + "-hatch",
            paint: SvgPaint.Of(secondaryInk, SvgColorRole.Text).WithOpacity(ink, HatchOpacity));
    }
}
