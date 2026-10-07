using System;
using ChartForgeX.Core;
using ChartForgeX.Themes;

namespace ChartForgeX.Rendering;

/// <summary>
/// Resolves how a mark of a state category is drawn (fill strength, outline, and line pattern) once, so state
/// timelines, Gantt lanes, categorical heatmaps, and their legend draw it the same way in SVG and PNG.
/// </summary>
internal readonly struct ChartStateMark {
    public const double OutlineWidth = 1.5;
    /// <summary>Dash length of the outline of an outlined mark, which is dashed as report views dash that state.</summary>
    public const double OutlineDash = 3;
    /// <summary>Gap between the dashes of the outline of an outlined mark.</summary>
    public const double OutlineGap = 2;
    public const double PatternLineWidth = 1.5;
    private const double QuietFill = 0.38;
    private const double QuietPatternFill = 0.55;
    private const double OutlinedFill = 0.12;
    private const double QuietOutlinedFill = 0.06;
    private const double QuietOutline = 0.6;

    private ChartStateMark(ChartColor color, double fillOpacity, bool outlined, double outlineOpacity, ChartFillPattern lines, ChartColor lineColor, ChartColor background, ChartStateCategory state) {
        Color = color;
        FillOpacity = fillOpacity;
        Outlined = outlined;
        OutlineOpacity = outlineOpacity;
        Lines = lines;
        LineColor = lineColor;
        Background = background;
        State = state;
    }

    public ChartStateCategory State { get; }

    /// <summary>Gets the category colour used for the fill and the outline.</summary>
    public ChartColor Color { get; }

    public double FillOpacity { get; }

    public bool Outlined { get; }

    public double OutlineOpacity { get; }

    /// <summary>Gets the lines drawn over the fill: none, forward diagonals, or crossed diagonals.</summary>
    public ChartFillPattern Lines { get; }

    /// <summary>Gets the pattern line colour: the surface behind the marks, so lines read as cuts on light and dark themes.</summary>
    public ChartColor LineColor { get; }

    private ChartColor Background { get; }

    /// <summary>Gets the fill as it appears on the backdrop (see <see cref="Backdrop(Chart)"/>), for choosing a readable text colour on the mark.</summary>
    public ChartColor Surface => Over(ChartColorMath.WithOpacity(Color, FillOpacity), Background);

    /// <summary>Gets the pattern token written to <c>data-cfx-pattern</c>, or null for a solid mark.</summary>
    public string? PatternToken => State.Pattern switch {
        ChartStatePattern.Hatched => "hatched",
        ChartStatePattern.CrossHatched => "cross-hatched",
        ChartStatePattern.Outlined => "outlined",
        _ => null
    };

    /// <summary>Gets the emphasis token written to <c>data-cfx-emphasis</c>, or null at normal emphasis.</summary>
    public string? EmphasisToken => State.Emphasis == ChartStateEmphasis.Quiet ? "quiet" : null;

    /// <summary>
    /// Returns the opaque colour behind the marks, from <see cref="ChartOptions.MarkBackdrop"/>. By default
    /// (<see cref="ChartMarkBackdrop.Layered"/>) that is the theme background, drawn or not (with a transparent chart it
    /// stands for the surface the host places the chart on), then the card and the plot background composited where they are drawn;
    /// the other choices take one theme surface whether or not it is drawn. Any surface is composited over white or black,
    /// whichever contrasts with the text colour, so a translucent or transparent surface still gives visible lines.
    /// </summary>
    public static ChartColor Backdrop(Chart chart) {
        var options = chart.Options;
        var theme = options.Theme;
        var backdrop = ChartColorMath.RelativeLuminance(theme.Text) > 0.5 ? ChartColor.FromRgb(0, 0, 0) : ChartColor.White;
        switch (options.MarkBackdrop) {
            case ChartMarkBackdrop.Background:
                return Over(theme.Background, backdrop);
            case ChartMarkBackdrop.Card:
                return Over(theme.CardBackground, backdrop);
            case ChartMarkBackdrop.Plot:
                return Over(theme.PlotBackground, backdrop);
        }

        backdrop = Over(theme.Background, backdrop);
        if (options.ShowCard && theme.UseCard) backdrop = Over(theme.CardBackground, backdrop);
        if (options.ShowPlotBackground) backdrop = Over(theme.PlotBackground, backdrop);
        return backdrop;
    }

    /// <summary>
    /// Resolves a prepared mark backdrop from the shared frame and tokens. A transparent frame assumes the host
    /// uses the selected theme background, unless the caller selects its card or plot token explicitly.
    /// </summary>
    internal static ChartColor Backdrop(ChartOptions options, VisualThemeColors colors, VisualFrame frame) {
        var opaqueBase = ChartColorMath.RelativeLuminance(colors.Foreground) > .5 ? ChartColor.Black : ChartColor.White;
        if (options.MarkBackdrop == ChartMarkBackdrop.Card) return Over(colors.ElevatedSurface, opaqueBase);
        if (options.MarkBackdrop == ChartMarkBackdrop.Plot) return Over(colors.Surface, opaqueBase);
        var background = Over(colors.Background, opaqueBase);
        return options.MarkBackdrop == ChartMarkBackdrop.Layered && frame.ShowSurface ? Over(colors.Surface, background) : background;
    }

    private static ChartColor Over(ChartColor top, ChartColor bottom) {
        var alpha = top.A / 255.0;
        return ChartColor.FromRgb(
            (byte)Math.Round(top.R * alpha + bottom.R * (1 - alpha)),
            (byte)Math.Round(top.G * alpha + bottom.G * (1 - alpha)),
            (byte)Math.Round(top.B * alpha + bottom.B * (1 - alpha)));
    }

    public static ChartStateMark For(Chart chart, ChartStateCategory state) {
        return For(state, Backdrop(chart));
    }

    /// <summary>Resolves the same state vocabulary against an already resolved prepared backdrop.</summary>
    internal static ChartStateMark For(ChartStateCategory state, ChartColor background) {
        var quiet = state.Emphasis == ChartStateEmphasis.Quiet;
        switch (state.Pattern) {
            case ChartStatePattern.Outlined:
                return new ChartStateMark(state.Color, quiet ? QuietOutlinedFill : OutlinedFill, true, quiet ? QuietOutline : 1, ChartFillPattern.None, background, background, state);
            case ChartStatePattern.Hatched:
            case ChartStatePattern.CrossHatched:
                // A quiet patterned mark keeps enough fill for the background-coloured lines to show.
                return new ChartStateMark(state.Color, quiet ? QuietPatternFill : 1, false, 0,
                    state.Pattern == ChartStatePattern.Hatched ? ChartFillPattern.DiagonalForward : ChartFillPattern.Crosshatch, background, background, state);
            default:
                return new ChartStateMark(state.Color, quiet ? QuietFill : 1, false, 0, ChartFillPattern.None, background, background, state);
        }
    }
}
