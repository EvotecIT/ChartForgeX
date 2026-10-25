using System;
using ChartForgeX.Core;
using ChartForgeX.Themes;

namespace ChartForgeX.Rendering;

internal static partial class VisualGaugeCompiler {
    private static ChartColor BandColor(VisualThemeColors colors, ChartGaugeBand band) =>
        ChartColorMath.WithOpacity(band.Color ?? VisualRadialPrimitives.StateColor(colors, band.State, colors.Accent), band.Opacity);

    private static SvgPaint BandPaint(VisualThemeColors colors, ChartGaugeBand band) {
        var source = band.Color ?? VisualRadialPrimitives.StateColor(colors, band.State, colors.Accent);
        var paint = SvgPaint.Of(source, band.Color.HasValue ? SvgColorRole.Series : SvgColorRole.Status);
        return band.Opacity == 1 ? paint : paint.WithOpacity(BandColor(colors, band), band.Opacity);
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
