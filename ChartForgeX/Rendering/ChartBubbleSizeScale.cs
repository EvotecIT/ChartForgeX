using System;
using ChartForgeX.Core;
using ChartForgeX.Primitives;

namespace ChartForgeX.Rendering;

/// <summary>Snapshots bubble size meaning once per preparation; final plot dimensions resolve responsive radii.</summary>
internal sealed class ChartBubbleSizeScale {
    private readonly double _minimumValue, _maximumValue, _minimumRadius;
    private readonly double? _maximumRadius;
    private readonly bool _reversed;
    private readonly ChartBubbleSizeEncoding _encoding;

    private ChartBubbleSizeScale(double minimumValue, double maximumValue, Chart chart, bool hasValues) {
        var options = chart.Options.Bubble;
        _minimumValue = minimumValue; _maximumValue = maximumValue;
        _minimumRadius = options.MinimumRadius; _maximumRadius = options.MaximumRadius; _reversed = options.Reversed;
        _encoding = options.SizeEncoding;
        HasValues = hasValues; Presentation = new ChartSizePresentation(chart, minimumValue, maximumValue, Mapping);
    }

    internal bool HasVisibleRadius => !_maximumRadius.HasValue || _maximumRadius.Value > 0;
    internal bool HasValues { get; }
    internal ChartSizePresentation Presentation { get; }
    internal double MinimumValue => _minimumValue;
    internal double MaximumValue => _maximumValue;
    internal bool Reversed => _reversed;
    internal string Mapping => _encoding == ChartBubbleSizeEncoding.Area ? "linear-area" : "sqrt-radius-range";

    internal static ChartBubbleSizeScale Create(Chart chart) {
        var options = chart.Options.Bubble;
        options.Validate();
        var minimum = double.PositiveInfinity; var maximum = double.NegativeInfinity;
        foreach (var series in chart.Series) {
            if (series.Kind != ChartSeriesKind.Bubble) continue;
            for (var index = 1; index < series.RenderPoints.Count; index += 2) {
                var size = series.RenderPoints[index].Y;
                minimum = Math.Min(minimum, size); maximum = Math.Max(maximum, size);
            }
        }
        // Empty supported tuple series contributes no source extent and remains a no-data scene.
        var hasValues = !double.IsPositiveInfinity(minimum);
        if (!hasValues) minimum = maximum = 0;
        return new ChartBubbleSizeScale(options.MinimumValue ?? minimum, options.MaximumValue ?? maximum, chart, hasValues);
    }

    internal double Radius(double size, ChartRect plot, ChartMarkerShape shape = ChartMarkerShape.Circle) {
        var maximumRadius = MaximumRadius(plot);
        var ratio = _minimumValue == _maximumValue ? .5 : Math.Max(0, Math.Min(1, ChartMath.Normalize(size, _minimumValue, _maximumValue)));
        if (_reversed) ratio = 1 - ratio;
        if (_encoding == ChartBubbleSizeEncoding.Area) {
            if (maximumRadius == 0) return 0;
            var minimumRatio = _minimumRadius / maximumRadius;
            return maximumRadius * Math.Sqrt(minimumRatio * minimumRatio * (1 - ratio) + ratio)
                * ChartMarkerGeometry.CircleAreaRadiusFactor(shape);
        }
        if (_minimumValue == _maximumValue) return ChartMath.InterpolateRange(_minimumRadius, maximumRadius, .5);
        // Preserve the family law: square-root interpolation of the radius range, not equal-area encoding.
        return ChartMath.InterpolateRange(_minimumRadius, maximumRadius, Math.Sqrt(ratio));
    }

    internal double MaximumRadius(ChartRect plot) => _maximumRadius ?? Math.Max(_minimumRadius,
        Math.Max(14, Math.Min(32, Math.Min(plot.Width, plot.Height) * .075)));
}
