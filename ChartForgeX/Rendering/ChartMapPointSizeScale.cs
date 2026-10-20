using System;
using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;

namespace ChartForgeX.Rendering;

/// <summary>Snapshots one map dataset's independent weight meaning; the shared circle-key owner consumes the same radii as its marks.</summary>
internal sealed class ChartMapPointSizeScale : IChartMarkerSizeScale {
    private readonly ChartMapPointSizeOptions _options;
    private readonly double? _fixedRadius;
    private double _width, _height, _maximumDot;

    internal ChartMapPointSizeScale(Chart chart, ChartSeries series, double minimum, double maximum, IReadOnlyList<double?> weights) {
        _options = series.MapPointSize.Clone(); _options.Validate(); _fixedRadius = series.MarkerRadius;
        MinimumValue = _options.MinimumValue ?? minimum; MaximumValue = _options.MaximumValue ?? maximum;
        HasValues = weights.Any(value => value.HasValue);
        Presentation = new ChartSizePresentation(chart, _options.SizeDimension, weights.Where(value => value.HasValue).Select(value => value!.Value),
            MinimumValue, MaximumValue, Mapping);
        var viewport = chart.Options.MapViewport;
        SetFrame(viewport.MaximumLongitude - viewport.MinimumLongitude, viewport.MaximumLatitude - viewport.MinimumLatitude,
            VisualMapCompiler.UsesCompactMapDot(viewport) ? 3.6 : 5.2);
    }
    internal bool HasValues { get; }
    public double MinimumValue { get; }
    public double MaximumValue { get; }
    public bool Reversed => _options.Reversed;
    public string Mapping => _fixedRadius.HasValue ? "fixed-radius" : "linear-radius-range";
    public ChartSizePresentation Presentation { get; }
    internal void SetFrame(double width, double height, double maximumDot) { _width = width; _height = height; _maximumDot = maximumDot; }
    internal double Dot(ChartRect plot) {
        var factor = Math.Min(plot.Width / _width, plot.Height / _height);
        return Math.Min(_maximumDot, Math.Min(_width * factor / Math.Max(1, _width / 3.5), _height * factor / Math.Max(1, _height / 3.5)) * .42);
    }
    public double Radius(double value, ChartRect plot) => Radius(value, Dot(plot));
    internal double Radius(double? value, double dot) {
        if (_fixedRadius.HasValue) return _fixedRadius.Value;
        if (!value.HasValue) return dot * 1.35;
        var ratio = ChartMarkerSizeMapping.Ratio(value.Value, MinimumValue, MaximumValue, Reversed, 0);
        if (_options.MinimumRadius.HasValue || _options.MaximumRadius.HasValue) {
            var minimum = _options.MinimumRadius ?? Math.Min(3, dot * 1.35);
            if (!_options.MinimumRadius.HasValue && _options.MaximumRadius.HasValue)
                minimum = Math.Min(minimum, _options.MaximumRadius.Value);
            var maximum = _options.MaximumRadius ?? Math.Max(minimum, dot * 2.2);
            if (maximum < minimum) throw new InvalidOperationException("The map maximum radius must be at least its resolved minimum radius.");
            return ChartMath.InterpolateRange(minimum, maximum, ratio);
        }
        return Math.Max(Math.Min(3, dot * 1.35), dot * (1.15 + ratio * 1.05));
    }
}
