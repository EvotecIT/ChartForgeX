using System;
using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Rendering;
using ChartForgeX.Primitives;

namespace ChartForgeX.Core;

/// <summary>Captures per-axis time domains in the private presentation view without changing source observations.</summary>
internal static class ChartTimeSpacingPreparation {
    internal static void Capture(Chart chart) {
        var axes = new[] { chart.Options.XAxis, chart.Options.YAxis, chart.Options.SecondaryXAxis, chart.Options.SecondaryYAxis };
        if (axes.All(axis => axis.TimeSpacing == null)) return;
        if (!VisualCartesianCompiler.SupportsXViewport(chart)) throw new NotSupportedException("Discrete time spacing requires supported Cartesian series.");
        var horizontal = ChartCartesianOrientation.IsHorizontal(chart);
        chart.Options.XAxis = CaptureAxis(chart, axes[0], horizontal, ChartAxisDimension.X, ChartAxisSide.Primary);
        chart.Options.YAxis = CaptureAxis(chart, axes[1], horizontal, ChartAxisDimension.Y, ChartAxisSide.Primary);
        chart.Options.SecondaryXAxis = CaptureAxis(chart, axes[2], horizontal, ChartAxisDimension.X, ChartAxisSide.Secondary);
        chart.Options.SecondaryYAxis = CaptureAxis(chart, axes[3], horizontal, ChartAxisDimension.Y, ChartAxisSide.Secondary);
    }
    private static ChartAxis CaptureAxis(Chart chart, ChartAxis axis, bool horizontal, ChartAxisDimension dimension, ChartAxisSide side) {
        var spacing = axis.TimeSpacing;
        if (spacing == null) return axis;
        if (axis.Scale != ChartScaleKind.Time) throw new InvalidOperationException("Discrete time spacing requires a time scale.");
        if (axis.TimeSessions != null) throw new InvalidOperationException("Discrete time spacing and included sessions cannot be combined.");
        var category = dimension == (horizontal ? ChartAxisDimension.Y : ChartAxisDimension.X);
        if (category && side == ChartAxisSide.Secondary) throw new NotSupportedException("A secondary category time axis is unavailable.");
        var values = new SortedSet<double>(); var envelope = new SortedSet<double>(); var participating = new List<ChartSeries>();
        foreach (var series in chart.Series) {
            if (!category && series.ValueAxis != side) continue;
            if (series.TimeBucketInterval != null) throw new NotSupportedException("Authored time-bucket intervals cannot be combined with discrete time spacing.");
            var points = TimeAxisPoints(series, category);
            var count = series.TrendLineSourcePoints.Count > 0 ? points.Count : series.ObservationCount;
            var stride = count == 0 ? 1 : Math.Max(1, points.Count / count);
            for (var index = 0; index < points.Count; index += stride) {
                var value = category ? points[index].X : points[index].Y;
                ValidateInstant(value); values.Add(value); envelope.Add(value);
                var width = series.Kind == ChartSeriesKind.Bubble ? 1 : Math.Min(stride, points.Count - index);
                for (var part = 0; part < width; part++) {
                    var bound = category ? points[index + part].X : points[index + part].Y;
                    ValidateInstant(bound); envelope.Add(bound);
                }
                if (values.Count > ChartTimeSpacingDomain.MaximumEntries) throw new InvalidOperationException("The ordinal time domain exceeds the 100,000-instant limit.");
            }
            participating.Add(series);
        }
        if (side == ChartAxisSide.Primary) {
            foreach (var annotation in chart.Annotations) {
                if (annotation.HasSourceGeometry) continue; // Drawings intentionally do not enlarge native domains.
                var kind = ChartCartesianOrientation.AnnotationKind(annotation, horizontal);
                var followsX = kind is ChartAnnotationKind.VerticalLine or ChartAnnotationKind.VerticalBand;
                if (followsX != (dimension == ChartAxisDimension.X)) continue;
                ValidateInstant(annotation.Value); envelope.Add(annotation.Value);
                if (annotation.EndValue is { } end) { ValidateInstant(end); envelope.Add(end); }
            }
        }
        if (category) foreach (var label in ChartCartesianOrientation.CategoryLabels(chart).Where(label => label.Groups.Count > 0)) {
            ValidateInstant(label.Value); envelope.Add(label.Value);
        }
        if (axis.Minimum.HasValue) ValidateInstant(axis.Minimum.Value);
        if (axis.Maximum.HasValue) ValidateInstant(axis.Maximum.Value);
        // Empty domains still have a deterministic transform; they do not fabricate observations or semantic facts.
        if (values.Count == 0) values.Add(axis.Minimum ?? axis.Maximum ?? 25569d);
        if (envelope.Count == 0) envelope.Add(values.Min);
        var minimum = Math.Min(axis.Minimum ?? envelope.Min, envelope.Min);
        var maximum = Math.Max(axis.Maximum ?? envelope.Max, envelope.Max);
        var instants = spacing.CalendarUnit is { } unit ? ChartTimeScale.UnitBoundaries(axis, minimum, maximum, unit) : values.ToArray();
        var domain = new ChartTimeSpacingDomain(spacing, instants, minimum, maximum);
        if (domain.Unit) {
            foreach (var series in participating) {
                var points = TimeAxisPoints(series, category);
                var count = series.TrendLineSourcePoints.Count > 0 ? points.Count : series.ObservationCount;
                var stride = count == 0 ? 1 : Math.Max(1, points.Count / count);
                var seen = new HashSet<double>();
                for (var index = 0; index < points.Count; index += stride) {
                    var value = category ? points[index].X : points[index].Y;
                    if (!seen.Add(domain.Forward(value))) throw new InvalidOperationException("A unit-time series contains more than one observation in the same calendar band; aggregate source data explicitly before charting.");
                }
            }
        }
        var captured = axis.Clone(); captured.PreparedTimeDomain = domain;
        return captured;
    }

    /// <summary>Includes typed waterfall display items without changing their intentionally empty ordinary point collection.</summary>
    private static IReadOnlyList<ChartPoint> TimeAxisPoints(ChartSeries series, bool category) {
        if (!category || !series.IsTypedWaterfall)
            return series.TrendLineSourcePoints.Count > 0 ? series.TrendLineSourcePoints : series.RenderPoints;
        if (series.WaterfallItems.Count > ChartTimeSpacingDomain.MaximumEntries)
            throw new InvalidOperationException("The ordinal time domain exceeds the 100,000-instant limit.");
        return ChartCartesianOrientation.Categories(series).Select(value => new ChartPoint(value, 0)).ToArray();
    }

    private static void ValidateInstant(double value) {
        if (!ChartDateTime.TryFromOrderedOaDate(value, out _)) throw new InvalidOperationException("Discrete time spacing requires finite ordered UTC instants on or after 1899-12-30.");
    }
}
