using System;
using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Primitives;

namespace ChartForgeX.Core;

public sealed partial class Chart {
    /// <summary>
    /// Adds an annotation line at the mean source Y value, following the chart's value-axis orientation.
    /// </summary>
    /// <param name="label">The annotation label.</param>
    /// <param name="points">The source points used to compute the mean.</param>
    /// <param name="color">An optional annotation color.</param>
    /// <returns>The current chart.</returns>
    public Chart AddMeanLine(string label, IEnumerable<ChartPoint> points, ChartColor? color = null) {
        var values = StatisticalValues(points, nameof(points));
        return AddStatisticalAnnotation(values.Average(), null, label, color, 1);
    }

    /// <summary>
    /// Adds an annotation line at the median source Y value, following the chart's value-axis orientation.
    /// </summary>
    /// <param name="label">The annotation label.</param>
    /// <param name="points">The source points used to compute the median.</param>
    /// <param name="color">An optional annotation color.</param>
    /// <returns>The current chart.</returns>
    public Chart AddMedianLine(string label, IEnumerable<ChartPoint> points, ChartColor? color = null) {
        var values = StatisticalValues(points, nameof(points));
        values.Sort();
        var middle = values.Count / 2;
        var median = values.Count % 2 == 0 ? (values[middle - 1] + values[middle]) / 2.0 : values[middle];
        return AddStatisticalAnnotation(median, null, label, color, 1);
    }

    /// <summary>
    /// Adds a band around the mean source Y value using population standard deviation and the chart's value-axis orientation.
    /// </summary>
    /// <param name="label">The annotation label.</param>
    /// <param name="points">The source points used to compute the band.</param>
    /// <param name="deviations">The number of standard deviations above and below the mean.</param>
    /// <param name="color">An optional annotation color.</param>
    /// <param name="opacity">The band opacity.</param>
    /// <returns>The current chart.</returns>
    public Chart AddStandardDeviationBand(string label, IEnumerable<ChartPoint> points, double deviations = 1, ChartColor? color = null, double opacity = 0.12) {
        ChartGuards.Finite(deviations, nameof(deviations));
        if (deviations <= 0) throw new ArgumentOutOfRangeException(nameof(deviations), deviations, "Deviation count must be greater than zero.");
        var values = StatisticalValues(points, nameof(points));
        if (values.Count < 2) throw new ArgumentException("Standard deviation bands require at least two points.", nameof(points));
        var mean = values.Average();
        var variance = values.Sum(value => (value - mean) * (value - mean)) / values.Count;
        var distance = Math.Sqrt(variance) * deviations;
        return AddStatisticalAnnotation(mean - distance, mean + distance, label, color, opacity);
    }

    private Chart AddStatisticalAnnotation(double value, double? endValue, string label, ChartColor? color, double opacity) {
        var kind = endValue.HasValue ? ChartAnnotationKind.HorizontalBand : ChartAnnotationKind.HorizontalLine;
        Annotations.Add(new ChartAnnotation(kind, value, endValue, label, color ?? Options.ResolvedTheme.Axis, opacity) { FollowsSeriesValueAxis = true });
        return this;
    }

    private static List<double> StatisticalValues(IEnumerable<ChartPoint> points, string parameterName) {
        var materialized = ChartGuards.Points(points, parameterName);
        if (materialized.Count == 0) throw new ArgumentException("Statistical overlays require at least one point.", parameterName);
        return materialized.Select(point => point.Y).ToList();
    }
}
