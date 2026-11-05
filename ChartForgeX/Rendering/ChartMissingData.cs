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

    /// <summary>The look of a missing heatmap cell: an authored no-data color is plain, otherwise the surface carries
    /// the hatch, both with the faint cell outline. The cells and their key paint from this, so the key matches them.</summary>
    internal static ChartMissingLook Look(ChartColorScale? scale, VisualThemeColors colors) => scale?.NoDataColor != null
        ? new(ChartColorScaleSurface.NoData(scale, colors), false, colors)
        : new(ChartColorBlend.Solid(colors.Surface, SvgColorRole.Surface), true, colors);

    /// <summary>A no-data key for families whose missing marks are not cells: the no-data fill outlined, and when
    /// unauthored hatched, in the key's caption ink so the pale swatch stays visible on both themes.</summary>
    internal static ChartMissingLook KeyLook(ChartColorScale? scale, VisualThemeColors colors) =>
        ChartMissingLook.InCaptionInk(ChartColorScaleSurface.NoData(scale, colors), scale?.NoDataColor == null);

    /// <summary>Paints a missing cell or its key swatch: fill, outline and, when unauthored, the hatch.</summary>
    internal static void Paint(VisualSceneBuilder builder, ChartRect bounds, double radius, ChartMissingLook look, string role, string hatchRole) {
        builder.Rect(bounds, look.Fill.Color, look.Outline, .75, radius, role,
            paint: new VisualScenePaintBinding(fill: look.Fill.Paint, stroke: look.OutlinePaint));
        if (look.Hatched && look.HatchInk is ChartColor ink)
            Hatch(builder, ChartPathBuilder.RoundedRectangle(bounds, Math.Min(radius, Math.Min(bounds.Width, bounds.Height) / 2)), ink, hatchRole);
    }
    /// <summary>Lays the shared missing-data hatch over an existing swatch, in the given secondary ink.</summary>
    internal static void Hatch(VisualSceneBuilder builder, ChartPath outline, ChartColor secondaryInk, string role) {
        var ink = secondaryInk.WithOpacity(HatchOpacity);
        builder.Pattern(outline, ChartFillPattern.DiagonalForward, ink, spacing: HatchSpacing, strokeWidth: 1, role: role + "-hatch",
            paint: SvgPaint.Of(secondaryInk, SvgColorRole.Text).WithOpacity(ink, HatchOpacity));
    }
}

/// <summary>One resolved missing-observation look shared by marks and keys.</summary>
internal readonly struct ChartMissingLook {
    internal ChartMissingLook(ChartColorBlend fill, bool hatched, VisualThemeColors colors) {
        Fill = fill; Hatched = hatched; HatchInk = colors.MutedForeground;
        Outline = colors.Border; OutlinePaint = SvgPaint.Of(colors.Border, SvgColorRole.Grid);
        // Thin dark outlines need more separation from the card to survive antialiasing.
        if (ChartColorMath.RelativeLuminance(colors.ElevatedSurface) < .5) {
            var outline = ChartColorMath.Blend(colors.Border, colors.MutedForeground, .18);
            Outline = outline;
            OutlinePaint = SvgPaint.Mix(outline, colors.Border, SvgColorRole.Grid, colors.MutedForeground, SvgColorRole.Text, .18);
        }
    }

    private ChartMissingLook(ChartColorBlend fill, bool hatched, ChartColor? ink) {
        Fill = fill; Hatched = hatched; HatchInk = ink; Outline = ink;
        OutlinePaint = ink is ChartColor color ? SvgPaint.Of(color, SvgColorRole.Text) : null;
    }

    /// <summary>A look whose outline and hatch take the ink of the key that draws it.</summary>
    internal static ChartMissingLook InCaptionInk(ChartColorBlend fill, bool hatched) => new(fill, hatched, (ChartColor?)null);

    internal ChartMissingLook WithInk(ChartColor ink) => Outline.HasValue ? this : new(Fill, Hatched, (ChartColor?)ink);

    internal ChartColorBlend Fill { get; }
    internal bool Hatched { get; }
    internal ChartColor? HatchInk { get; }
    internal ChartColor? Outline { get; }
    internal SvgPaint? OutlinePaint { get; }
}