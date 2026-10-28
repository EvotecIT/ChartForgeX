using System;
using ChartForgeX.Core;
using ChartForgeX.Primitives;

namespace ChartForgeX.Rendering;

/// <summary>Snapshots bubble size meaning once per preparation; final plot dimensions resolve responsive radii.</summary>
internal sealed class ChartBubbleSizeScale : IChartMarkerSizeScale {
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

    internal double Radius(double size, ChartRect plot, ChartMarkerOptions? markers = null) {
        var maximumRadius = MaximumRadius(plot);
        var ratio = ChartMarkerSizeMapping.Ratio(size, _minimumValue, _maximumValue, _reversed, .5);
        return ChartMarkerSizeMapping.BubbleRadius(ratio, _minimumRadius, maximumRadius, _minimumValue == _maximumValue, _encoding, markers?.Shape ?? ChartMarkerShape.Circle, markers?.CustomPath);
    }

    double IChartMarkerSizeScale.MinimumValue => MinimumValue;
    double IChartMarkerSizeScale.MaximumValue => MaximumValue;
    bool IChartMarkerSizeScale.Reversed => Reversed;
    string IChartMarkerSizeScale.Mapping => Mapping;
    ChartSizePresentation IChartMarkerSizeScale.Presentation => Presentation;
    double IChartMarkerSizeScale.Radius(double value, ChartRect plot) => Radius(value, plot);

    internal double MaximumRadius(ChartRect plot) => _maximumRadius ?? Math.Max(_minimumRadius,
        Math.Max(14, Math.Min(32, Math.Min(plot.Width, plot.Height) * .075)));
}
