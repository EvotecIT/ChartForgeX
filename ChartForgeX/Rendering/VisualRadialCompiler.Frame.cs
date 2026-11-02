using System;
using System.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;

namespace ChartForgeX.Rendering;

internal static partial class VisualRadialCompiler {
    /// <summary>Native radius constraints consumed by static paint and offline responsive projection.</summary>
    internal readonly struct FrameSizing {
        internal FrameSizing(double horizontal, double vertical, double extent) {
            Horizontal = horizontal; Vertical = vertical; Extent = extent;
        }
        internal double Horizontal { get; }
        internal double Vertical { get; }
        internal double Extent { get; }
        internal double Radius(ChartRect plot, double spacing) =>
            Math.Max(0, (Math.Min(Math.Min(plot.Width * Horizontal, WideWidth(plot)), plot.Height * Vertical) / 2 - spacing) / Extent);
    }

    /// <summary>A plot wider than this aspect ratio limits the radial diameter to <see cref="WideDiameterShare"/> of its width.</summary>
    internal const double WideAspect = 1.4;

    /// <summary>The share of a wide plot's width a radial chart may span, so it does not dominate a landscape canvas.</summary>
    internal const double WideDiameterShare = .6;

    private static double WideWidth(ChartRect plot) => plot.Width > plot.Height * WideAspect ? plot.Width * WideDiameterShare : double.PositiveInfinity;

    private static FrameSizing Sizing(ChartDataLabelPlacement placement, double extent) => new(
        placement is ChartDataLabelPlacement.Outside or ChartDataLabelPlacement.Left or ChartDataLabelPlacement.Right ? .57 : 1,
        placement is ChartDataLabelPlacement.Above or ChartDataLabelPlacement.Below ? .76 : 1, extent);

    /// <summary>Returns complete native overflow bounds; responsive totals must never ellipsize an exponent.</summary>
    internal static string[]? ResponsiveOverflowCenter(Chart chart, VisualRenderContext context, int seriesIndex) =>
        chart.Options.DonutCenterValue == null && !Total(chart, GetSlices(chart, context.Theme.Resolve(context.ThemeMode), seriesIndex), seriesIndex).FiniteTotal.HasValue
            ? new[] { FormatOverflowCenter(false), FormatOverflowCenter(true) } : null;

    /// <summary>Captures the same envelope policy used for each native radial series.</summary>
    internal static FrameSizing[] ResponsiveFrames(Chart chart, VisualRenderContext context, ChartRect plot) {
        var bands = ChartRadialBandResolver.Resolve(chart);
        var outer = Enumerable.Range(0, chart.Series.Count).OrderByDescending(i => bands[i]?.OuterRatio ?? 1).First();
        var outerPlacement = chart.Series[outer].DataLabelPlacement ?? chart.Options.DataLabelPlacement;
        var frame = ResolveBandFrame(chart, context, plot, bands, outer);
        return Enumerable.Range(0, chart.Series.Count).Select(index => {
            if (frame != null) return frame.Sizing;
            var series = chart.Series[index];
            var maximumOffset = GetSlices(chart, context.Theme.Resolve(context.ThemeMode), index)
                .Where(slice => slice.Value > 0 && slice.RadiusRatio.GetValueOrDefault(1) > 0 && slice.PointIndex >= 0 && slice.PointIndex < series.PointSliceOffsets.Count)
                .Select(slice => series.PointSliceOffsets[slice.PointIndex]).DefaultIfEmpty(0).Max();
            return Sizing(chart.Series.Count > 1 ? outerPlacement : series.DataLabelPlacement ?? chart.Options.DataLabelPlacement, 1 + maximumOffset);
        }).ToArray();
    }
}
