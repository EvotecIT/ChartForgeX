using System;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;

namespace ChartForgeX.Rendering;

internal static partial class VisualCartesianCompiler {
    private static ChartRect ReserveVerticalBarLabelGutters(Chart chart, VisualRenderContext context, VisualSceneBuilder builder,
        ChartRect plot, ChartRange range, ChartRange? secondaryRange, ChartStackLayout stacks, ChartBarCoordinateMap coordinates,
        VisualThemeColors colors, CartesianLabels labels) {
        var top = 0d; var bottom = 0d; var spacing = Math.Max(2, context.Theme.Spacing);
        for (var seriesIndex = 0; seriesIndex < chart.Series.Count; seriesIndex++) {
            var series = chart.Series[seriesIndex];
            if (series.Kind != ChartSeriesKind.Bar || !(series.ShowDataLabels ?? (chart.Options.ShowDataLabels ?? false))
                || ChartStackLayout.Participates(chart, series)) continue;
            var placement = series.DataLabelPlacement ?? chart.Options.DataLabelPlacement;
            if (placement is not (ChartDataLabelPlacement.Auto or ChartDataLabelPlacement.Above or ChartDataLabelPlacement.Below)) continue;
            var axis = ChartCartesianOrientation.ValueAxis(chart, series.ValueAxis);
            var selectedRange = series.ValueAxis == ChartAxisSide.Secondary ? secondaryRange! : range;
            var map = new ChartMapper(plot, selectedRange, chart.Options.XAxis, axis);
            var style = SeriesLabelStyle(chart, context, series, colors);
            for (var pointIndex = 0; pointIndex < series.RenderPoints.Count; pointIndex++) {
                var point = series.RenderPoints[pointIndex];
                if (series.HistogramBinLayout != null && !series.HistogramBins[pointIndex].Value.HasValue
                    && (pointIndex >= series.PointLabels.Count || series.PointLabels[pointIndex] == null)) continue;
                var anchor = map.Y(point.Y);
                if (!ChartMath.IsFinite(anchor) || anchor < plot.Top || anchor > plot.Bottom) continue;
                var label = labels.PointLabel(chart, series, pointIndex, style);
                if (label.Text.Length == 0) continue;
                // Oversized typography already has a bounded fitting pass against the actual
                // candidates. Do not allocate an unfitted font's height before that pass.
                if (label.Style.EffectiveFontSize > VisualTextFitting.MaximumOrdinaryFontSize) continue;
                var metrics = builder.MeasureText(label.DisplayedText, DisplayedStyle(label.Style));
                // Auto keeps every caption of a series on the same side of its bars, so the tallest
                // bar reserves its lane instead of moving its caption inside (and into a shorter format).
                var height = metrics.Height + BarLabelGap(context);
                var above = placement == ChartDataLabelPlacement.Above
                    || placement == ChartDataLabelPlacement.Auto && BarValueDirection(axis, point.Y) >= 0;
                // Reserving a partial deficit would change that same deficit when the
                // smaller plot remaps the point. A complete lane remains sufficient
                // after either edge is inset, including simultaneous signed captions.
                if (above && anchor - plot.Top < height) top = Math.Max(top, height);
                else if (!above && plot.Bottom - anchor < height) bottom = Math.Max(bottom, height);
            }
        }
        return InsetVerticalLabelGutters(plot, top, bottom, spacing);
    }

    /// <summary>The gap between a bar end and its value caption: the theme spacing, capped so the caption stays with its bar.</summary>
    private static double BarLabelGap(VisualRenderContext context) => Math.Min(context.Theme.Spacing, 6);

    private static ChartRect InsetVerticalLabelGutters(ChartRect bounds, double top, double bottom, double spacing) {
        var available = bounds.Height - Math.Min(bounds.Height / 2, Math.Max(1, spacing));
        var scale = top + bottom > available ? available / (top + bottom) : 1;
        top *= scale; bottom *= scale;
        return new ChartRect(bounds.Left, bounds.Top + top, bounds.Width, bounds.Height - top - bottom);
    }
}
