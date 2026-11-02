using System;
using ChartForgeX.Core;
using ChartForgeX.Themes;

namespace ChartForgeX.Rendering;

internal static partial class VisualGaugeCompiler {
    /// <summary>Automatic status bands are context, not the measurement: they paint muted at this share of their opacity.</summary>
    internal const double AutomaticBandOpacity = .6;

    // A healthy band is quiet; only warning and danger bands carry a loud status colour.
    private static ChartColor BandSource(VisualThemeColors colors, ChartGaugeBand band) => band.Color
        ?? (band.State == ChartSeriesState.Success ? colors.Quiet : VisualRadialPrimitives.StateColor(colors, band.State, colors.Accent));

    private static double BandOpacity(ChartGaugeBand band) => band.Color.HasValue ? band.Opacity : band.Opacity * AutomaticBandOpacity;

    private static ChartColor BandColor(VisualThemeColors colors, ChartGaugeBand band) =>
        ChartColorMath.WithOpacity(BandSource(colors, band), BandOpacity(band));

    private static SvgPaint BandPaint(VisualThemeColors colors, ChartGaugeBand band) {
        var paint = SvgPaint.Of(BandSource(colors, band), band.Color.HasValue ? SvgColorRole.Series : SvgColorRole.Status);
        var opacity = BandOpacity(band);
        return opacity == 1 ? paint : paint.WithOpacity(BandColor(colors, band), opacity);
    }

    private static SvgPaint NeedlePaint(GaugePaintContext chart, GaugeData data) => chart.Options.NeedleColor.HasValue
        ? SvgPaint.Of(chart.Options.NeedleColor.Value, SvgColorRole.Series) : chart.ValuePaint(data);

    /// <summary>Insets only painted spans; source intervals, states and readings stay exact.</summary>
    private static void BandRatios(GaugePaintContext chart, GaugeData data, ChartGaugeBand band, double length, bool radial, out double lower, out double upper) {
        lower = Ratio(data, band.Minimum); upper = Ratio(data, band.Maximum);
        if (chart.Options.BandGap == 0 || upper <= lower) return;
        var available = radial ? length * Sweep(chart.Options) : length;
        var inset = Math.Min((upper - lower) / 2, chart.Options.BandGap / Math.Max(double.Epsilon, available) / 2);
        lower += inset; upper -= inset;
    }
}
