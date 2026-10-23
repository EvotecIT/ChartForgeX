using System;
using System.Globalization;
using System.Linq;
using ChartForgeX.Rendering;

namespace ChartForgeX.Core;

/// <summary>Validates compressed-time source facts before any native chart layout is emitted.</summary>
internal static class ChartTimeSessionValidation {
    internal static void Validate(Chart chart) {
        if (chart.Options.YAxis.TimeSessions != null || chart.Options.SecondaryYAxis.TimeSessions != null || chart.Options.SecondaryXAxis.TimeSessions != null)
            throw new NotSupportedException("Time session compression is supported on the shared vertical Cartesian X axis only.");
        var axis = chart.Options.XAxis;
        var domain = axis.TimeSessions;
        if (domain == null) return;
        if (axis.Scale != ChartScaleKind.Time)
            throw new InvalidOperationException("Time session compression requires a time X axis.");
        if (!VisualCartesianCompiler.SupportsXViewport(chart) || ChartCartesianOrientation.IsHorizontal(chart)
            || chart.Series.Any(series => series.Kind is ChartSeriesKind.Bar or ChartSeriesKind.RangeBar or ChartSeriesKind.BoxPlot or ChartSeriesKind.Waterfall))
            throw new NotSupportedException("Time session compression requires supported vertical point, financial or paired-boundary series.");
        for (var index = 0; index < chart.Series.Count; index++) {
            var series = chart.Series[index];
            var count = series.ObservationCount;
            if (count == 0) continue;
            // Regressions retain their authored facts separately from fitted endpoints.
            var points = series.TrendLineSourcePoints.Count > 0 ? series.TrendLineSourcePoints : series.RenderPoints;
            var stride = series.TrendLineSourcePoints.Count > 0 ? 1 : Math.Max(1, points.Count / count);
            for (var raw = 0; raw < points.Count; raw += stride) {
                var value = points[raw].X;
                if (!domain.Includes(value)) throw new InvalidOperationException("Series " + index.ToString(CultureInfo.InvariantCulture)
                    + " ('" + series.Name + "') observation " + (raw / stride).ToString(CultureInfo.InvariantCulture)
                    + " at X=" + value.ToString("R", CultureInfo.InvariantCulture) + " falls outside the included time sessions.");
            }
        }
        for (var index = 0; index < chart.Annotations.Count; index++) {
            var annotation = chart.Annotations[index];
            if (annotation.HasSourceGeometry && (annotation.SourceStart is { } start && !domain.Includes(start.X)
                || annotation.SourceEnd is { } end && !domain.Includes(end.X)))
                throw new InvalidOperationException("Source annotation " + index.ToString(CultureInfo.InvariantCulture)
                    + " anchors an instant outside the included time sessions.");
            if (ChartCartesianOrientation.AnnotationKind(annotation, false) == ChartAnnotationKind.VerticalLine && domain.IsClosed(annotation.Value))
                throw new InvalidOperationException("Vertical annotation " + index.ToString(CultureInfo.InvariantCulture)
                    + " anchors a source instant inside a closed time-session gap.");
        }
        foreach (var label in axis.Labels) ValidateCaption(domain, label.Value);
        foreach (var label in chart.Options.XAxisLabels) ValidateCaption(domain, label.Value);
    }

    private static void ValidateCaption(ChartTimeSessionDomain domain, double value) {
        if (domain.IsClosed(value)) throw new InvalidOperationException("An authored X-axis caption at "
            + value.ToString("R", CultureInfo.InvariantCulture) + " anchors an instant inside a closed time-session gap.");
    }

    internal static void ValidateWindow(ChartAxis axis, double minimum, double maximum, string name) {
        if (axis.TimeSessions is { } domain && domain.Forward(maximum) <= domain.Forward(minimum))
            throw new ArgumentOutOfRangeException(name, "The X range contains no included time; a window wholly inside a closed session gap has no projected width.");
    }
}
