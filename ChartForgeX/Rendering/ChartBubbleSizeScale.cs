using System;
using ChartForgeX.Core;
using ChartForgeX.Primitives;

namespace ChartForgeX.Rendering;

/// <summary>Snapshots bubble size meaning once per preparation; final plot dimensions resolve responsive radii.</summary>
internal sealed class ChartBubbleSizeScale {
    private readonly double _minimumValue, _maximumValue, _minimumRadius;
    private readonly double? _maximumRadius;
    private readonly bool _reversed;

    private ChartBubbleSizeScale(double minimumValue, double maximumValue, ChartBubbleOptions options) {
        _minimumValue = minimumValue; _maximumValue = maximumValue;
        _minimumRadius = options.MinimumRadius; _maximumRadius = options.MaximumRadius; _reversed = options.Reversed;
    }

    internal bool HasVisibleRadius => !_maximumRadius.HasValue || _maximumRadius.Value > 0;

    internal static ChartBubbleSizeScale Create(Chart chart) {
        var options = chart.Options.Bubble;
        options.Validate();
        if (options.MinimumValue.HasValue)
            return new ChartBubbleSizeScale(options.MinimumValue.Value, options.MaximumValue!.Value, options);
        var minimum = double.PositiveInfinity; var maximum = double.NegativeInfinity;
        foreach (var series in chart.Series) {
            if (series.Kind != ChartSeriesKind.Bubble) continue;
            for (var index = 1; index < series.Points.Count; index += 2) {
                var size = series.Points[index].Y;
                minimum = Math.Min(minimum, size); maximum = Math.Max(maximum, size);
            }
        }
        // Empty supported tuple series contributes no source extent and remains a no-data scene.
        if (double.IsPositiveInfinity(minimum)) minimum = maximum = 0;
        return new ChartBubbleSizeScale(minimum, maximum, options);
    }

    internal double Radius(double size, ChartRect plot) {
        var maximumRadius = _maximumRadius ?? Math.Max(_minimumRadius,
            Math.Max(14, Math.Min(32, Math.Min(plot.Width, plot.Height) * .075)));
        if (_minimumValue == _maximumValue) return ChartMath.InterpolateRange(_minimumRadius, maximumRadius, .5);
        var ratio = Math.Max(0, Math.Min(1, ChartMath.Normalize(size, _minimumValue, _maximumValue)));
        if (_reversed) ratio = 1 - ratio;
        // Preserve the family law: square-root interpolation of the radius range, not equal-area encoding.
        return ChartMath.InterpolateRange(_minimumRadius, maximumRadius, Math.Sqrt(ratio));
    }
}
