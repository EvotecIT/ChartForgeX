using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;
using ChartForgeX.Typography;

namespace ChartForgeX.Rendering;

internal static partial class VisualCartesianCompiler {
    private sealed class CartesianValuePolicy {
        internal CartesianValuePolicy(VisualRenderContext context, ChartRect plot, CartesianValueSeries[] series, ChartCartesianProjection axes,
            ChartValueFormat format, (string Notation, string Format, string Culture) formatKey, string[] axisKeys, bool numericCaption, bool clip) {
            Context = context; Plot = plot; Series = series; Axes = axes; Format = format; FormatKey = formatKey; AxisKeys = axisKeys; NumericCaption = numericCaption; Clip = clip;
        }
        internal VisualRenderContext Context { get; }
        internal ChartRect Plot { get; }
        internal CartesianValueSeries[] Series { get; }
        internal ChartCartesianProjection Axes { get; }
        internal ChartValueFormat Format { get; }
        internal (string Notation, string Format, string Culture) FormatKey { get; }
        internal string[] AxisKeys { get; }
        internal bool NumericCaption { get; }
        internal bool Clip { get; }
        internal bool Same(CartesianValuePolicy other) => Plot.Equals(other.Plot) && FormatKey == other.FormatKey
            && AxisKeys.SequenceEqual(other.AxisKeys) && NumericCaption == other.NumericCaption && Clip == other.Clip
            && Context.Layout.Size.Equals(other.Context.Layout.Size) && Series.Length == other.Series.Length
            && VisualSeriesVisibilityCapture.SameAxis(Axes.X, other.Axes.X) && VisualSeriesVisibilityCapture.SameAxis(Axes.Y, other.Axes.Y)
            && VisualSeriesVisibilityCapture.SameAxis(Axes.SecondaryX, other.Axes.SecondaryX) && VisualSeriesVisibilityCapture.SameAxis(Axes.SecondaryY, other.Axes.SecondaryY)
            && Series.Select((row, index) => row.Same(other.Series[index])).All(value => value);
    }
    private sealed class CartesianValueSeries {
        private readonly ChartAxis _axis;
        private readonly Func<double, string> _readout;
        private readonly double[] _ticks;
        private readonly string _axisKey;
        private readonly IReadOnlyDictionary<double, string> _readoutFacts;
        private readonly string? _unit;
        internal CartesianValueSeries(string key, string name, ChartSeriesKind kind, ChartAxisSide side, ChartInterpolation interpolation, ChartStepPosition step,
            ChartMapper map, double baseline, ChartAxis axis, Func<double, string> readout, double[] ticks, IReadOnlyDictionary<double, string> readoutFacts, string axisKey, CartesianValuePoint[] points,
            string? unit = null) {
            Key = key; Name = name; Kind = kind; Side = side; Interpolation = interpolation; Step = step; Map = map; Baseline = baseline;
            _axis = axis; _readout = readout; _ticks = ticks; _readoutFacts = readoutFacts; _axisKey = axisKey; Points = points; _unit = unit;
        }
        internal string Key { get; }
        internal string Name { get; }
        internal ChartSeriesKind Kind { get; }
        internal ChartAxisSide Side { get; }
        internal ChartInterpolation Interpolation { get; }
        internal ChartStepPosition Step { get; }
        internal ChartMapper Map { get; }
        internal double Baseline { get; }
        internal CartesianValuePoint[] Points { get; }
        internal double Direction(double value, double endpoint) => MappedBarDirection(_axis, value, Baseline, endpoint);
        // Frames carry the same value unit as the prepared readouts they interpolate between.
        internal string Readout(double value, string? numericCaption) => ChartOptions.AppendUnit(numericCaption != null && _axis.ValueFormat == null
            && ChartAxisValueFormatter.FindExplicitLabel(_axis.Labels, value) == null ? numericCaption : _readout(value), _unit);
        internal bool Same(CartesianValueSeries other) => Key == other.Key && Name == other.Name && Kind == other.Kind && _unit == other._unit
            && Side == other.Side && Interpolation == other.Interpolation && Step == other.Step && Baseline == other.Baseline && _axisKey == other._axisKey
            && _ticks.SequenceEqual(other._ticks) && _readoutFacts.Count == other._readoutFacts.Count
            && _readoutFacts.All(pair => other._readoutFacts.TryGetValue(pair.Key, out var text) && text == pair.Value) && Points.Length == other.Points.Length
            && Points.Select((point, index) => point.Same(other.Points[index])).All(value => value);
    }
    private sealed class CartesianValuePoint {
        internal CartesianValuePoint(string id, ChartPoint point, double x, double y, string? fixedDisplay, TextCaseTransform textCase,
            double radius, bool marker, VisualMarkerScene.NativeMarkerPaint paint, BarSurfacePolicy bar, ChartColor color, double left, double width, ChartRect bounds) {
            Id = id; Source = point; X = x; Y = y; FixedDisplay = fixedDisplay; TextCase = textCase; Radius = radius; Marker = marker;
            MarkerPaint = paint; Bar = bar; Color = color; Left = left; Width = width; Bounds = bounds;

        }
        internal string Id { get; }
        internal ChartPoint Source { get; }
        internal double X { get; }
        internal double Y { get; }
        internal string? FixedDisplay { get; }
        internal TextCaseTransform TextCase { get; }
        internal double Radius { get; }
        internal bool Marker { get; }
        internal VisualMarkerScene.NativeMarkerPaint MarkerPaint { get; }
        internal BarSurfacePolicy Bar { get; }
        internal ChartColor Color { get; }
        internal double Left { get; }
        internal double Width { get; }
        internal ChartRect Bounds { get; }
        internal bool Same(CartesianValuePoint other) => Id == other.Id && Source.X == other.Source.X && Source.BreakBefore == other.Source.BreakBefore
            && X == other.X && FixedDisplay == other.FixedDisplay && TextCase == other.TextCase && Radius == other.Radius && Marker == other.Marker
            && MarkerPaint.Same(other.MarkerPaint) && Color.Equals(other.Color) && Left == other.Left && Width == other.Width && SameBar(Bar, other.Bar);
        private static bool SameBar(BarSurfacePolicy a, BarSurfacePolicy b) {
            var x = a.Style; var y = b.Style;
            return a.CornerRadius == b.CornerRadius && a.Role == b.Role && a.Pattern == b.Pattern && a.Backdrop.Equals(b.Backdrop)
                && x.Kind == y.Kind && x.BodyOpacity == y.BodyOpacity && x.CapOpacity == y.CapOpacity && x.CapThickness == y.CapThickness
                && x.CapInset == y.CapInset && x.CornerRadius == y.CornerRadius && x.CapShadowOpacity == y.CapShadowOpacity
                && x.CapShadowOffset == y.CapShadowOffset && x.CapShadowSpread == y.CapShadowSpread && x.CapHighlightOpacity == y.CapHighlightOpacity;
        }
    }
}
